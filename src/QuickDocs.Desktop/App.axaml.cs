using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using QuickDocs.Desktop.ViewModels;
using QuickDocs.Desktop.Views;

namespace QuickDocs.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // public override void OnFrameworkInitializationCompleted()
    // {
    //     if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    //     {
    //         // 1. Creamos la vista de Login
    //         var loginView = new LoginView();
            
    //         // 2. Definimos que el Login es la ventana PRINCIPAL de arranque
    //         desktop.MainWindow = loginView;
            
    //         // NOTA: No instancies ni hagas .Show() de MainWindow acá.
    //         // La MainWindow se debe abrir ÚNICAMENTE cuando el LoginView 
    //         // ejecute el evento OnLoginExitoso (tal como lo programamos en su .axaml.cs).
    //     }

    //     base.OnFrameworkInitializationCompleted();
    // }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Línea original (la pausamos un ratito):
            // desktop.MainWindow = new LoginView();
            
            // Línea temporal para diseñar la Fase 6:
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}