using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlahiaPos.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPuedeEditarPrecioCarritoToUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PuedeEditarPrecioCarrito",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PuedeEditarPrecioCarrito",
                table: "Usuarios");
        }
    }
}
