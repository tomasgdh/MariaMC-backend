using Entities.Items;
using Entities.RequestModels;
using MercadoPago.Resource.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApiMariaMC.IServicies;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace WebApiMariaMC.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost(Name = "InsertUser")]
        public int InsertUser(NewUserRequest newUserRequest)
        {
            return _userService.RegisterNewUser(newUserRequest);
        }


        [HttpPost]
        [Authorize]
        public ActionResult<object> GetDataUser([FromBody] UsuarioRequest usuario)
        {
            Usuario? user = _userService.GetForIdUsuario(usuario.IdUsuario);
            if (user == null)
            {
                return new { result = "error", message = "El usuario o la contraseña no son válidos" };
            }

            var u = new
            {
                user.IdUsuario,
                user.NombreUsuario,
                user.IdEmpleadoNavigation.Nombre,
                user.IdEmpleadoNavigation.Apellido
            };

            return new { result = "ok", user = u };
        }

        [HttpGet(Name = "GetAllUsers")]
        [NonAction]
        public List<Usuario> GetAllUsers()
        {
            return _userService.GetAllUsers();
        }

        [HttpPost(Name = "UpdateUser")]
        [NonAction]
        public void UpdateUser(Usuario userItem)
        {
            _userService.UpdateUser(userItem);
        }

        [HttpDelete(Name = "DeleteUser")]
        [NonAction]
        public void DeleteUser(int Id)
        {
            _userService.DeleteUser(Id);
        }
        public class UsuarioRequest
        {
            public int IdUsuario { get; set; }
        }
    }
}