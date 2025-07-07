using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace V1_2025_07.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminOtpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "Admins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OtpExpiry",
                table: "Admins",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "OtpExpiry",
                table: "Admins");
        }
    }
}
