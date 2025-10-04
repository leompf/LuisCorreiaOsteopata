using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuisCorreiaOsteopata.Library.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserEntity_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Staff",
                newName: "Names");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Patients",
                newName: "Names");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "AspNetUsers",
                newName: "Names");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Names",
                table: "Staff",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "Names",
                table: "Patients",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "Names",
                table: "AspNetUsers",
                newName: "FirstName");
        }
    }
}
