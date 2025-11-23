using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DestekAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCozumAciklamasiToSikayet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AlpemixKodu",
                table: "Sikayetler",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CozumAciklamasi",
                table: "Sikayetler",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CozumAciklamasi",
                table: "Sikayetler");

            migrationBuilder.AlterColumn<string>(
                name: "AlpemixKodu",
                table: "Sikayetler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
