using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DestekAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAlpemixKoduToSikayet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlpemixKodu",
                table: "Sikayetler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlpemixKodu",
                table: "Sikayetler");
        }
    }
}
