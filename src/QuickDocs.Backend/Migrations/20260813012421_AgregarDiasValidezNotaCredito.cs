using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickDocs.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDiasValidezNotaCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiasValidez",
                table: "Documentos",
                type: "INTEGER",
                nullable: true,
                defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiasValidez",
                table: "Documentos");
        }
    }
}