namespace QuickDocs.Desktop.Models
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}