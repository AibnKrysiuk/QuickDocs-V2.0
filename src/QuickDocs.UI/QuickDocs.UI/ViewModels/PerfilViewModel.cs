using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls; // 🛠️ Requerido para usar OpenFileDialog
using QuickDocs.Core.Models;
using Avalonia.Media.Imaging; // 🎯 Requerido para usar la clase Bitmap

namespace QuickDocs.UI.ViewModels
{
    public class PerfilViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        private string _rutaLogoSeleccionadoLocal = string.Empty; // 📸 Almacena temporalmente la ruta del logo elegido

        // Campos respaldatorios (Backing fields)
        private string _nombreFantasia = string.Empty;
        private string _cuitCuil = string.Empty;
        private string _condicionIva = string.Empty;
        private string _telefonoPrincipal = string.Empty;
        private string _telefonoSecundario = string.Empty;
        private string _emailContacto = string.Empty;
        private string _direccion = string.Empty;
        private string _logoPath = string.Empty;

        private Bitmap? _logoImagen;
        public Bitmap? LogoImagen
        {
            get => _logoImagen;
            set => SetProperty(ref _logoImagen, value);
        }

        // ─── PROPIEDADES ENLAZADAS A LA VISTA (Bindings) ───
        public string NombreFantasia
        {
            get => _nombreFantasia;
            set => SetProperty(ref _nombreFantasia, value);
        }

        public string CuitCuil
        {
            get => _cuitCuil;
            set => SetProperty(ref _cuitCuil, value);
        }

        public string CondicionIva
        {
            get => _condicionIva;
            set => SetProperty(ref _condicionIva, value);
        }

        public string TelefonoPrincipal
        {
            get => _telefonoPrincipal;
            set => SetProperty(ref _telefonoPrincipal, value);
        }

        public string TelefonoSecundario
        {
            get => _telefonoSecundario;
            set => SetProperty(ref _telefonoSecundario, value);
        }

        public string EmailContacto
        {
            get => _emailContacto;
            set => SetProperty(ref _emailContacto, value);
        }

        public string Direccion
        {
            get => _direccion;
            set => SetProperty(ref _direccion, value);
        }

        public string LogoPath
        {
            get => _logoPath;
            set => SetProperty(ref _logoPath, value);
        }

        // ─── COMANDOS ───
        public ICommand PrevisualizarCabeceraCommand { get; }
        public ICommand GuardarPerfilCommand { get; }
        public ICommand SeleccionarLogoCommand { get; }

        // Constructor base
        public PerfilViewModel()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5018/") };

            PrevisualizarCabeceraCommand = new AsyncRelayCommand(EjecutarPrevisualizarCabecera);
            GuardarPerfilCommand = new AsyncRelayCommand(EjecutarGuardarPerfil);
            SeleccionarLogoCommand = new AsyncRelayCommand(EjecutarSeleccionarLogo); // 🛠️ Cambiado a AsyncRelayCommand

