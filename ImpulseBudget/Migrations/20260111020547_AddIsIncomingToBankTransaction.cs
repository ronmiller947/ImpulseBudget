using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpulseBudget.Migrations
{
    /// <inheritdoc />
    public partial class AddIsIncomingToBankTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsIncoming",
                table: "BankTransactions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIncoming",
                table: "BankTransactions");
        }
    }
}
