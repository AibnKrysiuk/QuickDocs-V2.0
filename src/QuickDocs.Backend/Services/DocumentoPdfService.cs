using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuickDocs.Core.Models;
// using System;
// using System.IO;
// using System.Threading.Tasks;

namespace QuickDocs.Backend.Services
{
    public class DocumentoPdfService
    {
        // ─── 🏛️ MÉTODO CENTRALIZADO Y OPTIMIZADO PARA LA CABECERA ESTÉTICA ───
        private void DibujarCabeceraComun(RowDescriptor row, Perfil perfil, string tituloDocumento, string subtituloInfo)
        {
            // LADO IZQUIERDO: Espacio exclusivo para el Logo corporativo
            var colLogo = row.RelativeItem();
            if (!string.IsNullOrEmpty(perfil.LogoPath) && File.Exists(perfil.LogoPath))
            {
                // Se aplica el alto guardado en el perfil si existe, o un alto estimado (ej. 60)
                colLogo.Height(60).Image(perfil.LogoPath);
            }
            else
            {
                colLogo.Height(60).Placeholder();
            }

            // LADO DERECHO: Datos Identitarios, Fiscales y de Contacto alineados a la derecha
            row.RelativeItem().Column(col =>
            {
                // 1. Tipo de Documento (Ej: "PRESUPUESTO")
                col.Item().AlignRight().Text(tituloDocumento.ToUpper()).FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                
                // 2. Nombre de Fantasía o Razón Social (Aplicando tamaño de fuente del perfil o 13 por defecto)
                string nombreComercial = (perfil.NombreFantasia ?? "SIN NOMBRE COMERCIAL").ToUpper();
                col.Item().AlignRight().Text(nombreComercial).FontSize(13).Bold();

                // 3. Dirección Comercial
                string direccion = !string.IsNullOrWhiteSpace(perfil.Direccion) ? perfil.Direccion : "Dirección no especificada";
                col.Item().AlignRight().Text(direccion).FontSize(10);

                // 4. Localidad / Ubicación
                if (!string.IsNullOrWhiteSpace(perfil.Direccion))
                {
                    col.Item().AlignRight().Text(perfil.Direccion).FontSize(10);
                }

                // 5. CUIT / CUIL
                string cuit = !string.IsNullOrWhiteSpace(perfil.CuitCuil) ? perfil.CuitCuil : "XX-XXXXXXXX-X";
                col.Item().AlignRight().Text($"CUIT: {cuit}").FontSize(10);

                // 6. Teléfonos (Unificados en una sola línea)
                string telefonos = perfil.TelefonoPrincipal ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(perfil.TelefonoSecundario))
                {
                    telefonos += $" - {perfil.TelefonoSecundario}";
                }
                if (!string.IsNullOrWhiteSpace(telefonos))
                {
                    col.Item().AlignRight().Text($"Tel: {telefonos}").FontSize(10);
                }

                // 7. Email de contacto
                string email = !string.IsNullOrWhiteSpace(perfil.EmailContacto) ? perfil.EmailContacto : "Email no especificado";
                col.Item().AlignRight().Text(email).FontSize(10);

                // 📄 Bloque dinámico adicional de información temporal (si existe)
                if (!string.IsNullOrWhiteSpace(subtituloInfo))
                {
                    col.Item().PaddingTop(6).AlignRight().Text(subtituloInfo).FontSize(9).FontColor(Colors.Grey.Darken2);
                }
            });
        }

        // ─── ENDPOINT DE PREVISUALIZACIÓN PARA LA VENTANA DE PERFIL ───
        public byte[] GenerarPdfPruebaCabecera(Perfil perfil)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.Header().Row(row =>
                    {
                        DibujarCabeceraComun(row, perfil, "PRESUPUESTO", string.Empty);
                    });

