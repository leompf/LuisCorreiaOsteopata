using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuisCorreiaOsteopata.Library.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntitiesUserPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PaymentReference",
                table: "Payments",
                newName: "StripePaymentIntentId");

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "StripePaymentIntentId",
                table: "Payments",
                newName: "PaymentReference");
        }
    }
}
