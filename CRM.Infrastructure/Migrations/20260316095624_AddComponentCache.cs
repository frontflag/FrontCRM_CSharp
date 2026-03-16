using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComponentCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "component_cache",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Mpn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ManufacturerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "text", maxLength: 2000, nullable: true),
                    LifecycleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PackageType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsRoHSCompliant = table.Column<bool>(type: "boolean", nullable: true),
                    SpecsJson = table.Column<string>(type: "text", nullable: true),
                    SellersJson = table.Column<string>(type: "text", nullable: true),
                    AlternativesJson = table.Column<string>(type: "text", nullable: true),
                    ApplicationsJson = table.Column<string>(type: "text", nullable: true),
                    PriceTrendJson = table.Column<string>(type: "text", nullable: true),
                    NewsJson = table.Column<string>(type: "text", nullable: true),
                    DataSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QueryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component_cache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_component_cache_Mpn",
                table: "component_cache",
                column: "Mpn",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "component_cache");
        }
    }
}
