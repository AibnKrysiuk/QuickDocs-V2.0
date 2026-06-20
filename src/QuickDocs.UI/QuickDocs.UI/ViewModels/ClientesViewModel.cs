using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using QuickDocs.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using System.Linq; // 👈 Importante para usar los filtros de LINQ

namespace QuickDocs.UI.ViewModels
{
    public partial class ClientesViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "http://localhost:5018/api/clientes"; 

        // Lista de respaldo para guardar TODOS los clientes descargados de la API
        private List<Cliente> _todosLosClientes = new();

        // --- Propiedades del Formulario (Campos de texto) ---
        [ObservableProperty]
        private string _nombre = string.Empty;

        [ObservableProperty]
        private string _cuitCuil = string.Empty;

        [ObservableProperty]
        private string? _email;

        [ObservableProperty]
        private string? _telefono;

        [ObservableProperty]
        private string? _direccion;

        [ObservableProperty]
        private string? _localidad;

        // --- BUSCADOR REACTIVO EN MEMORIA ---
        private string _textoBuscar = string.Empty;
        public string TextoBuscar
        {
            get => _textoBuscar;
            set
            {
                SetProperty(ref _textoBuscar, value);
                // Cada vez que el usuario escribe, ejecutamos el filtro instantáneamente
                FiltrarClientes();
            }
        }

        // --- Estado de Selección ---
        private Cliente? _clienteSeleccionado;
        private Cliente? _clienteActual;

        public bool EsEdicion => _clienteActual != null;
        public Cliente? ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set
            {
                SetProperty(ref _clienteSeleccionado, value);
                CargarClienteEnFormulario(value);
            }
        }

        // Colección dinámica que alimenta la lista de la derecha
        public ObservableCollection<Cliente> Clientes { get; } = new ObservableCollection<Cliente>();

        // --- Comandos de CommunityToolkit ---
        public IAsyncRelayCommand CargarClientesCommand { get; }
        public IAsyncRelayCommand GuardarClienteCommand { get; }
        public IAsyncRelayCommand<Cliente> BorrarClienteCommand { get; }
        public IRelayCommand LimpiarFormularioCommand { get; }
        public IRelayCommand LimpiarFiltrosCommand { get; }

        // --- Constructor ---
        public ClientesViewModel()
        {
            _httpClient = new HttpClient();

            CargarClientesCommand = new AsyncRelayCommand(CargarClientesAsync);
            GuardarClienteCommand = new AsyncRelayCommand(GuardarClienteAsync);
            BorrarClienteCommand = new AsyncRelayCommand<Cliente>(BorrarClienteAsync);
            LimpiarFormularioCommand = new RelayCommand(LimpiarCampos);
            LimpiarFiltrosCommand = new RelayCommand(LimpiarFiltros);

            Dispatcher.UIThread.Post(async () => await CargarClientesAsync());
        }

        // --- Métodos de Lógica (CRUD hacia la API) ---
        private async Task CargarClientesAsync()
        {
            try
            {
                var lista = await _httpClient.GetFromJsonAsync<List<Cliente>>($"{ApiUrl}?usuarioId=1");
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Guardamos la lista original completa en el respaldo de memoria
                    _todosLosClientes = lista ?? new List<Cliente>();

                    // Aplicamos el filtro para refrescar lo que ve la pantalla
                    FiltrarClientes();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar: {ex.Message}");
            }
        }

        // 🔥 MÉTODO CLAVE: Filtra los clientes reactivamente sin sobrecargar la API
        private void FiltrarClientes()
        {
            Clientes.Clear();

            // Si no hay texto de búsqueda, mostramos la lista completa ordenada por nombre
            if (string.IsNullOrWhiteSpace(TextoBuscar))
            {
                var ordenados = _todosLosClientes.OrderBy(c => c.Nombre);
                foreach (var cliente in ordenados)
                {
                    Clientes.Add(cliente);
                }
                return;
            }

            // Normalizamos el texto a buscar
            string busqueda = TextoBuscar.ToLower().Trim();

            // Filtramos comparando con Nombre, CUIT/CUIL o Localidad (controlando nulos)
            var filtrados = _todosLosClientes.Where(c =>
                c.Nombre.ToLower().Contains(busqueda) ||
                c.CuitCuil.ToLower().Contains(busqueda) ||
                (c.Localidad != null && c.Localidad.ToLower().Contains(busqueda))
            ).OrderBy(c => c.Nombre);

            foreach (var cliente in filtrados)
            {
                Clientes.Add(cliente);
            }
        }

        private async Task GuardarClienteAsync()
        {
            if (string.IsNullOrWhiteSpace(Nombre)) return;

            var clienteData = new Cliente
            {
                Id = ClienteSeleccionado?.Id ?? 0,
                Nombre = Nombre,
                CuitCuil = CuitCuil,
                Email = Email,
                Telefono = Telefono,
                Direccion = Direccion,
                Localidad = Localidad,
                UsuarioId = 1,
                FechaAlta = DateTime.Now
            };

            try
            {
                HttpResponseMessage response;
                if (clienteData.Id == 0)
                {
                    response = await _httpClient.PostAsJsonAsync(ApiUrl, clienteData);
                }
                else
                {
                    response = await _httpClient.PutAsJsonAsync($"{ApiUrl}/{clienteData.Id}", clienteData);
                }

                if (response.IsSuccessStatusCode)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        LimpiarCampos();
                        await CargarClientesAsync();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error crítico en UI al guardar: {ex.Message}");
            }
        }

        private async Task BorrarClienteAsync(Cliente? cliente)
        {
            if (cliente == null) return;

            try
            {
                await _httpClient.DeleteAsync($"{ApiUrl}/{cliente.Id}");
                await CargarClientesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al borrar: {ex.Message}");
            }
        }

        private void CargarClienteEnFormulario(Cliente? cliente)
        {
            _clienteActual = cliente; 
            
            // Avisamos a la vista que cambie la visibilidad del botón Cancelar
            OnPropertyChanged(nameof(EsEdicion)); 

            if (cliente == null)
            {
                LimpiarCampos();
                return;
            }

            // Cargamos los TextBox de la izquierda
            Nombre = cliente.Nombre;
            CuitCuil = cliente.CuitCuil;
            Email = cliente.Email;
            Telefono = cliente.Telefono;
            Direccion = cliente.Direccion;
            Localidad = cliente.Localidad;
        }

        private void LimpiarFiltros()
        {
            TextoBuscar = string.Empty; // Vacía la caja de búsqueda
            LimpiarCampos();            // Resetea el formulario y deselecciona la grilla
        }
        private void LimpiarCampos()
        {
            _clienteActual = null;
            _clienteSeleccionado = null; // Deseleccionamos también la fila de la lista
            
            OnPropertyChanged(nameof(EsEdicion));
            OnPropertyChanged(nameof(ClienteSeleccionado)); // Avisamos la deselección

            Nombre = string.Empty;
            CuitCuil = string.Empty;
            Email = string.Empty;
            Telefono = string.Empty;
            Direccion = string.Empty;
            Localidad = string.Empty;
        }
    }
}