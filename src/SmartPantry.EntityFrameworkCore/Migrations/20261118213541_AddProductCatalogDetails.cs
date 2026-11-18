using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPantry.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCatalogDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "AppProducts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "Allergens",
                table: "AppProducts",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Categories",
                table: "AppProducts",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "AppProducts",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ingredients",
                table: "AppProducts",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NovaGroup",
                table: "AppProducts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutriScore",
                table: "AppProducts",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_Barcode",
                table: "AppProducts",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppProducts_Barcode",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "Allergens",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "Categories",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "Ingredients",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "NovaGroup",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "NutriScore",
                table: "AppProducts");

            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "AppProducts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
