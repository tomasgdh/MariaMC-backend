using Entities.RequestModels;
using Entities.ResponseModels;
using Logic.ILogic;
using Logic.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        private readonly ILogger<SessionController> _logger;
        private readonly ISessionLogic _sessionLogic;

        public SessionController(
            ILogger<SessionController> logger,
            ISessionLogic sessionLogic)
        {
            _logger = logger;
            _sessionLogic = sessionLogic;
        }

        // Clase 9: La decoración [AllowAnonymous] indica que para este endpoint específico no es necesario estar autentificado independientemente de la configuración del controlador
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<object>> Login(LoginRequest user)
        {
            return await _sessionLogic.Login(user);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<object>> UpdatePassword(UpdatePasswordRequest update)
        {
            return await _sessionLogic.UpdatePassword(update);
        }

        [HttpPost]
        public async Task<ActionResult<object>> RefreshToken([FromBody] RefreshTokenRequest tokenModel)
        {
            try {
                RefreshTokenResponse result = await _sessionLogic.RefreshToken(tokenModel);
                if (result.result != "ok")
                {
                    return new { result = "NoAutorizado", message = "Ocurrio un error al actualizar la contraseña" };
                }
                return result;
            }
            catch (Exception ex) { return new { result = "NoAutorizado", message = "Ocurrio un error al actualizar la contraseña" }; }
                        
        }

    }
}
