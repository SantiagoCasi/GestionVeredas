using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SistemaVeredas.Data;

#nullable disable

namespace SistemaVeredas.Migrations
{
    /// <inheritdoc />
    // RF-VER-18: código numérico de la vereda para el usuario final (distinto del Id interno), único.
    // Las veredas que ya existen reciben 1, 2, 3… según su orden de carga (por Id).
    [DbContext(typeof(AppDbContext))]
    [Migration("20261006140000_CodigoDeVereda")]
    public partial class CodigoDeVereda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Codigo",
                table: "Veredas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Antes de crear el índice único: si no, todas las veredas existentes quedarían con 0.
            migrationBuilder.Sql(
                @"UPDATE v SET v.Codigo = n.Numero
                  FROM Veredas v
                  INNER JOIN (SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS Numero FROM Veredas) n ON n.Id = v.Id;");

            migrationBuilder.CreateIndex(
                name: "IX_Veredas_Codigo",
                table: "Veredas",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Veredas_Codigo",
                table: "Veredas");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Veredas");
        }
    }
}
