using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NutritionApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "diet_plans",
                columns: new[] { "id", "diet_type", "goal", "is_template", "name", "target_weight", "user_id", "macro_carb_pct", "macro_fat_pct", "macro_protein_pct" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "Balanced", "WeightLoss", true, "Perte de poids", 0f, null, 35, 30, 35 },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "Balanced", "Maintenance", true, "Maintien", 0f, null, 45, 30, 25 },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "HighProtein", "WeightGain", true, "Prise de masse", 0f, null, 50, 20, 30 },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "Mediterranean", "Maintenance", true, "Équilibre méditerranéen", 0f, null, 50, 30, 20 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "diet_plans",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"));

            migrationBuilder.DeleteData(
                table: "diet_plans",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"));

            migrationBuilder.DeleteData(
                table: "diet_plans",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"));

            migrationBuilder.DeleteData(
                table: "diet_plans",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"));
        }
    }
}
