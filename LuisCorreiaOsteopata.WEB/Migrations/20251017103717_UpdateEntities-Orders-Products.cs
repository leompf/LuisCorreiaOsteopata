using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuisCorreiaOsteopata.Library.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntitiesOrdersProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AppointmentCredits_AppointmentCreditId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Orders_OrderId",
                table: "OrderDetails");

            migrationBuilder.DropTable(
                name: "AppointmentCredits");

            migrationBuilder.DropColumn(
                name: "IsCreditProduct",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Appointments");

            migrationBuilder.RenameColumn(
                name: "CreditQuantity",
                table: "Products",
                newName: "ProductType");

            migrationBuilder.RenameColumn(
                name: "AppointmentCreditId",
                table: "Appointments",
                newName: "OrderDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_Appointments_AppointmentCreditId",
                table: "Appointments",
                newName: "IX_Appointments_OrderDetailId");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemainingUses",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_OrderDetails_OrderDetailId",
                table: "Appointments",
                column: "OrderDetailId",
                principalTable: "OrderDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Orders_OrderId",
                table: "OrderDetails",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_OrderDetails_OrderDetailId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Orders_OrderId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "RemainingUses",
                table: "OrderDetails");

            migrationBuilder.RenameColumn(
                name: "ProductType",
                table: "Products",
                newName: "CreditQuantity");

            migrationBuilder.RenameColumn(
                name: "OrderDetailId",
                table: "Appointments",
                newName: "AppointmentCreditId");

            migrationBuilder.RenameIndex(
                name: "IX_Appointments_OrderDetailId",
                table: "Appointments",
                newName: "IX_Appointments_AppointmentCreditId");

            migrationBuilder.AddColumn<bool>(
                name: "IsCreditProduct",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "OrderDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Appointments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "AppointmentCredits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    TotalAppointments = table.Column<int>(type: "int", nullable: false),
                    UsedAppointments = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentCredits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentCredits_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentCredits_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentCredits_PaymentId",
                table: "AppointmentCredits",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentCredits_UserId",
                table: "AppointmentCredits",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AppointmentCredits_AppointmentCreditId",
                table: "Appointments",
                column: "AppointmentCreditId",
                principalTable: "AppointmentCredits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Orders_OrderId",
                table: "OrderDetails",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }
    }
}
