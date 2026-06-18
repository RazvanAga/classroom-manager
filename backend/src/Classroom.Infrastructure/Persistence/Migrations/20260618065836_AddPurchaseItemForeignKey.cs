using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classroom.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseItemForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_ItemId",
                table: "PointTransactions",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_PointTransactions_AvatarItems_ItemId",
                table: "PointTransactions",
                column: "ItemId",
                principalTable: "AvatarItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PointTransactions_AvatarItems_ItemId",
                table: "PointTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PointTransactions_ItemId",
                table: "PointTransactions");
        }
    }
}
