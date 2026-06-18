using System.Data;
using System.Security.Claims;
using Classroom.Api.Common.Authorization;
using Classroom.Api.Common.Security;
using Classroom.Api.Common.Validation;
using Classroom.Domain.Avatars;
using Classroom.Domain.Points;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Classroom.Api.Features.Store;

public static class StoreEndpoints
{
    /// <summary>Bounded retries for the serializable purchase txn (design.md §4.1).</summary>
    private const int MaxPurchaseAttempts = 3;

    public static IEndpointRouteBuilder MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        // The store is per-student (it needs their wallet + owned set), nested under the class route
        // like the rest of the roster-scoped endpoints. The catalog itself is global (design.md §3.3).
        var group = app.MapGroup("/api/classes/{classId:guid}/students/{studentId:guid}/store")
            .WithTags("Store")
            .RequireAuthorization();

        group.MapGet("/", StoreAsync)
            .WithSummary("List buyable catalog options for a student (catalog minus owned, with affordability).");

        group.MapPost("/purchase", PurchaseAsync)
            .AddEndpointFilter<ValidationFilter<PurchaseRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Buy an option for a student in a serializable transaction (no overspend, no double-buy).");

        return app;
    }

    private static async Task<IResult> StoreAsync(
        Guid classId,
        Guid studentId,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        // Shop is kiosk-reachable (design.md §5.4): a kiosk session scoped to this class, or a member.
        if (!await IsKioskOrMember(authz, user, classId))
        {
            return Forbidden();
        }

        if (!await StudentInClass(db, classId, studentId))
        {
            return StudentNotFound();
        }

        var wallet = await WalletAsync(db, studentId);

        // The catalog minus what the student already owns (anti-join). Defaults are owned from creation,
        // so they naturally drop out. Affordability is computed against the wallet for the UI.
        var ownedIds = db.StudentOwnedItems
            .Where(o => o.StudentId == studentId)
            .Select(o => o.ItemId);

        var items = await db.AvatarItems
            .Where(i => !ownedIds.Contains(i.Id))
            .OrderBy(i => i.Slot)
            .ThenBy(i => i.Cost)
            .ThenBy(i => i.DisplayName)
            .Select(i => new StoreItemResponse(
                i.Id, i.Slot, i.OptionValue, i.DisplayName, i.Cost, i.Rarity, wallet >= i.Cost))
            .ToListAsync();

        return Results.Ok(new StoreResponse(studentId, wallet, AvatarCatalog.Style, items));
    }

    private static async Task<IResult> PurchaseAsync(
        Guid classId,
        Guid studentId,
        PurchaseRequest request,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        // Purchase is kiosk-reachable (design.md §5.4); the txn/affordability guarantees are unchanged.
        if (!await IsKioskOrMember(authz, user, classId))
        {
            return Forbidden();
        }

        if (!await StudentInClass(db, classId, studentId))
        {
            return StudentNotFound();
        }

        var item = await db.AvatarItems
            .Where(i => i.Id == request.ItemId)
            .Select(i => new { i.Id, i.Cost })
            .FirstOrDefaultAsync();
        if (item is null)
        {
            return ItemNotFound();
        }

        // Where each guarantee lives (design.md §4.1): the UNIQUE(StudentId, ItemId) composite PK on
        // StudentOwnedItem is the *real* DB-level guard against double-buy. The "wallet never goes
        // negative via a purchase" invariant is cross-row — a CHECK can't express it — so it rests
        // entirely on this serializable transaction + retry + the in-transaction re-sum below.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                // Re-sum the wallet from the ledger INSIDE the transaction (and afresh on every retry).
                // Never trust a client-sent balance; LedgerMath stays the single source of truth.
                var wallet = await WalletAsync(db, studentId);
                if (wallet < item.Cost)
                {
                    // The wallet may already be negative from deductions (§2.5); a purchase is the one
                    // operation blocked when funds are short. No row written, txn rolls back on dispose.
                    return InsufficientFunds(wallet, item.Cost);
                }

                // One negative, system-generated Purchase ledger row (no awarding teacher) ...
                db.PointTransactions.Add(new PointTransaction
                {
                    StudentId = studentId,
                    Amount = -item.Cost,
                    Type = PointTransactionType.Purchase,
                    ItemId = item.Id,
                });
                // ... plus the inventory row whose composite PK is the double-buy backstop.
                db.StudentOwnedItems.Add(new StudentOwnedItem { StudentId = studentId, ItemId = item.Id });

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new PurchaseResponse(studentId, item.Id, item.Cost, wallet - item.Cost));
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Either the student already owned it, or two concurrent purchases both passed the
                // affordability check and raced to insert the same ownership row — the unique PK
                // rejected the loser. Both are a 409; retrying wouldn't help.
                return AlreadyOwned();
            }
            catch (Exception ex) when (attempt < MaxPurchaseAttempts && IsTransient(ex))
            {
                // Serialization failure (40001) or deadlock (40P01): retry the whole transaction with a
                // fresh re-sum. Clear the tracked-but-unsaved rows so the retry starts clean.
                db.ChangeTracker.Clear();
            }
        }
    }

    /// <summary>Re-sums a student's spendable wallet from the ledger via the shared <see cref="LedgerMath"/>.</summary>
    private static async Task<int> WalletAsync(ClassroomDbContext db, Guid studentId)
    {
        var entries = await db.PointTransactions
            .Where(t => t.StudentId == studentId)
            .Select(t => new LedgerEntry(t.Amount, t.Type, t.VoidedAt != null))
            .ToListAsync();
        return LedgerMath.Summarize(entries).Wallet;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static bool IsTransient(Exception ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            if (e is PostgresException pg)
            {
                return pg.SqlState is PostgresErrorCodes.SerializationFailure
                    or PostgresErrorCodes.DeadlockDetected;
            }
        }

        return false;
    }

    private static async Task<bool> IsKioskOrMember(
        IAuthorizationService authz, ClaimsPrincipal user, Guid classId)
    {
        var result = await authz.AuthorizeAsync(user, classId, new KioskOrMemberRequirement());
        return result.Succeeded;
    }

    private static Task<bool> StudentInClass(ClassroomDbContext db, Guid classId, Guid studentId) =>
        db.Students.AnyAsync(s => s.Id == studentId && s.ClassId == classId);

    private static IResult Forbidden() => Results.Problem(
        title: "Forbidden",
        detail: "You do not have access to this class.",
        statusCode: StatusCodes.Status403Forbidden);

    private static IResult StudentNotFound() => Results.Problem(
        title: "Student not found",
        detail: "No such student exists in this class.",
        statusCode: StatusCodes.Status404NotFound);

    private static IResult ItemNotFound() => Results.Problem(
        title: "Item not found",
        detail: "No such catalog option exists.",
        statusCode: StatusCodes.Status404NotFound);

    private static IResult AlreadyOwned() => Results.Problem(
        title: "Already owned",
        detail: "The student already owns this option.",
        statusCode: StatusCodes.Status409Conflict);

    private static IResult InsufficientFunds(int wallet, int cost) => Results.Problem(
        title: "Insufficient funds",
        detail: $"This option costs {cost} but the wallet holds {wallet}.",
        statusCode: StatusCodes.Status402PaymentRequired);
}
