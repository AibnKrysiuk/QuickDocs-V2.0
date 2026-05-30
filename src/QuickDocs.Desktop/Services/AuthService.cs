using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using QuickDocs.Desktop.Models;

namespace QuickDocs.Desktop.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        // La URL base de tu API en Docker (ajustá el puerto si usas otro, por ej: 5000 o 5001)
        private const string BaseUrl = "http://localhost:5018/api/auth/";

        public AuthService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Envía las credenciales a la API y devuelve la respuesta si el login es exitoso.
        /// </summary>
        public async Task<LoginResponse?> LoginAsync(string username, string contrasena)
        {
            try
            {
                // 1. Armamos el DTO de ida con los datos que ingresó el usuario
                var loginRequest = new LoginRequest
                {
                    Username = username,
                    Contrasena = contrasena
                };

                // 2. Hacemos la petición POST enviando el objeto como JSON
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}login", loginRequest);

                // 3. Si la API responde con un código de éxito (200 OK)
                if (response.IsSuccessStatusCode)
                {
                    // Deserializamos el JSON de vuelta en nuestro objeto LoginResponse
                    var resultado = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    return resultado;
                }

                // Si la API devuelve 401 Unauthorized u otro error, retornamos null
                return null;
            }
            catch (Exception ex)
            {
                // Por ahora, si hay un error de red (ej: Docker apagado), mostramos en consola y devolvemos null
                Console.WriteLine($"Error de conexión con la API: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Envía los datos a la API para dar de alta un nuevo usuario
        /// </summary>
        public async Task<bool> RegisterAsync(string username, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}register", new { Username = username, Password = password });
                
                // Si la API devuelve 200 OK, fue exitoso
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al conectar con el servidor de registro: {ex.Message}");
                return false;
            }
        }
    }
}