using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickDocs.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDiasValidezYDescuentoPresupuesto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoDescuento",
                table: "Documentos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Presupuesto_DiasValidez",
                table: "Documentos",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotivoDescuento",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Presupuesto_DiasValidez",
                table: "Documentos");
        }
    }
}
