
using Data.Models;
using Entities.Items;
using Logic.ILogic;
using Microsoft.EntityFrameworkCore;

namespace Logic.Logic
{
    public class UserLogic : IUserLogic
    {
        private readonly Maria_MCContext _context;  // Ajusta esto con el nombre correcto de tu DbContext

        public UserLogic(Maria_MCContext context)
        {
            _context = context;
        }

        public List<Usuario> GetAllUsers()
        {
            return _context.Usuarios.ToList();
        }

        public int InsertUser(Usuario item)
        {
            item.Contraseña = PasswordHash.Hash(item.Contraseña);
            _context.Usuarios.Add(item);
            _context.SaveChanges();
            return item.IdUsuario;
        }

        public void UpdateUser(Usuario item)
        {
            var usuarioAmodificar = _context.Usuarios.Find(item.IdUsuario);
            if (usuarioAmodificar != null)
            {
                usuarioAmodificar.NombreUsuario = item.NombreUsuario;
                usuarioAmodificar.Contraseña = PasswordHash.Hash(item.Contraseña);
                _context.SaveChanges();
            }
        }

        public void UpdatePassword(int IdUsuario, string password)
        {
            var usuarioAmodificar = _context.Usuarios.Find(IdUsuario);
            if (usuarioAmodificar != null)
            {
                usuarioAmodificar.Contraseña = PasswordHash.Hash(password);
                _context.SaveChanges();
            }
        }

        public void DeleteUser(int Id)
        {
            var usuarioAEliminar = _context.Usuarios.Find(Id);
            if (usuarioAEliminar != null)
            {
                _context.Usuarios.Remove(usuarioAEliminar);
                _context.SaveChanges();
            }
        }

        public string loginUser(string userName, string password)
        {
            return "";
        }

        public Usuario? GetForUsername(string username)
        {
            return (from u in _context.Usuarios
                    .Include(p => p.IdEmpleadoNavigation)
                        //where (u.ExpiredAt == null || u.ExpiredAt > DateTime.Now)
                    where u.Activo == "S"
                    && u.NombreUsuario == username.ToLower()
                    select u).FirstOrDefault();
        }
        public Usuario? GetForIdUsuario(int IdUsuario)
        {
            return (from u in _context.Usuarios
                    .Include(p => p.IdEmpleadoNavigation)
                        //where (u.ExpiredAt == null || u.ExpiredAt > DateTime.Now)
                    where u.Activo == "S"
                    && u.IdUsuario == IdUsuario
                    select u).FirstOrDefault();
        }

        public Usuario? GetForToken(string token)
        {
            return (from u in _context.Usuarios
                        //where (u.ExpiredAt == null || u.ExpiredAt > DateTime.Now)
                    where u.Activo == "S"
                    && u.token == token
                    select u).FirstOrDefault();
        }
    }
}
