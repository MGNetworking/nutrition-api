using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueWeightEntryPerUserAndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_weight_entries_user_id",
                table: "weight_entries");

            migrationBuilder.CreateIndex(
                name: "ix_weight_entries_user_id_measured_at",
                table: "weight_entries",
                columns: new[] { "user_id", "measured_at" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_weight_entries_user_id_measured_at",
                table: "weight_entries");

            migrationBuilder.CreateIndex(
                name: "ix_weight_entries_user_id",
                table: "weight_entries",
                column: "user_id");
        }
    }
}
