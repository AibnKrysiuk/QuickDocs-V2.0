using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using QuickDocs.Desktop.ViewModels;
using System;

namespace QuickDocs.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        // Lógica para el botón Salir
        private void BotonSalir_Click(object? sender, RoutedEventArgs e)
        {
            // Como usamos ShutdownMode.OnExplicitShutdown en App.axaml.cs,
            // tenemos que decirle a la aplicación que se apague por completo.
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
}