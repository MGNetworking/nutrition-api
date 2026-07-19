using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "food_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    off_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    calories_per_100g = table.Column<float>(type: "real", nullable: false),
                    proteins_per_100g = table.Column<int>(type: "integer", nullable: false),
                    carbs_per_100g = table.Column<int>(type: "integer", nullable: false),
                    fats_per_100g = table.Column<int>(type: "integer", nullable: false),
                    allergens_tags = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'"),
                    cached_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_food_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    keycloak_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    activity_level = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    height = table.Column<float>(type: "real", nullable: false),
                    allergies = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'"),
                    dietary_preferences = table.Column<List<string>>(type: "text[]", nullable: false, defaultValueSql: "'{}'"),
                    subscription_tier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Free"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "diet_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_template = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    diet_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    goal = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    target_weight = table.Column<float>(type: "real", nullable: false),
                    macro_protein_pct = table.Column<int>(type: "integer", nullable: false),
                    macro_carb_pct = table.Column<int>(type: "integer", nullable: false),
                    macro_fat_pct = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diet_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_diet_plans_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "diets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    diet_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    goal = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    target_weight = table.Column<float>(type: "real", nullable: false),
                    calorie_target = table.Column<int>(type: "integer", nullable: false),
                    macro_protein_pct = table.Column<int>(type: "integer", nullable: false),
                    macro_carb_pct = table.Column<int>(type: "integer", nullable: false),
                    macro_fat_pct = table.Column<int>(type: "integer", nullable: false),
                    diet_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diets", x => x.id);
                    table.ForeignKey(
                        name: "fk_diets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    meal_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_saved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meals", x => x.id);
                    table.ForeignKey(
                        name: "fk_meals_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_food_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    food_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_food_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_saved_food_items_food_items_food_item_id",
                        column: x => x.food_item_id,
                        principalTable: "food_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_saved_food_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weight_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight = table.Column<float>(type: "real", nullable: false),
                    measured_at = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weight_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_weight_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meal_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    meal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    food_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<float>(type: "real", nullable: false),
                    nutrition_calories = table.Column<float>(type: "real", nullable: false),
                    nutrition_proteins = table.Column<int>(type: "integer", nullable: false),
                    nutrition_carbs = table.Column<int>(type: "integer", nullable: false),
                    nutrition_fats = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meal_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_meal_items_food_items_food_item_id",
                        column: x => x.food_item_id,
                        principalTable: "food_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_meal_items_meals_meal_id",
                        column: x => x.meal_id,
                        principalTable: "meals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_diet_plans_is_template",
                table: "diet_plans",
                column: "is_template");

            migrationBuilder.CreateIndex(
                name: "ix_diet_plans_user_id",
                table: "diet_plans",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_diets_diet_status",
                table: "diets",
                column: "diet_status");

            migrationBuilder.CreateIndex(
                name: "ix_diets_user_id",
                table: "diets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_food_items_name",
                table: "food_items",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_food_items_off_id",
                table: "food_items",
                column: "off_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_meal_items_food_item_id",
                table: "meal_items",
                column: "food_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_meal_items_meal_id",
                table: "meal_items",
                column: "meal_id");

            migrationBuilder.CreateIndex(
                name: "ix_meals_user_id",
                table: "meals",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_saved_food_items_food_item_id",
                table: "saved_food_items",
                column: "food_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_saved_food_items_user_id_food_item_id",
                table: "saved_food_items",
                columns: new[] { "user_id", "food_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_keycloak_id",
                table: "users",
                column: "keycloak_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weight_entries_measured_at",
                table: "weight_entries",
                column: "measured_at");

            migrationBuilder.CreateIndex(
                name: "ix_weight_entries_user_id",
                table: "weight_entries",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diet_plans");

            migrationBuilder.DropTable(
                name: "diets");

            migrationBuilder.DropTable(
                name: "meal_items");

            migrationBuilder.DropTable(
                name: "saved_food_items");

            migrationBuilder.DropTable(
                name: "weight_entries");

            migrationBuilder.DropTable(
                name: "meals");

            migrationBuilder.DropTable(
                name: "food_items");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
