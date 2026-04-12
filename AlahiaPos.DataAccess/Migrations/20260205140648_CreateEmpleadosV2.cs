using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlahiaPos.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateEmpleadosV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Citas_Empleados_IdEmpleado",
                table: "Citas");

            migrationBuilder.DropForeignKey(
                name: "FK_Empleados_Empresas_IdEmpresa",
                table: "Empleados");

            migrationBuilder.DropForeignKey(
                name: "FK_HorariosEstilistas_Empleados_IdEmpleado",
                table: "HorariosEstilistas");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Empleados_IdEmpleado",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Empleados",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "FechaInseccion",
                table: "Empleados");

            migrationBuilder.RenameTable(
                name: "Empleados",
                newName: "EmpleadosP");

            migrationBuilder.RenameIndex(
                name: "IX_Empleados_IdEmpresa",
                table: "EmpleadosP",
                newName: "IX_EmpleadosP_IdEmpresa");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmpleadosP",
                table: "EmpleadosP",
                column: "IdEmpleados");

            migrationBuilder.AddForeignKey(
                name: "FK_Citas_EmpleadosP_IdEmpleado",
                table: "Citas",
                column: "IdEmpleado",
                principalTable: "EmpleadosP",
                principalColumn: "IdEmpleados",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmpleadosP_Empresas_IdEmpresa",
                table: "EmpleadosP",
                column: "IdEmpresa",
                principalTable: "Empresas",
                principalColumn: "IdEmpresa",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HorariosEstilistas_EmpleadosP_IdEmpleado",
                table: "HorariosEstilistas",
                column: "IdEmpleado",
                principalTable: "EmpleadosP",
                principalColumn: "IdEmpleados",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_EmpleadosP_IdEmpleado",
                table: "Usuarios",
                column: "IdEmpleado",
                principalTable: "EmpleadosP",
                principalColumn: "IdEmpleados",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Citas_EmpleadosP_IdEmpleado",
                table: "Citas");

            migrationBuilder.DropForeignKey(
                name: "FK_EmpleadosP_Empresas_IdEmpresa",
                table: "EmpleadosP");

            migrationBuilder.DropForeignKey(
                name: "FK_HorariosEstilistas_EmpleadosP_IdEmpleado",
                table: "HorariosEstilistas");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_EmpleadosP_IdEmpleado",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmpleadosP",
                table: "EmpleadosP");

            migrationBuilder.RenameTable(
                name: "EmpleadosP",
                newName: "Empleados");

            migrationBuilder.RenameIndex(
                name: "IX_EmpleadosP_IdEmpresa",
                table: "Empleados",
                newName: "IX_Empleados_IdEmpresa");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInseccion",
                table: "Empleados",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Empleados",
                table: "Empleados",
                column: "IdEmpleados");

            migrationBuilder.AddForeignKey(
                name: "FK_Citas_Empleados_IdEmpleado",
                table: "Citas",
                column: "IdEmpleado",
                principalTable: "Empleados",
                principalColumn: "IdEmpleados",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Empleados_Empresas_IdEmpresa",
                table: "Empleados",
                column: "IdEmpresa",
                principalTable: "Empresas",
                principalColumn: "IdEmpresa",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HorariosEstilistas_Empleados_IdEmpleado",
                table: "HorariosEstilistas",
                column: "IdEmpleado",
                principalTable: "Empleados",
                principalColumn: "IdEmpleados",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Empleados_IdEmpleado",
                table: "Usuarios",
                column: "IdEmpleado",
                principalTable: "Empleados",
                principalColumn: "IdEmpleados",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
