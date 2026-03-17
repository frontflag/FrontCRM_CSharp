using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContactHistoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "customercontacthistory",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextFollowUpTime",
                table: "customercontacthistory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatorId",
                table: "customercontacthistory",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "customercontacthistory",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "customercontacthistory",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "customercontacthistory");

            migrationBuilder.DropColumn(
                name: "NextFollowUpTime",
                table: "customercontacthistory");

            migrationBuilder.DropColumn(
                name: "OperatorId",
                table: "customercontacthistory");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "customercontacthistory");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "customercontacthistory");
        }
    }
}
