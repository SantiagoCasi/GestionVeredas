using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaVeredas.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposSuelo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Medida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Disponible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposSuelo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Paquetes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paquetes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paquetes_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Veredas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Calle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntreCalle1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntreCalle2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Altura = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Ubicacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaReclamo = table.Column<DateTime>(type: "date", nullable: true),
                    FechaRelevo = table.Column<DateTime>(type: "date", nullable: true),
                    Fotos = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Prioridad = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ACargoFrentista = table.Column<bool>(type: "bit", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: true),
                    PaqueteId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veredas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Veredas_Paquetes_PaqueteId",
                        column: x => x.PaqueteId,
                        principalTable: "Paquetes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Veredas_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mediciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VeredaId = table.Column<int>(type: "int", nullable: false),
                    TipoSueloId = table.Column<int>(type: "int", nullable: false),
                    Sector = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Largo = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Ancho = table.Column<decimal>(type: "decimal(6,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mediciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mediciones_TiposSuelo_TipoSueloId",
                        column: x => x.TipoSueloId,
                        principalTable: "TiposSuelo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mediciones_Veredas_VeredaId",
                        column: x => x.VeredaId,
                        principalTable: "Veredas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mediciones_TipoSueloId",
                table: "Mediciones",
                column: "TipoSueloId");

            migrationBuilder.CreateIndex(
                name: "IX_Mediciones_VeredaId",
                table: "Mediciones",
                column: "VeredaId");

            migrationBuilder.CreateIndex(
                name: "IX_Paquetes_ProveedorId",
                table: "Paquetes",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Veredas_PaqueteId",
                table: "Veredas",
                column: "PaqueteId");

            migrationBuilder.CreateIndex(
                name: "IX_Veredas_ProveedorId",
                table: "Veredas",
                column: "ProveedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mediciones");

            migrationBuilder.DropTable(
                name: "TiposSuelo");

            migrationBuilder.DropTable(
                name: "Veredas");

            migrationBuilder.DropTable(
                name: "Paquetes");

            migrationBuilder.DropTable(
                name: "Proveedores");
        }
    }
}
