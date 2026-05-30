using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuickDocs.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ModelosAisladosPorUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    CuitCuil = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    Direccion = table.Column<string>(type: "text", nullable: true),
                    Localidad = table.Column<string>(type: "text", nullable: true),
                    FechaAlta = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clientes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Marca = table.Column<string>(type: "text", nullable: true),
                    UnidadMedida = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    NombreFantasia = table.Column<string>(type: "text", nullable: false),
                    Direccion = table.Column<string>(type: "text", nullable: false),
                    TelefonoPrincipal = table.Column<string>(type: "text", nullable: true),
                    TelefonoSecundario = table.Column<string>(type: "text", nullable: true),
                    CondicionIva = table.Column<string>(type: "text", nullable: false),
                    LogoPath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perfiles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PuntoEmision = table.Column<int>(type: "integer", nullable: false),
                    NumeroCorrelativo = table.Column<int>(type: "integer", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: true),
                    ClienteNombre = table.Column<string>(type: "text", nullable: false),
                    Discriminador = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Detalle = table.Column<string>(type: "text", nullable: true),
                    Presupuesto_FechaVencimiento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: true),
                    Descuento = table.Column<decimal>(type: "numeric", nullable: true),
                    Presupuesto_Total = table.Column<decimal>(type: "numeric", nullable: true),
                    Presupuesto_Estado = table.Column<int>(type: "integer", nullable: true),
                    ImporteRecibido = table.Column<decimal>(type: "numeric", nullable: true),
                    FormaPago = table.Column<int>(type: "integer", nullable: true),
                    Recibo_Detalle = table.Column<string>(type: "text", nullable: true),
                    RemitoId = table.Column<int>(type: "integer", nullable: true),
                    PresupuestoId = table.Column<int>(type: "integer", nullable: true),
                    DireccionEntrega = table.Column<string>(type: "text", nullable: true),
                    Remito_Estado = table.Column<int>(type: "integer", nullable: true),
                    FechaEntrega = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Remito_Subtotal = table.Column<decimal>(type: "numeric", nullable: true),
                    Remito_Descuento = table.Column<decimal>(type: "numeric", nullable: true),
                    Remito_Total = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documentos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetallesPresupuesto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PresupuestoId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: true),
                    DescripcionSnapshot = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric", nullable: false),
                    PrecioAplicado = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesPresupuesto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesPresupuesto_Documentos_PresupuestoId",
                        column: x => x.PresupuestoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesPresupuesto_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DetallesRemito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RemitoId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: true),
                    DescripcionSnapshot = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric", nullable: false),
                    PrecioAplicado = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesRemito", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesRemito_Documentos_RemitoId",
                        column: x => x.RemitoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesRemito_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_UsuarioId",
                table: "Clientes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPresupuesto_ItemId",
                table: "DetallesPresupuesto",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPresupuesto_PresupuestoId",
                table: "DetallesPresupuesto",
                column: "PresupuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesRemito_ItemId",
                table: "DetallesRemito",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesRemito_RemitoId",
                table: "DetallesRemito",
                column: "RemitoId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_ClienteId",
                table: "Documentos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_UsuarioId",
                table: "Documentos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_UsuarioId",
                table: "Items",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Perfiles_UsuarioId",
                table: "Perfiles",
                column: "UsuarioId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesPresupuesto");

            migrationBuilder.DropTable(
                name: "DetallesRemito");

            migrationBuilder.DropTable(
                name: "Perfiles");

            migrationBuilder.DropTable(
                name: "Documentos");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
