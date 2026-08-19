using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;

namespace QuickDocs.UI.Desktop;

sealed class Program
{
    private static Process? _procesoBackend;

    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DetenerBackend();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => DetenerBackend();

        try
        {
            IniciarBackendYEsperar();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            DetenerBackend();
        }
    }

    private static void IniciarBackendYEsperar()
    {
        // Si ya hay un backend corriendo (por ejemplo, quedó abierto de una sesión anterior
        // que no cerró bien), no lanzamos uno nuevo — reusamos el que ya está.
        if (BackendYaResponde().GetAwaiter().GetResult())
        {
            return;
        }

        string carpetaBase = AppContext.BaseDirectory;
        string nombreEjecutable = OperatingSystem.IsWindows() ? "QuickDocs.Backend.exe" : "QuickDocs.Backend";
        string rutaBackend = Path.Combine(carpetaBase, "backend", nombreEjecutable);

        if (!File.Exists(rutaBackend))
        {
            // No detenemos la app por esto — puede que el usuario esté corriendo
            // en modo desarrollo con el backend levantado a mano desde otra terminal.
            System.Diagnostics.Debug.WriteLine($"[WARN] No se encontró el backend en: {rutaBackend}");
            return;
        }

        _procesoBackend = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = rutaBackend,
                WorkingDirectory = Path.GetDirectoryName(rutaBackend),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        _procesoBackend.Start();

        // Esperamos hasta 10 segundos a que el backend responda antes de continuar
        const int intentosMaximos = 40;
        for (int i = 0; i < intentosMaximos; i++)
        {
            if (BackendYaResponde().GetAwaiter().GetResult()) return;
            Thread.Sleep(250);
        }

        System.Diagnostics.Debug.WriteLine("[WARN] El backend no respondió a tiempo. La app va a intentar arrancar igual.");
    }

    private static async Task<bool> BackendYaResponde()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
            var respuesta = await client.GetAsync("http://localhost:5018/weatherforecast");
            return respuesta.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static void DetenerBackend()
    {
        try
        {
            if (_procesoBackend != null && !_procesoBackend.HasExited)
            {
                _procesoBackend.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Si ya se cerró solo, o algo falla al matarlo, no queremos que esto tire abajo el cierre de la app.
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
    #if DEBUG
                .WithDeveloperTools()
    #endif
                .WithInterFont()
                .LogToTrace();
}