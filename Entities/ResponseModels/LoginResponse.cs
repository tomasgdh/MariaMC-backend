namespace Entities.ResponseModels
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string forzarCambioPass { get; set; } = string.Empty;
    }

    public class RefreshTokenResponse
    {
        public string result { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
