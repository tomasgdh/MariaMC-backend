using Entities.Items;

namespace Logic.ILogic
{
    public interface IUserLogic
    {
        int InsertUser(Usuario userItem);
        List<Usuario> GetAllUsers();
        void UpdateUser(Usuario item);
        void UpdatePassword(int IdUsuario, string password);
        void DeleteUser(int id);
        string loginUser(string username, string password);
        Usuario? GetForUsername(string username);
        Usuario? GetForIdUsuario(int IdUsuario);
        Usuario? GetForToken(string token);
    }
}
