
using Entities.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.RequestModels
{
    public class NewUserRequest
    {
        public string NombreUsuario { get; set; }
        public string Contraseña { get; set; }
        public Usuario ToUserioItem()
        {
            var userItem = new Usuario();
            userItem.NombreUsuario = NombreUsuario;
            userItem.Contraseña = Contraseña;
            return userItem;
        }

    }
}
