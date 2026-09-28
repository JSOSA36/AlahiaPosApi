using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlahiaPos.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPuedeAnularFacturaToUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PuedeAnularFactura",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PuedeAnularFactura",
                table: "Usuarios");
        }
    }
}
