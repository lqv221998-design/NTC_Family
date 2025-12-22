using System.Threading.Tasks;
using System.Collections.Generic;

namespace NTC.FamilyManager.Base
{
    public interface IAuthService
    {
        bool IsAuthenticated { get; }
        string UserName { get; }
        string UserEmail { get; }

        Task<bool> LoginAsync();
        Task<string> GetAccessTokenAsync();
        void Logout();
    }
}
