using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuisCorreiaOsteopata.Library.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueNif : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Nif",
                table: "AspNetUsers",
                column: "Nif",
                unique: true,
                filter: "[Nif] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Nif",
                table: "AspNetUsers");
        }
    }
}
