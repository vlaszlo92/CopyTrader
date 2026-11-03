using CopyTrader.Web.Areas.Identity.Data;
using CopyTrader.Web.Models;

namespace CopyTrader.Web.Interfaces
{
    public interface IApplicationUserRepository
    {
        Task<ApplicationUser> GetApplicationUserAsync(string id);
        Task<List<AdminUserViewModel>> GetAllUserAsync();
        Task<List<int>> UpdateUserAsync(List<AdminUserViewModel> users);
        Task<bool> IsTraderAsync(int id);
    }
}
