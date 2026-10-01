using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaVeredas.Migrations
{
    /// <inheritdoc />
    // SPEC-001: roturas (pozos) de la vereda con su tipo de suelo, fórmula y totales en Veredas.
    // Borra la tabla Mediciones sin convertir los datos: eran de prueba (D-27).
    public partial class RoturasEnVeredas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VeredaId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Medidas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TipoSueloId = table.Column<int>(type: "int", nullable: false),
                    SubtotalM2 = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roturas_TiposSuelo_TipoSueloId",
                        column: x => x.TipoSueloId,
                        principalTable: "TiposSuelo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Roturas_Veredas_VeredaId",
                        column: x => x.VeredaId,
                        principalTable: "Veredas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roturas_TipoSueloId",
                table: "Roturas",
                column: "TipoSueloId");

            migrationBuilder.CreateIndex(
                name: "IX_Roturas_VeredaId",
                table: "Roturas",
                column: "VeredaId");

            migrationBuilder.AddColumn<string>(
                name: "Medicion",
                table: "Veredas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalM2",
                table: "Veredas",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TieneCordon",
                table: "Veredas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MedicionCordon",
                table: "Veredas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCordonM3",
                table: "Veredas",
                type: "decimal(10,3)",
                nullable: true);

            migrationBuilder.DropTable(
                name: "Mediciones");
        }

        /// <inheritdoc />
        // Vuelve a crear Mediciones vacía, con su estructura anterior. No recupera datos.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Roturas");

            migrationBuilder.DropColumn(
                name: "Medicion",
                table: "Veredas");

            migrationBuilder.DropColumn(
                name: "TotalM2",
                table: "Veredas");

            migrationBuilder.DropColumn(
                name: "TieneCordon",
                table: "Veredas");

            migrationBuilder.DropColumn(
                name: "MedicionCordon",
                table: "Veredas");

            migrationBuilder.DropColumn(
                name: "TotalCordonM3",
                table: "Veredas");

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
        }
    }
}
