using Entities.Items;
using Entities.RequestModels;
using Logic.ILogic;
using WebApiMariaMC.IServicies;

namespace WebApiMariaMC.Servicies
{
    public class UserService: IUserService
    {
        private readonly IUserLogic _userLogic;
        public UserService(IUserLogic userLogic)
        {
            _userLogic = userLogic;
        }
        public List<Usuario> GetAllUsers() { return _userLogic.GetAllUsers(); }

        public int RegisterNewUser(NewUserRequest newUserRequest) { 
            var newUser = newUserRequest.ToUserioItem();
            return _userLogic.InsertUser(newUser);
        }

        public Usuario? GetForIdUsuario(int IdUsuario)
        {
            return _userLogic.GetForIdUsuario(IdUsuario);
        }

        public void UpdateUser(Usuario item)
        {
            _userLogic.UpdateUser(item);
        }

        public void DeleteUser(int id)
        {
            _userLogic.DeleteUser(id);
        }

    }
}
