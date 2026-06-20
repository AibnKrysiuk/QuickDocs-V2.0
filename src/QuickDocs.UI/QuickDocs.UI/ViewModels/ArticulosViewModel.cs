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
    public partial class ArticulosViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "http://localhost:5018/api/items";

        // Lista de respaldo para guardar TODO el catálogo descargado de la API
        private List<Item> _todosLosItems = new();

        public List<TipoItem> TiposItem { get; } = new() { TipoItem.Producto, TipoItem.Servicio };
        public List<string> UnidadesMedida { get; } = new() { "u.", "kg", "l.", "hs", "mts" };

        [ObservableProperty]
        private TipoItem _tipoSeleccionado = TipoItem.Producto;

        [ObservableProperty]
        private string _descripcion = string.Empty;

        [ObservableProperty]
        private string? _marca;

        [ObservableProperty]
        private decimal _precioUnitario;

        [ObservableProperty]
        private string _unidadSeleccionada = "u.";

        // --- BUSCADOR REACTIVO ---
        private string _textoBuscar = string.Empty;
        public string TextoBuscar
        {
            get => _textoBuscar;
            set
            {
                SetProperty(ref _textoBuscar, value);
                // Cada vez que el usuario escribe, ejecutamos el filtro instantáneamente
                FiltrarItems();
            }
        }

        private Item? _itemSeleccionado;
        private Item? _itemActual;

        public bool EsEdicion => _itemActual != null;
        public Item? ItemSeleccionado
        {
            get => _itemSeleccionado;
            set
            {
                SetProperty(ref _itemSeleccionado, value);
                CargarItemEnFormulario(value);
            }
        }

        // Es la colección que la vista (el ListBox) observa
        public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>();

        public IAsyncRelayCommand CargarItemsCommand { get; }
        public IAsyncRelayCommand GuardarItemCommand { get; }
        public IAsyncRelayCommand<Item> BorrarItemCommand { get; }
        public IRelayCommand LimpiarFormularioCommand { get; }
        public IRelayCommand LimpiarFiltrosCommand { get; }

        public ArticulosViewModel()
        {
            _httpClient = new HttpClient();

            CargarItemsCommand = new AsyncRelayCommand(CargarItemsAsync);
            GuardarItemCommand = new AsyncRelayCommand(GuardarItemAsync);
            BorrarItemCommand = new AsyncRelayCommand<Item>(BorrarItemAsync);
            LimpiarFormularioCommand = new RelayCommand(LimpiarCampos);
            LimpiarFiltrosCommand = new RelayCommand(LimpiarFiltros);

            Dispatcher.UIThread.Post(async () => await CargarItemsAsync());
        }

        private async Task CargarItemsAsync()
        {
            try
            {
                var lista = await _httpClient.GetFromJsonAsync<List<Item>>($"{ApiUrl}?usuarioId=1");

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Guardamos la lista original completa en el respaldo de memoria
                    _todosLosItems = lista ?? new List<Item>();
                    
                    // Aplicamos el filtro para refrescar lo que ve la pantalla
                    FiltrarItems();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar artículos: {ex.Message}");
            }
        }

        // 🔥 MÉTODO CLAVE: Filtra la lista en caliente sin pegarle de nuevo a la base de datos
        private void FiltrarItems()
        {
            Items.Clear();

            // Si no hay texto de búsqueda, mostramos todo el catálogo ordenado
            if (string.IsNullOrWhiteSpace(TextoBuscar))
            {
                foreach (var item in _todosLosItems)
                {
                    Items.Add(item);
                }
                return;
            }

            // Normalizamos la búsqueda a minúsculas para ignorar mayúsculas/minúsculas
            string busqueda = TextoBuscar.ToLower().Trim();

            var filtrados = _todosLosItems.Where(i => 
                i.Descripcion.ToLower().Contains(busqueda) || 
                (i.Marca != null && i.Marca.ToLower().Contains(busqueda)) ||
                i.Tipo.ToString().ToLower().Contains(busqueda)
            );

            foreach (var item in filtrados)
            {
                Items.Add(item);
            }
        }

        private async Task GuardarItemAsync()
        {
            if (string.IsNullOrWhiteSpace(Descripcion)) return;

            var itemData = new Item
            {
                Id = ItemSeleccionado?.Id ?? 0,
                Descripcion = Descripcion,
                Marca = string.IsNullOrWhiteSpace(Marca) ? null : Marca,
                PrecioUnitario = PrecioUnitario,
                Tipo = TipoSeleccionado,
                UnidadMedida = UnidadSeleccionada,
                UsuarioId = 1
            };

            try
            {
                HttpResponseMessage response;
                if (itemData.Id == 0)
                {
                    response = await _httpClient.PostAsJsonAsync(ApiUrl, itemData);
                }
                else
                {
                    response = await _httpClient.PutAsJsonAsync($"{ApiUrl}/{itemData.Id}", itemData);
                }

                if (response.IsSuccessStatusCode)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        LimpiarCampos();
                        await CargarItemsAsync();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error crítico al guardar artículo: {ex.Message}");
            }
        }

        private async Task BorrarItemAsync(Item? item)
        {
            if (item == null) return;

            try
            {
                await _httpClient.DeleteAsync($"{ApiUrl}/{item.Id}");
                await CargarItemsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al borrar artículo: {ex.Message}");
            }
        }

        private void CargarItemEnFormulario(Item? item)
        {
            _itemActual = item; 
            OnPropertyChanged(nameof(EsEdicion)); 

            if (item == null)
            {
                LimpiarCampos();
                return;
            }

            Descripcion = item.Descripcion;
            Marca = item.Marca;
            PrecioUnitario = item.PrecioUnitario;
            TipoSeleccionado = item.Tipo;
            UnidadSeleccionada = item.UnidadMedida ?? "u.";
        }

        private void LimpiarFiltros()
        {
            TextoBuscar = string.Empty; // Vacía la caja de búsqueda
            LimpiarCampos();            // Resetea el formulario y deselecciona la grilla
        }

        private void LimpiarCampos()
        {
            _itemSeleccionado = null;
            _itemActual = null;
            OnPropertyChanged(nameof(ItemSeleccionado));
            OnPropertyChanged(nameof(EsEdicion));

            Descripcion = string.Empty;
            Marca = string.Empty;
            PrecioUnitario = 0;
            TipoSeleccionado = TipoItem.Producto;
            UnidadSeleccionada = "u.";
            
            // Al limpiar o cancelar, reseteamos el buscador para volver a listar todo
            TextoBuscar = string.Empty;
        }
    }
}