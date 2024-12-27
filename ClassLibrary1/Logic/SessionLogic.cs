using Logic.ILogic;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Data.Models;
using Entities.Items;
using Logic.Session;
using Entities.RequestModels;
using Entities.ResponseModels;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using MercadoPago.Resource.User;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Imaging;
using System.Drawing;

namespace Logic.Logic
{
    public class SessionLogic : ISessionLogic
    {
        private readonly Maria_MCContext _dataContext;
        private readonly IUserLogic _userLogic;
        private readonly Jwt _jwtSettings;

        public SessionLogic(
            IOptions<Jwt> jwtSettings,
            Maria_MCContext dataContext,
            IUserLogic userLogic
        )
        {
            _jwtSettings = jwtSettings.Value;
            _dataContext = dataContext;
            _userLogic = userLogic;
        }

        public async Task<object> Login(LoginRequest login)
        {
            try {
                if (string.IsNullOrWhiteSpace(login.Username))
                {
                    return new { result = "error", message = "El usuario o la contraseña no son válidos" };
                }

                if (string.IsNullOrWhiteSpace(login.Password))
                {
                    return new { result = "error", message = "El usuario o la contraseña no son válidos" };
                }

                Usuario? user = _userLogic.GetForUsername(login.Username);
                if (user == null)
                {
                    return new { result = "error", message = "El usuario o la contraseña no son válidos" };
                }

                if (!PasswordHash.Verify(login.Password, user.Contraseña))
                {
                    return new { result = "error", message = "El usuario o la contraseña no son válidos" };
                }

                Entities.Items.Session session = new Entities.Items.Session();
                session.IdUsuario = user.IdUsuario;
                session.OpenedAt = DateTime.Now;

                await _dataContext.Sessions.AddAsync(session);
                await _dataContext.SaveChangesAsync();

                /* En esta sección comienza la creación del JWT */
                string nombreCompleto = user.IdEmpleadoNavigation.Nombre.ToString() + ", " + user.IdEmpleadoNavigation.Apellido.ToString();
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub,"Security Token"),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    //new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
                    new Claim("userId", user.IdUsuario.ToString()),
                    new Claim("nombreCompleto", nombreCompleto),
                    new Claim("sessionId", session.Id.ToString()),
                    new Claim("role", "['A',]"),
                };

                var token = GenerateToken(claims, TimeSpan.FromMinutes(15));//15
                var refreshToken = GenerateToken(claims, TimeSpan.FromHours(12));
                await SaveRefreshToken(user.IdUsuario, refreshToken);
                /* Clase 9: Fin de la creación del JWT */
                // Extraer la primera letra del nombre y del apellido
                string initials = $"{user.IdEmpleadoNavigation.Nombre[0]}{user.IdEmpleadoNavigation.Apellido[0]}".ToUpper();
                GenerateAvatar(initials, user.IdUsuario.ToString());