                    page.Content().PaddingVertical(1.5f, Unit.Centimetre).Column(col =>
                    {
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(20).AlignCenter().Column(c =>
                        {
                            c.Item().Text("Vista Previa del Cuerpo del Documento").Bold().FontSize(12).FontColor(Colors.Blue.Medium);
                            c.Item().PaddingTop(5).Text("Este recuadro simula el espacio donde se listarán tus artículos, renglones, subtotales y firmas según el comprobante que emitas (Presupuesto, Recibo, Remito o Nota de Crédito).").AlignCenter().FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ─── DOCUMENTO 1: PRESUPUESTO ───
        public byte[] GenerarPresupuestoPdf(Presupuesto presupuesto, Perfil perfil, Cliente cliente)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(1, Unit.Centimetre);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Row(row =>
                    {
                        string info = $"Fecha: {presupuesto.FechaEmision.ToLocalTime():dd/MM/yyyy}\n" +
                                     $"Estado: {presupuesto.Estado}";
                        DibujarCabeceraComun(row, perfil, "PRESUPUESTO", info);
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // 🎯 BLOQUE DE CLIENTE ELÁSTICO CONDICIONAL
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Background(Colors.Grey.Lighten3).Column(c =>
                        {
                            c.Item().Text("CLIENTE:").Bold().FontSize(11);
                            
                            // Al garantizar un objeto Cliente real, leemos directo de sus propiedades
                            c.Item().Text($"Nombre: {cliente?.Nombre ?? "Consumidor Final / Público General"}");
                            
                            if (!string.IsNullOrWhiteSpace(cliente?.CuitCuil))
                            {
                                c.Item().Text($"CUIT/CUIL: {cliente.CuitCuil}");
                            }
                            
                            if (!string.IsNullOrWhiteSpace(cliente?.Direccion))
                            {
                                c.Item().Text($"Dirección: {cliente.Direccion}");
                            }
                        });

                        col.Item().PaddingTop(1, Unit.Centimetre);

                        col.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);  
                                columns.RelativeColumn();      
                                columns.ConstantColumn(80);  
                                columns.ConstantColumn(80);  
                            });

                            tabla.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Cant.").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Descripción").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignRight().Text("P. Unit").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignRight().Text("Importe").FontColor(Colors.White).Bold();
                            });

