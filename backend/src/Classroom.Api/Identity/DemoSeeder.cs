using Classroom.Domain.Avatars;
using Classroom.Domain.Behaviors;
using Classroom.Domain.Classes;
using Classroom.Domain.Common;
using Classroom.Domain.Groups;
using Classroom.Domain.Identity;
using Classroom.Domain.Points;
using Classroom.Domain.Students;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Classroom.Api.Identity;

/// <summary>
/// Builds and resets the shared, writable demo account to a rich, known state (design.md §9.2): a full
/// class, a varied multi-week point history, students showing off purchased avatars, and a saved
/// grouping — so a reviewer's one-click login always lands on something populated.
/// <para>
/// <see cref="ReseedAsync"/> is the single entry point: it ensures the demo teacher exists, wipes
/// whatever previous visitors left behind — scoped <b>strictly</b> to the demo teacher's own classes,
/// so it can never touch the real owner's data — then rebuilds the rich state deterministically from a
/// fixed seed. Invoked once at startup and on a schedule by <see cref="DemoReseedService"/>.
/// </para>
/// </summary>
public sealed class DemoSeeder(
    ClassroomDbContext db,
    UserManager<ApplicationUser> users,
    IPasswordHasher<ApplicationUser> pinHasher,
    IOptions<DemoAccountOptions> options)
{
    // A fixed seed makes the "rich state" reproducible run-to-run — who leads the board and who owns
    // what stays stable, so the demo reads as curated rather than random noise (design.md §9.2).
    private const int Seed = 20260619;

    private readonly DemoAccountOptions _options = options.Value;

    private static readonly IReadOnlyList<(string Name, Gender Gender)> Roster =
    [
        ("Ava", Gender.Female), ("Liam", Gender.Male), ("Sofia", Gender.Female),
        ("Noah", Gender.Male), ("Mia", Gender.Female), ("Lucas", Gender.Male),
        ("Emma", Gender.Female), ("Ethan", Gender.Male), ("Olivia", Gender.Female),
        ("Mason", Gender.Male), ("Isabella", Gender.Female), ("James", Gender.Male),
    ];

    /// <summary>
    /// Resets the demo to its rich, known state. Idempotent and self-healing: re-running it discards
    /// the prior demo content and rebuilds from scratch.
    /// </summary>
    public async Task ReseedAsync(CancellationToken cancellationToken = default)
    {
        var teacher = await EnsureDemoTeacherAsync(cancellationToken);

        // All-or-nothing: a half-applied reset would leave the demo worse than before.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WipeDemoDataAsync(teacher.Id, cancellationToken);
        await BuildRichClassAsync(teacher.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures the demo teacher exists (idempotent), returning it. Pre-confirmed and given a kiosk PIN
    /// so the demo is fully interactive out of the box (no SMTP in v1; design.md §5.2).
    /// </summary>
    public async Task<ApplicationUser> EnsureDemoTeacherAsync(CancellationToken cancellationToken = default)
    {
        var existing = await users.FindByEmailAsync(_options.Email);
        if (existing is not null)
        {
            return existing;
        }

        var teacher = new ApplicationUser
        {
            Id = EntityId.New(),
            UserName = _options.Email,
            Email = _options.Email,
            EmailConfirmed = true,
            DisplayName = _options.DisplayName,
        };
        teacher.KioskPinHash = pinHasher.HashPassword(teacher, _options.KioskPin);

        var result = await users.CreateAsync(teacher, _options.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Failed to seed demo teacher '{_options.Email}': {errors}");
        }

        return teacher;
    }

    /// <summary>
    /// Hard-deletes every row belonging to the demo teacher's classes, in child-before-parent order so
    /// nothing is orphaned. Scoped to <paramref name="teacherId"/>'s classes only — the real owner's
    /// data is never in range. (A hard delete is right here: the demo is disposable, unlike the
    /// soft-delete/purge paths that preserve a real teacher's history.)
    /// </summary>
    private async Task WipeDemoDataAsync(Guid teacherId, CancellationToken cancellationToken)
    {
        var classIds = await db.ClassTeachers
            .Where(ct => ct.TeacherId == teacherId)
            .Select(ct => ct.ClassId)
            .ToListAsync(cancellationToken);
        if (classIds.Count == 0)
        {
            return;
        }

        // Include soft-deleted rows: a previous visitor may have removed a student, and the reset must
        // clear those tombstones too (the global filter would otherwise hide them from the wipe).
        var studentIds = await db.Students
            .IgnoreQueryFilters()
            .Where(s => classIds.Contains(s.ClassId))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        await db.PointTransactions.Where(t => studentIds.Contains(t.StudentId)).ExecuteDeleteAsync(cancellationToken);
        await db.StudentEquipped.Where(e => studentIds.Contains(e.StudentId)).ExecuteDeleteAsync(cancellationToken);
        await db.StudentOwnedItems.Where(o => studentIds.Contains(o.StudentId)).ExecuteDeleteAsync(cancellationToken);
        await db.GroupMembers.Where(m => studentIds.Contains(m.StudentId)).ExecuteDeleteAsync(cancellationToken);
        await db.Groupings.Where(g => classIds.Contains(g.ClassId)).ExecuteDeleteAsync(cancellationToken);
        await db.Students.IgnoreQueryFilters().Where(s => studentIds.Contains(s.Id)).ExecuteDeleteAsync(cancellationToken);
        await db.Behaviors.Where(b => classIds.Contains(b.ClassId)).ExecuteDeleteAsync(cancellationToken);
        await db.ClassTeachers.Where(ct => classIds.Contains(ct.ClassId)).ExecuteDeleteAsync(cancellationToken);
        await db.Classes.IgnoreQueryFilters().Where(c => classIds.Contains(c.Id)).ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Materializes the rich demo class: roster, default behaviors, default avatars, a multi-week point
    /// history, avatar purchases for the higher earners, and one saved grouping. Everything is queued
    /// on the context and committed in a single <c>SaveChanges</c> within the caller's transaction.
    /// </summary>
    private async Task BuildRichClassAsync(Guid teacherId, CancellationToken cancellationToken)
    {
        var rng = new Random(Seed);
        var now = DateTime.UtcNow;

        // 1) The class, owned by the demo teacher, seeded with the default behavior catalog.
        var klass = new Class { Name = "Room 12 — Demo Class", CreatedAt = now.AddDays(-45) };
        klass.Teachers.Add(new ClassTeacher { TeacherId = teacherId, Role = ClassRole.Owner });
        db.Classes.Add(klass);

        var behaviors = DefaultBehaviors.CreateFor(klass.Id).ToList();
        db.Behaviors.AddRange(behaviors);
        var positives = behaviors.Where(b => b.DefaultPoints > 0).ToList();
        var negatives = behaviors.Where(b => b.DefaultPoints < 0).ToList();

        // The global avatar catalog is already seeded (design.md §3.3); read what we need to grant and
        // sell. Defaults are the free starter set; the rest are priced store options.
        var defaultItems = await db.AvatarItems
            .Where(i => i.IsDefault)
            .Select(i => new { i.Id, i.Slot })
            .ToListAsync(cancellationToken);
        var purchasable = await db.AvatarItems
            .Where(i => !i.IsDefault && i.Cost > 0)
            .Select(i => new { i.Id, i.Slot, i.Cost })
            .ToListAsync(cancellationToken);

        // 2) Students, each granted + equipped the free default avatar (mirrors roster add, §3.3).
        var students = Roster
            .Select((r, i) => new Student
            {
                ClassId = klass.Id,
                DisplayName = r.Name,
                Gender = r.Gender,
                CreatedAt = klass.CreatedAt.AddMinutes(i),
            })
            .ToList();
        db.Students.AddRange(students);

        // Track the equipped row per (student, slot) so a purchase below can swap it in place, and the
        // running wallet so a purchase never overspends (CLAUDE.md invariant).
        var equipped = new Dictionary<(Guid Student, AvatarSlot Slot), StudentEquipped>();
        var wallet = students.ToDictionary(s => s.Id, _ => 0);

        foreach (var student in students)
        {
            foreach (var item in defaultItems)
            {
                db.StudentOwnedItems.Add(new StudentOwnedItem
                {
                    StudentId = student.Id,
                    ItemId = item.Id,
                    AcquiredAt = student.CreatedAt,
                });
                var slot = new StudentEquipped { StudentId = student.Id, Slot = item.Slot, ItemId = item.Id };
                db.StudentEquipped.Add(slot);
                equipped[(student.Id, item.Slot)] = slot;
            }
        }

        // 3) A varied point history spread over the last five weeks. Weighted ~80% positive so wallets
        //    trend up and the leaderboard is lively, with occasional deductions for realism.
        foreach (var student in students)
        {
            var eventCount = rng.Next(10, 26);
            for (var e = 0; e < eventCount; e++)
            {
                var usePositive = negatives.Count == 0 || rng.NextDouble() < 0.8;
                var behavior = usePositive
                    ? positives[rng.Next(positives.Count)]
                    : negatives[rng.Next(negatives.Count)];

                var createdAt = now
                    .AddDays(-rng.Next(0, 35))
                    .AddHours(-rng.Next(0, 6))
                    .AddMinutes(-rng.Next(0, 60));

                db.PointTransactions.Add(new PointTransaction
                {
                    StudentId = student.Id,
                    Amount = behavior.DefaultPoints,
                    Type = behavior.DefaultPoints >= 0 ? PointTransactionType.Award : PointTransactionType.Deduction,
                    BehaviorId = behavior.Id,
                    AwardedByTeacherId = teacherId,
                    CreatedAt = createdAt,
                });
                wallet[student.Id] += behavior.DefaultPoints;
            }
        }

        // 4) Avatar purchases for the spenders — roughly half the class buys one or two options they can
        //    afford, so the demo shows off the earn → spend → show-off loop. Each is a Purchase ledger
        //    row plus an owned item, and the equipped slot is swapped to the new option in place.
        var slotsAvailable = purchasable.Select(p => p.Slot).Distinct().ToList();
        foreach (var student in students)
        {
            if (rng.NextDouble() < 0.5)
            {
                continue;
            }

            var budget = wallet[student.Id];
            var slots = slotsAvailable.OrderBy(_ => rng.Next()).Take(rng.Next(1, 3));
            foreach (var slot in slots)
            {
                var affordable = purchasable.Where(p => p.Slot == slot && p.Cost <= budget).ToList();
                if (affordable.Count == 0)
                {
                    continue;
                }

                var pick = affordable[rng.Next(affordable.Count)];
                budget -= pick.Cost;
                var boughtAt = now.AddDays(-rng.Next(0, 10));

                db.StudentOwnedItems.Add(new StudentOwnedItem
                {
                    StudentId = student.Id,
                    ItemId = pick.Id,
                    AcquiredAt = boughtAt,
                });
                db.PointTransactions.Add(new PointTransaction
                {
                    StudentId = student.Id,
                    Amount = -pick.Cost,
                    Type = PointTransactionType.Purchase,
                    ItemId = pick.Id,
                    CreatedAt = boughtAt,
                });
                equipped[(student.Id, slot)].ItemId = pick.Id;
            }
        }

        // 5) One saved grouping, so the persisted-group-maker (#12) has something to show on arrival.
        var grouping = new Grouping
        {
            ClassId = klass.Id,
            Name = "Project teams",
            GroupSize = 3,
            BalancedByGender = true,
            CreatedAt = now.AddDays(-2),
        };
        db.Groupings.Add(grouping);

        var groupingStudents = students.Select(s => new GroupingStudent(s.Id, s.Gender)).ToList();
        var groups = GroupFormer.Form(groupingStudents, grouping.GroupSize, grouping.BalancedByGender, Seed);
        for (var groupNumber = 0; groupNumber < groups.Count; groupNumber++)
        {
            foreach (var studentId in groups[groupNumber])
            {
                db.GroupMembers.Add(new GroupMember
                {
                    GroupingId = grouping.Id,
                    StudentId = studentId,
                    GroupNumber = groupNumber,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
