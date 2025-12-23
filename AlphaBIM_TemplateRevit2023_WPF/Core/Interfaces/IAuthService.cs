using System.Threading.Tasks;

namespace NTC.FamilyManager.Core.Interfaces
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
