using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerBlacklistFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BlackListAt",
                table: "customerinfo",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlackListByUserId",
                table: "customerinfo",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlackListByUserName",
                table: "customerinfo",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlackListAt",
                table: "customerinfo");

            migrationBuilder.DropColumn(
                name: "BlackListByUserId",
                table: "customerinfo");

            migrationBuilder.DropColumn(
                name: "BlackListByUserName",
                table: "customerinfo");
        }
    }
}
