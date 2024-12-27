using Entities.RequestModels;
using Entities.ResponseModels;

namespace Logic.ILogic
{
    public interface ISessionLogic
    {
        Task<object> Login(LoginRequest login);
        Task<object> UpdatePassword(UpdatePasswordRequest update);
        Task<RefreshTokenResponse> RefreshToken(RefreshTokenRequest tokenModel);
    }
}
