using CopyTrader.Web.Areas.Identity.Data;
using CopyTrader.Web.Models;

namespace CopyTrader.Web.Interfaces
{
    public interface IUserAPIKeyRepository
    {
        Task<bool> AddUserAPIKeyAsync(UserAPI apiKey);
        Task<bool> ModifyUserAPIKeyAsync(UserAPI apiKey);
        Task<List<UserProfileAPIViewModel>> GetUserAPIKeysByUserIdAsync(string userId);
        Task<bool> IsUserAPIExistAsync(string userId);
        Task<int> GetAPIIdByAPIKeyAsync(string apikey);
    }
}