            Task.Run(async () => await CargarPerfilDesdeApi());
        }
        private async Task CargarPerfilDesdeApi()
        {
            try
            {
                // Consultamos el perfil del usuario de pruebas (ID 1)
                var response = await _httpClient.GetAsync("api/perfiles/1");

                if (response.IsSuccessStatusCode)
                {
                    var perfilGuardado = await response.Content.ReadFromJsonAsync<Perfil>();
                    if (perfilGuardado != null)
                    {
                        // Poblamos la pantalla con lo que verdaderamente hay en SQLite
                        NombreFantasia = perfilGuardado.NombreFantasia;
                        CuitCuil = perfilGuardado.CuitCuil;
                        CondicionIva = perfilGuardado.CondicionIva;
                        TelefonoPrincipal = perfilGuardado.TelefonoPrincipal ?? string.Empty;
                        TelefonoSecundario = perfilGuardado.TelefonoSecundario ?? string.Empty;
                        EmailContacto = perfilGuardado.EmailContacto;
                        Direccion = perfilGuardado.Direccion;
                        
                        // Asignamos las rutas para el XAML y el PDF
                        LogoPath = perfilGuardado.LogoPath;
                        _rutaLogoSeleccionadoLocal = perfilGuardado.LogoPath;

                        // 🎯 Convertimos la ruta física de Linux en un Bitmap real para la pantalla
                        if (!string.IsNullOrWhiteSpace(LogoPath) && File.Exists(LogoPath))
                        {
                            try
                            {
                                LogoImagen = new Bitmap(LogoPath);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR AL CARGAR BITMAP]: {ex.Message}");
                                LogoImagen = null;
                            }
                        }
                        else
                        {
                            LogoImagen = null;
                        }
                    }
                }
                else
                {
                    // Si el perfil no existe en la DB todavía (primera vez), dejamos la semilla
                    CargarDatosDePrueba();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR AL CARGAR PERFIL]: {ex.Message}");
                CargarDatosDePrueba(); // Contingencia si la API está apagada
            }
        }
        private void CargarDatosDePrueba()
        {
            NombreFantasia = "QuickDocs";
            CuitCuil = "20-37147929-6";
            CondicionIva = "Responsable inscripto";
            TelefonoPrincipal = "21456325";
            TelefonoSecundario = "21456325";
            EmailContacto = "quickdocs@gmail.com";
            Direccion = "Algun Lugar 123, La Plata, Bs.As. 1900";
        }

        // ─── LÓGICA DE PREVISUALIZACIÓN DE PDF ───
        private async Task EjecutarPrevisualizarCabecera()
        {
            try
            {
                var perfilMuestra = new Perfil
                {
                    UsuarioId = 1,
                    NombreFantasia = this.NombreFantasia,
                    Direccion = this.Direccion,
                    CuitCuil = this.CuitCuil,       // 🎯 Enviamos el nuevo campo
                    EmailContacto = this.EmailContacto, // 🎯 Enviamos el nuevo campo
                    CondicionIva = this.CondicionIva,
                    TelefonoPrincipal = this.TelefonoPrincipal,
                    TelefonoSecundario = this.TelefonoSecundario,
                    LogoPath = _rutaLogoSeleccionadoLocal // Si seleccionó uno local, se previsualizará en el PDF temporal
                };

                var response = await _httpClient.PostAsJsonAsync("api/perfiles/previsualizar-pdf", perfilMuestra);

                if (response.IsSuccessStatusCode)
                {
                    byte[] pdfBytes = await response.Content.ReadAsByteArrayAsync();

                    string tempPath = Path.Combine(Path.GetTempPath(), "Previsualizacion_Cabecera.pdf");
                    await File.WriteAllBytesAsync(tempPath, pdfBytes);

                    // Lanzamos el visor capturando y silenciando las advertencias de dconf de Linux
                    try 
                    {
                        using var prod = new Process();
                        prod.StartInfo.FileName = "xdg-open"; 
                        prod.StartInfo.Arguments = $"\"{tempPath}\"";
                        prod.StartInfo.UseShellExecute = false;
                        prod.StartInfo.CreateNoWindow = true;
                        prod.StartInfo.RedirectStandardError = true; // Absorbe el dconf-CRITICAL
                        prod.StartInfo.RedirectStandardOutput = true; 
                        prod.Start();
                    }
                    catch
                    {
                        // Plan B genérico si xdg-open no responde
                        Process.Start(new ProcessStartInfo { FileName = tempPath, UseShellExecute = true });
                    }

                }
                else
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ERROR API]: {errorMsg}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR EXCEPCIÓN]: {ex.Message}");
            }
        }

        // ─── 💾 LÓGICA DE GUARDADO MEDIANTE FORM-DATA CONTRA LA API ───
        private async Task EjecutarGuardarPerfil()
        {
            try
            {
                using var content = new MultipartFormDataContent();

                // Empaquetamos los datos en formato de texto plano para el Form
                content.Add(new StringContent("1"), "UsuarioId"); // Usuario fijo para pruebas locales
                content.Add(new StringContent(NombreFantasia ?? string.Empty), "NombreFantasia");
                content.Add(new StringContent(Direccion ?? string.Empty), "Direccion");
                content.Add(new StringContent(CuitCuil ?? string.Empty), "CuitCuil");
                content.Add(new StringContent(CondicionIva ?? string.Empty), "CondicionIva");
                content.Add(new StringContent(TelefonoPrincipal ?? string.Empty), "TelefonoPrincipal");
                content.Add(new StringContent(TelefonoSecundario ?? string.Empty), "TelefonoSecundario");
                content.Add(new StringContent(EmailContacto ?? string.Empty), "EmailContacto");

                // Si se seleccionó una ruta de logo válida en el disco local, la adjuntamos
                if (!string.IsNullOrWhiteSpace(_rutaLogoSeleccionadoLocal) && File.Exists(_rutaLogoSeleccionadoLocal))
                {
                    byte[] fileBytes = await File.ReadAllBytesAsync(_rutaLogoSeleccionadoLocal);
                    var fileContent = new ByteArrayContent(fileBytes);
                    
                    // Identificamos el tipo mime básico de la imagen
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                    
                    content.Add(fileContent, "LogoArchivo", Path.GetFileName(_rutaLogoSeleccionadoLocal));
                }

                var response = await _httpClient.PostAsync("api/perfiles", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("[ÉXITO] El perfil comercial y el logo se persistieron correctamente.");
                    await CargarPerfilDesdeApi();
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ERROR API AL GUARDAR]: {errorResponse}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR EXCEPCIÓN AL GUARDAR]: {ex.Message}");
            }
        }

        // ─── 📸 APERTURA DEL DIÁLOGO DEL S.O. PARA SELECCIONAR LA IMAGEN ───
        private async Task EjecutarSeleccionarLogo()
        {
            try
            {
                // 1. Buscamos la ventana principal activa en el ciclo de vida de la app de escritorio
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var topWindow = desktop.MainWindow;
                    if (topWindow == null) return;

                    // 2. Invocamos las opciones del nuevo selector de archivos
                    var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
                    {
                        Title = "Seleccionar Logotipo de la Empresa",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new Avalonia.Platform.Storage.FilePickerFileType("Imágenes")
                            {
                                Patterns = new[] { "*.png", "*.jpg", "*.jpeg" }
                            }
                        }
                    };

                    // 3. Abrimos el selector usando el StorageProvider de la ventana principal
                    var files = await topWindow.StorageProvider.OpenFilePickerAsync(options);

                    if (files != null && files.Count > 0)
                    {
                        // 4. Obtenemos la ruta absoluta del archivo en Linux
                        _rutaLogoSeleccionadoLocal = files[0].Path.LocalPath;
                        Console.WriteLine($"[LOGO LOCAL SELECCIONADO]: {_rutaLogoSeleccionadoLocal}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR SELECCIONAR LOGO]: {ex.Message}");
            }
        }

        // ─── IMPLEMENTACIÓN DE NOTIFY PROPERTY CHANGED ───
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}