                            foreach (var detalle in presupuesto.Detalles)
                            {
                                tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{detalle.Cantidad:G}");
                                tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(detalle.DescripcionSnapshot);
                                tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${detalle.PrecioAplicado:N2}");
                                tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${detalle.Importe:N2}");
                            }
                        });

                        // BLOQUE INFERIOR DE TOTALES Y VALIDEZ
                        col.Item().PaddingTop(20).Row(rowTotales =>
                        {
                            // Lado izquierdo inferior: Frase dinámica de validez
                            int diasCalculados = (presupuesto.FechaVencimiento.Date - presupuesto.FechaEmision.Date).Days;
                            if (diasCalculados <= 0) diasCalculados = 15; // Contingencia por horas UTC

                            rowTotales.RelativeItem().AlignLeft().AlignBottom().Column(colValidez =>
                            {
                                colValidez.Item().Text($"📌 Documento válido por {diasCalculados} días.").FontSize(10).Italic().Bold().FontColor(Colors.Grey.Darken3);
                            });

                            // Lado derecho inferior: Totales numéricos
                            rowTotales.ConstantItem(200).Column(totalesCol =>
                            {
                                totalesCol.Item().Row(r => { r.RelativeItem().Text("Subtotal:"); r.ConstantItem(80).AlignRight().Text($"${presupuesto.Subtotal:N2}"); });
                                if (presupuesto.Descuento > 0)
                                {
                                    totalesCol.Item().Row(r => { r.RelativeItem().Text("Descuento:").FontColor(Colors.Red.Medium); r.ConstantItem(80).AlignRight().Text($"-${presupuesto.Descuento:N2}").FontColor(Colors.Red.Medium); });
                                }
                                totalesCol.Item().Row(r => { r.RelativeItem().Text("TOTAL:").Bold().FontSize(12); r.ConstantItem(80).AlignRight().Text($"${presupuesto.Total:N2}").Bold().FontSize(12); });
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
                });
            }).GeneratePdf();
        }

        // ─── DOCUMENTO 2: RECIBO ───
        public async Task<byte[]> GenerarReciboPdfAsync(Recibo recibo, Perfil perfil, Cliente cliente)
        {
            return await Task.Run(() =>
            {
                return Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(1, Unit.Centimetre);
                        page.Size(PageSizes.A4);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                        page.Header().Row(row =>
                        {
                            string info = $"Fecha: {recibo.FechaEmision.ToLocalTime():dd/MM/yyyy}\n" +
                                         (recibo.RemitoId.HasValue ? $"Remito Asociado: #{recibo.RemitoId.Value}\n" : "") +
                                         $"Comprobante: #{recibo.Id.ToString("D8")}";
                            DibujarCabeceraComun(row, perfil, "RECIBO DE PAGO", info);
                        });

                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                        {
                            col.Item().Border(2).BorderColor(Colors.Green.Darken3).Background(Colors.Green.Lighten5).Padding(15).Row(row =>
                            {
                                row.RelativeItem().AlignMiddle().Text("RECIBIMOS LA SUMA DE:").Bold().FontSize(12).FontColor(Colors.Green.Darken4);
                                row.ConstantItem(150).AlignRight().AlignMiddle().Background(Colors.White).Border(1).BorderColor(Colors.Green.Darken3).Padding(5).AlignCenter().Text($"${recibo.ImporteRecibido:N2}").FontSize(16).Bold().FontColor(Colors.Green.Darken4);
                            });

                            col.Item().PaddingTop(0.5f, Unit.Centimetre);

                            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Background(Colors.Grey.Lighten3).Column(c =>
                            {
                                c.Item().Text("DE / PARA:").Bold().FontSize(11);
                                c.Item().Text($"Nombre: {cliente.Nombre}");
                                c.Item().Text($"Id Cliente: {(cliente.Id > 0 ? cliente.Id.ToString() : "No registrado")}");
                                if (!string.IsNullOrEmpty(cliente.CuitCuil) && cliente.CuitCuil != "00-00000000-0")
                                {
                                    c.Item().Text($"CUIT/CUIL: {cliente.CuitCuil}");
                                    c.Item().Text($"Dirección: {cliente.Direccion}");
                                }
                            });

                            col.Item().PaddingTop(1, Unit.Centimetre);

                            col.Item().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(det =>
                                {
                                    det.Item().Row(r => { r.ConstantItem(120).Text("En Concepto de:").Bold(); r.RelativeItem().Text(string.IsNullOrEmpty(recibo.Detalle) ? "Pago / Entrega general a cuenta." : recibo.Detalle); });
                                    det.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                    det.Item().Row(r => { r.ConstantItem(120).Text("Forma de Pago:").Bold(); string formaTexto = recibo.FormaPago.ToString(); r.RelativeItem().Text(formaTexto).Bold().FontColor(Colors.Blue.Darken3); });
                                });

                            col.Item().PaddingTop(3, Unit.Centimetre).Row(row =>
                            {
                                row.RelativeItem();
                                row.ConstantItem(200).Column(firmaCol =>
                                {
                                    firmaCol.Item().BorderTop(1).BorderColor(Colors.Grey.Darken1).PaddingTop(5).AlignCenter().Text("Firma y Aclaración").FontSize(9);
                                    firmaCol.Item().AlignCenter().Text(perfil.NombreFantasia).FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
                    });
                }).GeneratePdf();
            });
        }

        // ─── DOCUMENTO 3: REMITO ───
        public async Task<byte[]> GenerarRemitoPdfAsync(Remito remito, Perfil perfil, Cliente cliente)
        {
            return await Task.Run(() =>
            {
                return Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(1, Unit.Centimetre);
                        page.Size(PageSizes.A4);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                        page.Header().Row(row =>
                        {
                            string info = $"Documento No Válido como Factura\nFecha Emisión: {remito.FechaEmision.ToLocalTime():dd/MM/yyyy}\n" +
                                         (remito.PresupuestoId.HasValue ? $"Origen Presupuesto: #{remito.PresupuestoId.Value}\n" : "") +
                                         $"Número: #{remito.Id.ToString("D8")}";
                            DibujarCabeceraComun(row, perfil, "REMITO", info);
                        });

                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Background(Colors.Grey.Lighten4).Column(c =>
                                {
                                    c.Item().Text("DESTINATARIO:").Bold().FontSize(11).FontColor(Colors.Grey.Darken3);
                                    c.Item().Text($"Nombre: {cliente.Nombre}");
                                    c.Item().Text($"CUIT/CUIL: {cliente.CuitCuil}");
                                    c.Item().Text($"Teléfono Ref: {cliente.Direccion}");
                                });

                                row.ConstantItem(15);

                                row.RelativeItem().Border(1).BorderColor(Colors.Orange.Darken2).Padding(10).Background(Colors.Orange.Lighten5).Column(d =>
                                {
                                    d.Item().Text("LUGAR DE ENTREGA:").Bold().FontSize(11).FontColor(Colors.Orange.Darken3);
                                    d.Item().Text(string.IsNullOrEmpty(remito.DireccionEntrega) ? "Se retira por el local del emisor" : remito.DireccionEntrega).Bold();
                                    d.Item().PaddingTop(5).Text($"Estado del Envío: {remito.Estado}").FontSize(9).Italic();
                                });
                            });

                            col.Item().PaddingTop(0.8f, Unit.Centimetre);

                            col.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(columns => { columns.ConstantColumn(60); columns.RelativeColumn(); columns.ConstantColumn(100); });
                                tabla.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("Cant.").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("Descripción del Artículo").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("Control").FontColor(Colors.White).Bold();
                                });

                                foreach (var detalle in remito.Detalles)
                                {
                                    tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{detalle.Cantidad:G}");
                                    tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(detalle.DescripcionSnapshot);
                                    tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("[  ] Ok").FontColor(Colors.Grey.Lighten1);
                                }
                            });

                            col.Item().PaddingTop(2.5f, Unit.Centimetre).Row(row =>
                            {
                                row.RelativeItem().Column(nota =>
                                {
                                    nota.Item().Text("Notas del Transportista:").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                                    nota.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                    nota.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                });
                                row.ConstantItem(40);
                                row.RelativeItem().Column(firmaCol =>
                                {
                                    firmaCol.Item().BorderTop(1).BorderColor(Colors.Grey.Darken1).PaddingTop(5).AlignCenter().Text("Firma de Quien Recibe").FontSize(10).Bold();
                                    firmaCol.Item().PaddingTop(15).Text("Aclaración: ___________________________").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    firmaCol.Item().PaddingTop(8).Text("DNI/CI:       ___________________________").FontSize(9).FontColor(Colors.Grey.Darken2);
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
                    });
                }).GeneratePdf();
            });
        }

        // ─── DOCUMENTO 4: NOTA DE CRÉDITO ───
        public async Task<byte[]> GenerarNotaCreditoPdfAsync(NotaCredito notaCredito, Perfil perfil, Cliente cliente)
        {
            return await Task.Run(() =>
            {
                return Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(1, Unit.Centimetre);
                        page.Size(PageSizes.A4);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                        page.Header().Row(row =>
                        {
                            string info = $"Fecha Emisión: {notaCredito.FechaEmision.ToLocalTime():dd/MM/yyyy}\n" +
                                         $"Vence (Saldo Válido): {notaCredito.FechaVencimiento.ToLocalTime():dd/MM/yyyy}\n" +
                                         $"Comprobante: #{notaCredito.Id.ToString("D8")}";
                            DibujarCabeceraComun(row, perfil, "NOTA DE CRÉDITO", info);
                        });

                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                        {
                            col.Item().Border(2).BorderColor(Colors.Red.Darken3).Background(Colors.Red.Lighten5).Padding(15).Row(row =>
                            {
                                row.RelativeItem().AlignMiddle().Text("CRÉDITO A FAVOR DEL CLIENTE:").Bold().FontSize(12).FontColor(Colors.Red.Darken4);
                                row.ConstantItem(150).AlignRight().AlignMiddle().Background(Colors.White).Border(1).BorderColor(Colors.Red.Darken3).Padding(5).AlignCenter().Text($"${notaCredito.Total:N2}").FontSize(16).Bold().FontColor(Colors.Red.Darken4);
                            });

                            col.Item().PaddingTop(0.5f, Unit.Centimetre);

                            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Background(Colors.Grey.Lighten3).Column(c =>
                            {
                                c.Item().Text("CLIENTE / BENEFICIARIO:").Bold().FontSize(11).FontColor(Colors.Grey.Darken3);
                                c.Item().Text($"Nombre: {cliente.Nombre}");
                                if (!string.IsNullOrEmpty(cliente.CuitCuil) && cliente.CuitCuil != "00-00000000-0")
                                {
                                    c.Item().Text($"CUIT/CUIL: {cliente.CuitCuil}");
                                    c.Item().Text($"Dirección: {cliente.Direccion}");
                                }
                            });

                            col.Item().PaddingTop(1, Unit.Centimetre);

                            col.Item().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(det =>
                            {
                                det.Item().Row(r => { r.ConstantItem(140).Text("Concepto / Motivo:").Bold(); r.RelativeItem().Text(string.IsNullOrEmpty(notaCredito.Detalle) ? "Devolución de mercadería / Ajuste de saldo." : notaCredito.Detalle); });
                                det.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                det.Item().Row(r => { r.ConstantItem(140).Text("Estado del Comprobante:").Bold(); r.RelativeItem().Text(notaCredito.Estado.ToString()).Bold().FontColor(Colors.Green.Darken3); });
                            });

                            col.Item().PaddingTop(3, Unit.Centimetre).Row(row =>
                            {
                                row.RelativeItem();
                                row.ConstantItem(200).Column(firmaCol =>
                                {
                                    firmaCol.Item().BorderTop(1).BorderColor(Colors.Grey.Darken1).PaddingTop(5).AlignCenter().Text("Firma Autorizada").FontSize(9);
                                    firmaCol.Item().AlignCenter().Text(perfil.NombreFantasia).FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
                    });
                }).GeneratePdf();
            });
        }
    }
}