using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebApiMariaMC.Servicies;

namespace WebApiMariaMC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly MercadoPagoService _mercadoPagoService;

        public PaymentController(MercadoPagoService mercadoPagoService)
        {
            _mercadoPagoService = mercadoPagoService;
        }

        [HttpPost("create-point-payment")]
        public async Task<IActionResult> CreatePointPayment([FromBody] PaymentRequest request)
        {
            var pointPayment = await _mercadoPagoService.CreatePointPaymentAsync(request.Amount, request.Description);
            return Ok(new { Id = pointPayment.id, Status = pointPayment.status });
        }
    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}