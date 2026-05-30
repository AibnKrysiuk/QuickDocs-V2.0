using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuickDocs.Core.Models;
using System;

namespace QuickDocs.Backend.Services
{
    public class DocumentoPdfService
    {
        public byte[] GenerarPresupuestoPdf(Presupuesto presupuesto, Perfil perfil, Cliente cliente)
        {
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(1, Unit.Centimetre);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    // 1. EL ENCABEZADO
                    page.Header().Row(row =>
                    {
                        // Lado Izquierdo: Datos del Perfil
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(perfil.NombreFantasia).FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                            col.Item().Text($"Dirección: {perfil.Direccion}");
                            col.Item().Text($"Teléfono: {perfil.TelefonoPrincipal}");
                        });

                        // Lado Derecho: Datos del Presupuesto (Alineados correctamente)
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("PRESUPUESTO").FontSize(18).Bold();
                            col.Item().Text($"Fecha: {presupuesto.FechaEmision.ToLocalTime():dd/MM/yyyy}");
                            col.Item().Text($"Vence: {presupuesto.FechaVencimiento.ToLocalTime():dd/MM/yyyy}");
                            col.Item().Text($"Estado: {presupuesto.Estado}").Bold();
                        });
                    });

                    // 2. EL CONTENIDO
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // Recuadro del Cliente
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Background(Colors.Grey.Lighten3).Column(c =>
                        {
                            c.Item().Text("CLIENTE:").Bold().FontSize(11);
                            c.Item().Text($"Nombre: {cliente.Nombre}");
                            c.Item().Text($"CUIT/CUIL: {cliente.CuitCuil}");
                            c.Item().Text($"Dirección: {cliente.Direccion}");
                        });

                        col.Item().PaddingTop(1, Unit.Centimetre);

                        // La Tabla de Renglones
                        col.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);  // Cantidad
                                columns.RelativeColumn();      // Descripción
                                columns.ConstantColumn(80);  // Precio Unitario
                                columns.ConstantColumn(80);  // Importe Total
                            });

                            // Encabezado de la Tabla
                            tabla.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Cant.").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Descripción").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignRight().Text("P. Unit").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignRight().Text("Importe").FontColor(Colors.White).Bold();
                            });

                            // Renglones
                            foreach (var detalle in presupuesto.Detalles)
                            {
                                tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{detalle.Cantidad:G}");
                                tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(detalle.DescripcionSnapshot);
                                tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${detalle.PrecioAplicado:N2}");
                                tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${detalle.Importe:N2}");
                            }
                        });

                        // Bloque de Totales
                        col.Item().AlignRight().PaddingTop(20).Width(200).Column(totalesCol =>
                        {
                            totalesCol.Item().Row(r => 
                            { 
                                r.RelativeItem().Text("Subtotal:"); 
                                r.ConstantItem(80).AlignRight().Text($"${presupuesto.Subtotal:N2}"); 
                            });
                            
                            if (presupuesto.Descuento > 0)
                            {
                                totalesCol.Item().Row(r => 
                                { 
                                    r.RelativeItem().Text("Descuento:").FontColor(Colors.Red.Medium); 
                                    r.ConstantItem(80).AlignRight().Text($"-${presupuesto.Descuento:N2}").FontColor(Colors.Red.Medium); 
                                });
                            }
                            
                            totalesCol.Item().Row(r => 
                            { 
                                r.RelativeItem().Text("TOTAL:").Bold().FontSize(12); 
                                r.ConstantItem(80).AlignRight().Text($"${presupuesto.Total:N2}").Bold().FontSize(12); 
                            });
                        });
                    });

                    // 3. EL PIE DE PÁGINA
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / "); // ¡Cambiado de Text a Span!
                        x.TotalPages();
                    });
                });
            });

            return documento.GeneratePdf();
        }
    
        // 🎯 NUEVO MÉTODO PARA GENERAR EL PDF DEL RECIBO
        public async Task<byte[]> GenerarReciboPdfAsync(Recibo recibo, Perfil perfil, Cliente cliente)
        {
            // Usamos Task.Run para que corra de forma asíncrona sin bloquear el hilo principal de la API
            return await Task.Run(() =>
            {
                var documento = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(1, Unit.Centimetre);
                        page.Size(PageSizes.A4);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                        // 1. EL ENCABEZADO (Mismo diseño azul que el presupuesto)
                        page.Header().Row(row =>
                        {
                            // Lado Izquierdo: Datos del Perfil (Tu tía)
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(perfil.NombreFantasia).FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                                col.Item().Text($"Dirección: {perfil.Direccion}");
                                col.Item().Text($"Teléfono: {perfil.TelefonoPrincipal}");
                            });

                            // Lado Derecho: Datos del Recibo
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text("RECIBO DE PAGO").FontSize(18).Bold().FontColor(Colors.Green.Darken3);
                                col.Item().Text($"Fecha: {recibo.FechaEmision.ToLocalTime():dd/MM/yyyy}");
                                if (recibo.RemitoId.HasValue)
                                {
                                    col.Item().Text($"Remito Asociado: #{recibo.RemitoId.Value}");
                                }
                                col.Item().Text($"Comprobante: #{recibo.Id.ToString("D8")}"); // Formato 00000001
                            });
                        });

                        // 2. EL CONTENIDO
                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                        {
                            // Bloque Destacado: El Importe En Grande
                            col.Item().Border(2).BorderColor(Colors.Green.Darken3).Background(Colors.Green.Lighten5).Padding(15).Row(row =>
                            {
                                row.RelativeItem().AlignMiddle().Text("RECIBIMOS LA SUMA DE:").Bold().FontSize(12).FontColor(Colors.Green.Darken4);
                                row.ConstantItem(150).AlignRight().AlignMiddle().Background(Colors.White).Border(1).BorderColor(Colors.Green.Darken3).Padding(5).AlignCenter()
                                    .Text($"${recibo.ImporteRecibido:N2}").FontSize(16).Bold().FontColor(Colors.Green.Darken4);
                            });

                            col.Item().PaddingTop(0.5f, Unit.Centimetre);

                            // Recuadro del Cliente (Mismo estilo gris que el presupuesto para mantener coherencia)
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

                            // Detalles del Recibo (Concepto y Forma de Pago)
                            col.Item().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(det =>
                            {
                                det.Item().Row(r =>
                                {
                                    r.ConstantItem(120).Text("En Concepto de:").Bold();
                                    r.RelativeItem().Text(string.IsNullOrEmpty(recibo.Detalle) ? "Pago / Entrega general a cuenta." : recibo.Detalle);
                                });

                                det.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                det.Item().Row(r =>
                                {
                                    r.ConstantItem(120).Text("Forma de Pago:").Bold();
                                    // Mapea el Enum a texto (podes personalizar el switch según tus Enums reales)
                                    string formaTexto = recibo.FormaPago.ToString(); 
                                    r.RelativeItem().Text(formaTexto).Bold().FontColor(Colors.Blue.Darken3);
                                });
                            });

                            // Espacio para la firma (Muy importante en los recibos físicos que imprima tu tía)
                            col.Item().PaddingTop(3, Unit.Centimetre).Row(row =>
                            {
                                row.RelativeItem(); // Espacio vacío izquierdo
                                row.ConstantItem(200).Column(firmaCol =>
                                {
                                    firmaCol.Item().BorderTop(1).BorderColor(Colors.Grey.Darken1).PaddingTop(5).AlignCenter().Text("Firma y Aclaración").FontSize(9);
                                    firmaCol.Item().AlignCenter().Text(perfil.NombreFantasia).FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                                });
                            });
                        });

                        // 3. EL PIE DE PÁGINA
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });

                return documento.GeneratePdf();
            });
        }
    
        // 🎯 MÉTODO PARA GENERAR EL PDF DEL REMITO (Logística y Entrega)
        public async Task<byte[]> GenerarRemitoPdfAsync(Remito remito, Perfil perfil, Cliente cliente)
        {
            return await Task.Run(() =>
            {
                var documento = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(1, Unit.Centimetre);
                        page.Size(PageSizes.A4);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                        // 1. EL ENCABEZADO (Diseño en tonos Grafito/Gris Oscuro)
                        page.Header().Row(row =>
                        {
                            // Lado Izquierdo: Datos de tu tía (Emisor)
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(perfil.NombreFantasia).FontSize(20).Bold().FontColor(Colors.Grey.Darken3);
                                col.Item().Text($"Dirección: {perfil.Direccion}");
                                col.Item().Text($"Teléfono: {perfil.TelefonoPrincipal}");
                            });

                            // Lado Derecho: Datos del Remito
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text("REMITO").FontSize(22).Bold().FontColor(Colors.Grey.Darken4);
                                col.Item().Text($"Documento No Válido como Factura").FontSize(8).Italic().FontColor(Colors.Red.Medium);
                                col.Item().Text($"Fecha Emisión: {remito.FechaEmision.ToLocalTime():dd/MM/yyyy}");
                                if (remito.PresupuestoId.HasValue)
                                {
                                    col.Item().Text($"Origen Presupuesto: #{remito.PresupuestoId.Value}");
                                }
                                col.Item().Text($"Número: #{remito.Id.ToString("D8")}");
                            });
                        });

                        // 2. EL CONTENIDO
                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                        {
                            // Fila con dos columnas: Datos del Cliente (izq) y Datos de Entrega (der)
                            col.Item().Row(row =>
                            {
                                // Datos del Cliente
                                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Background(Colors.Grey.Lighten4).Column(c =>
                                {
                                    c.Item().Text("DESTINATARIO:").Bold().FontSize(11).FontColor(Colors.Grey.Darken3);
                                    c.Item().Text($"Nombre: {cliente.Nombre}");
                                    c.Item().Text($"CUIT/CUIL: {cliente.CuitCuil}");
                                    c.Item().Text($"Teléfono Ref: {cliente.Direccion}"); // Usa campos genéricos si no tenés teléfono en el modelo
                                });

                                row.ConstantItem(15); // Espacio de separación del medio

                                // Bloque de Logística: Dirección de Entrega Destacada
                                row.RelativeItem().Border(1).BorderColor(Colors.Orange.Darken2).Padding(10).Background(Colors.Orange.Lighten5).Column(d =>
                                {
                                    d.Item().Text("LUGAR DE ENTREGA:").Bold().FontSize(11).FontColor(Colors.Orange.Darken3);
                                    d.Item().Text(string.IsNullOrEmpty(remito.DireccionEntrega) ? "Se retira por el local del emisor" : remito.DireccionEntrega).Bold();
                                    d.Item().PaddingTop(5).Text($"Estado del Envío: {remito.Estado}").FontSize(9).Italic();
                                });
                            });

                            col.Item().PaddingTop(0.8f, Unit.Centimetre);

                            // La Tabla de Renglones (Acá se listan los productos a entregar)
                            col.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(60);   // Cantidad
                                    columns.RelativeColumn();       // Descripción / Artículo
                                    columns.ConstantColumn(100);  // Observaciones / Estado
                                });

                                // Encabezado de la Tabla (Gris Oscuro)
                                tabla.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("Cant.").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("Descripción del Artículo").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("Control").FontColor(Colors.White).Bold();
                                });

                                // Renglones de la mercadería
                                foreach (var detalle in remito.Detalles)
                                {
                                    tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{detalle.Cantidad:G}");
                                    tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(detalle.DescripcionSnapshot);
                                    // Renglón vacío o con guiones para que el transportista o cliente tilde manualmente al contar
                                    tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("[  ] Ok").FontColor(Colors.Grey.Lighten1);
                                }
                            });

                            // 3. SECCIÓN DE CONFORMIDAD Y RECEPCIÓN (Pie del contenido del Remito)
                            col.Item().PaddingTop(2.5f, Unit.Centimetre).Row(row =>
                            {
                                // Espacio izquierdo para aclaraciones del chofer
                                row.RelativeItem().Column(nota =>
                                {
                                    nota.Item().Text("Notas del Transportista:").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                                    nota.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                    nota.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                });

                                row.ConstantItem(40); // Separador

                                // Cuadro de Firma de conformidad de recepción
                                row.RelativeItem().Column(firmaCol =>
                                {
                                    firmaCol.Item().BorderTop(1).BorderColor(Colors.Grey.Darken1).PaddingTop(5).AlignCenter()
                                        .Text("Firma de Quien Recibe").FontSize(10).Bold();
                                    
                                    firmaCol.Item().PaddingTop(15).Text("Aclaración: ___________________________").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    firmaCol.Item().PaddingTop(8).Text("DNI/CI:       ___________________________").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    firmaCol.Item().PaddingTop(8).Text("Fecha/Hora:  ____/____/____  ____:____ hs").FontSize(9).FontColor(Colors.Grey.Darken2);
                                });
                            });
                        });

                        // 4. EL PIE DE PÁGINA (Paginado estándar)
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });

                return documento.GeneratePdf();
            });
        }
    
        // 🎯 MÉTODO PARA GENERAR EL PDF DE LA NOTA DE CRÉDITO (Saldos a Favor / Devoluciones)
        public async Task<byte[]> GenerarNotaCreditoPdfAsync(NotaCredito notaCredito, Perfil perfil, Cliente cliente)
        {
            return await Task.Run(() =>
            {
                var documento = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(1, Unit.Centimetre);
                        page.Size(PageSizes.A4);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                        // 1. EL ENCABEZADO (Diseño en tonos Bordó / Rojo Oscuro)
                        page.Header().Row(row =>
                        {
                            // Lado Izquierdo: Datos del Perfil Emisor (Tu tía)
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(perfil.NombreFantasia).FontSize(20).Bold().FontColor(Colors.Red.Darken3);
                                col.Item().Text($"Dirección: {perfil.Direccion}");
                                col.Item().Text($"Teléfono: {perfil.TelefonoPrincipal}");
                            });

                            // Lado Derecho: Datos de la Nota de Crédito
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text("NOTA DE CRÉDITO").FontSize(20).Bold().FontColor(Colors.Red.Darken4);
                                col.Item().Text($"Fecha Emisión: {notaCredito.FechaEmision.ToLocalTime():dd/MM/yyyy}");
                                col.Item().Text($"Vence (Saldo Válido): {notaCredito.FechaVencimiento.ToLocalTime():dd/MM/yyyy}").FontSize(9).Italic();
                                col.Item().Text($"Comprobante: #{notaCredito.Id.ToString("D8")}");
                            });
                        });

                        // 2. EL CONTENIDO
                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                        {
                            // Bloque Destacado: El Saldo a Favor / Crédito en Grande
                            col.Item().Border(2).BorderColor(Colors.Red.Darken3).Background(Colors.Red.Lighten5).Padding(15).Row(row =>
                            {
                                row.RelativeItem().AlignMiddle().Text("CRÉDITO A FAVOR DEL CLIENTE:").Bold().FontSize(12).FontColor(Colors.Red.Darken4);
                                row.ConstantItem(150).AlignRight().AlignMiddle().Background(Colors.White).Border(1).BorderColor(Colors.Red.Darken3).Padding(5).AlignCenter()
                                    .Text($"${notaCredito.Total:N2}").FontSize(16).Bold().FontColor(Colors.Red.Darken4);
                            });

                            col.Item().PaddingTop(0.5f, Unit.Centimetre);

                            // Recuadro del Cliente (Estilo gris estándar para mantener la línea)
                            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Background(Colors.Grey.Lighten3).Column(c =>
                            {
                                c.Item().Text("CLIENTE / BENEFICIARIO:").Bold().FontSize(11).FontColor(Colors.Grey.Darken3);
                                c.Item().Text($"Nombre: {cliente.Nombre}");
                                c.Item().Text($"Id Cliente: {(cliente.Id > 0 ? cliente.Id.ToString() : "No registrado / Público General")}");
                                if (!string.IsNullOrEmpty(cliente.CuitCuil) && cliente.CuitCuil != "00-00000000-0")
                                {
                                    c.Item().Text($"CUIT/CUIL: {cliente.CuitCuil}");
                                    c.Item().Text($"Dirección: {cliente.Direccion}");
                                }
                            });

                            col.Item().PaddingTop(1, Unit.Centimetre);

                            // Detalles y Concepto de la Nota de Crédito
                            col.Item().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(det =>
                            {
                                det.Item().Row(r =>
                                {
                                    r.ConstantItem(140).Text("Concepto / Motivo:").Bold();
                                    r.RelativeItem().Text(string.IsNullOrEmpty(notaCredito.Detalle) ? "Devolución de mercadería / Ajuste de saldo." : notaCredito.Detalle);
                                });

                                det.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                det.Item().Row(r =>
                                {
                                    r.ConstantItem(140).Text("Estado del Comprobante:").Bold();
                                    r.RelativeItem().Text(notaCredito.Estado.ToString()).Bold().FontColor(Colors.Green.Darken3);
                                });

                                det.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                det.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("* Este documento representa un saldo disponible que el cliente podrá utilizar como parte de pago en futuras compras antes de su fecha de vencimiento.").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                                });
                            });

                            // Firma autorizada del comercio
                            col.Item().PaddingTop(3, Unit.Centimetre).Row(row =>
                            {
                                row.RelativeItem(); // Desplazar a la derecha
                                row.ConstantItem(200).Column(firmaCol =>
                                {
                                    firmaCol.Item().BorderTop(1).BorderColor(Colors.Grey.Darken1).PaddingTop(5).AlignCenter().Text("Firma Autorizada").FontSize(9);
                                    firmaCol.Item().AlignCenter().Text(perfil.NombreFantasia).FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                                });
                            });
                        });

                        // 3. EL PIE DE PÁGINA
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });

                return documento.GeneratePdf();
            });
        }
    }
}