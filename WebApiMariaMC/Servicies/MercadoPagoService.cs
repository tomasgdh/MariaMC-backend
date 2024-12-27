using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace WebApiMariaMC.Servicies
{
    public class MercadoPagoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken;

        public MercadoPagoService(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _accessToken = configuration["MercadoPago:AccessToken"];
        }

        public async Task<dynamic> CreatePointPaymentAsync(decimal amount, string description)
        {
            var requestContent = new
            {
                amount = amount,
                description = description,
                payment_method_id = "point",
                external_reference = Guid.NewGuid().ToString() // Genera un UUID//"YOUR_EXTERNAL_REFERENCE"
            };

            var jsonContent = JsonConvert.SerializeObject(requestContent);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

            var response = await _httpClient.PostAsync("https://api.mercadopago.com/point/integration-api", httpContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<dynamic>(responseContent);
        }
    }
}