                return new 
                {
                    result = "ok",
                    token,
                    refreshToken,
                    forzarCambioPass = false,
                };
            } 
            catch(Exception ex) {
                return new { result = "error", message = "Ocurrio un error al Loguearse" }; 
            }
            
        }

        private void GenerateAvatar(string initials, string userId)
        {
            string path = Path.Combine("Portraits", $"{userId}.png");

            // Verificar si la imagen ya existe
            if (File.Exists(path))
            {
                // Retornar la URL de la imagen existente
                return;
            }

            int width = 40;
            int height = 40;

            // Generar color aleatorio
            Random rnd = new Random();
            Color bgColor = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));

            // Crear la imagen
            using (Bitmap bmp = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    // Fondo
                    g.Clear(bgColor);

                    // Dibujar las iniciales
                    using (Font font = new Font("Arial", 18))
                    using (Brush textBrush = new SolidBrush(Color.White))
                    {
                        var textSize = g.MeasureString(initials, font);
                        g.DrawString(initials, font, textBrush,
                            (width - textSize.Width) / 2,
                            (height - textSize.Height) / 2);
                    }
                }

                // Guardar la imagen
                bmp.Save(path, ImageFormat.Png);
            }
        }

        public async Task<object> UpdatePassword(UpdatePasswordRequest update)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(update.CurrentPassword))
                {
                    return new { result = "error", message = "la contraseña actual no es válida" };
                }

                if (string.IsNullOrWhiteSpace(update.NewPassword))
                {
                    return new { result = "error", message = "la contraseña nueva no es válida" };
                }

                Usuario? user = _userLogic.GetForIdUsuario(update.IdUsuario);
                if (user == null)
                {
                    return new { result = "error", message = "El usuario o la contraseña no son válidos" };
                }

                if (!PasswordHash.Verify(update.CurrentPassword, user.Contraseña))
                {
                    return new { result = "error", message = "El usuario o la contraseña no son válidos" };
                }

                if (!ValidatePassword(update.NewPassword))
                {
                    return new { result = "error", message = "La clave debe tener mínimo 8 caracteres, 1 mayúscula, 1 minúscula, 1 número y 1 caracter especial (!#$%&=*)" };
                }

                _userLogic.UpdatePassword(update.IdUsuario,update.NewPassword);

                return new
                {
                    result = "ok",
                    message = "Se actualizo correctamente"
                };
            }
            catch (Exception ex)
            {
                return new { result = "error", message = "Ocurrio un error al actualizar la contraseña" };
            }

        }
        bool ValidatePassword(string value)
        {
            Regex minLength = new Regex(@".{8,}");
            Regex hasUpperCase = new Regex(@"[A-Z]");
            Regex hasLowerCase = new Regex(@"[a-z]");
            Regex hasNumber = new Regex(@"\d");
            Regex hasSpecialChar = new Regex(@"[!#$%&=*]");

            bool isValid = minLength.IsMatch(value) &&
                           hasUpperCase.IsMatch(value) &&
                           hasLowerCase.IsMatch(value) &&
                           hasNumber.IsMatch(value) &&
                           hasSpecialChar.IsMatch(value);

            return isValid;
        }

        private string GenerateToken(IEnumerable<Claim> claims, TimeSpan validFor)
        {

            /* En esta sección comienza la creación del JWT*/

            // Primero se recuperan los datos de configuración para el JWT.
            var jwt = _jwtSettings;
            if (jwt == null
                || string.IsNullOrEmpty(jwt.Key)
                || string.IsNullOrEmpty(jwt.Subject)
            )
            {
                throw new Exception("No JWT configuration.");
            }

            // Se crea la clave de cifrado simétrico a partir de la contraseña.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

            // Crear una lista de claims sin duplicar el audience
            var claimsList = claims.Where(c => c.Type != JwtRegisteredClaimNames.Aud).ToList();

            // Se firma el token con la clave simétrica creada
            var singIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                jwt.Issuer,
                jwt.Audience,
                claimsList,
                expires: DateTime.Now.Add(validFor),
                signingCredentials: singIn
            );

            /* Fin de la creación del JWT */

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            // Primero se recuperan los datos de configuración para el JWT.
            var jwt = _jwtSettings;
            if (jwt == null
                || string.IsNullOrEmpty(jwt.Key)
                || string.IsNullOrEmpty(jwt.Subject)
            )
            {
                throw new Exception("No JWT configuration.");
            }
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true, // Ignorar la validación de la expiración
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        public async Task<RefreshTokenResponse> RefreshToken(RefreshTokenRequest tokenModel)
        {
            var principal = GetPrincipalFromExpiredToken(tokenModel.AccessToken);
            if (principal == null) return new RefreshTokenResponse { result = "error" };

            var userId = principal.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (userId == null || !ValidateRefreshToken(userId, tokenModel.RefreshToken)) return new RefreshTokenResponse { result = "error" };

            var newAccessToken = GenerateToken(principal.Claims, TimeSpan.FromMinutes(15));//15

            // Decodificar el token de refresco existente para obtener su fecha de expiración
            var handler = new JwtSecurityTokenHandler();
            var refreshTokenDecoded = handler.ReadJwtToken(tokenModel.RefreshToken);

            // Obtener la fecha de expiración del token de refresco actual
            var refreshTokenExpiry = refreshTokenDecoded.ValidTo;

            // Calcular el tiempo restante de expiración
            var timeRemaining = refreshTokenExpiry - DateTime.UtcNow;

            var newRefreshToken = GenerateToken(principal.Claims, timeRemaining);

            await SaveRefreshToken(int.Parse(userId), newRefreshToken);

            return new RefreshTokenResponse { result = "ok", AccessToken = newAccessToken, RefreshToken = newRefreshToken };


        }

        private bool ValidateRefreshToken(string userId, string refreshToken)
        {
            var user = _dataContext.Usuarios.SingleOrDefault(u => u.IdUsuario == int.Parse(userId));
            if (user == null) return false;

            if (user.token != refreshToken) return false;

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(refreshToken) as JwtSecurityToken;

            if (jwtToken == null) return false;

            var expClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Exp);
            if (expClaim == null) return false;

            var expDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim.Value)).UtcDateTime;
            return expDate > DateTime.UtcNow;
        }

        private async Task SaveRefreshToken(int userId, string refreshToken)
        {
            var user = await _dataContext.Usuarios.SingleOrDefaultAsync(u => u.IdUsuario == userId);
            if (user != null)
            {
                user.token = refreshToken;
                await _dataContext.SaveChangesAsync();

            }
        }
    }
}
