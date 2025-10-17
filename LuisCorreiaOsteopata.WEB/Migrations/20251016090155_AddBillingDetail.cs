using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuisCorreiaOsteopata.Library.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillingDetailId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ZipCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    City = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NIF = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingDetails_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BillingDetailId",
                table: "Orders",
                column: "BillingDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingDetails_UserId",
                table: "BillingDetails",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_BillingDetails_BillingDetailId",
                table: "Orders",
                column: "BillingDetailId",
                principalTable: "BillingDetails",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_BillingDetails_BillingDetailId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "BillingDetails");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BillingDetailId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BillingDetailId",
                table: "Orders");
        }
    }
}
