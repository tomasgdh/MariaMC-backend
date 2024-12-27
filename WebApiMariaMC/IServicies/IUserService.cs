using Entities.Items;
using Entities.RequestModels;

namespace WebApiMariaMC.IServicies
{
    public interface IUserService
    {
        //Usuario Authenticate(string username, string password);
        int RegisterNewUser(NewUserRequest newUserRequest);
        List<Usuario> GetAllUsers();
        Usuario? GetForIdUsuario(int IdUsuario);
        void UpdateUser(Usuario item);
        void DeleteUser(int Id);
    }
}
