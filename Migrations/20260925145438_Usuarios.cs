using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaVeredas.Migrations
{
    /// <inheritdoc />
    public partial class Usuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    UsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsNombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UsApellido = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UsEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsContrasena = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsActivo = table.Column<bool>(type: "bit", nullable: false),
                    token_recovery = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.UsId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_UsEmail",
                table: "Usuarios",
                column: "UsEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
