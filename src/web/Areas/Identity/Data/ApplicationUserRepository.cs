using Microsoft.EntityFrameworkCore;
using CopyTrader.Web.Interfaces;
using CopyTrader.Web.Models;

namespace CopyTrader.Web.Areas.Identity.Data
{
    public class ApplicationUserRepository : IApplicationUserRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ApplicationUserRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<ApplicationUser> GetApplicationUserAsync(string id)
        {
            return await _dbContext.GetUserAsync(id);
        }

        public async Task<List<AdminUserViewModel>> GetAllUserAsync() {
            //TODO: write this properly before release. userapi getter instead of user

            var list = await _dbContext.GetAllUserapisAsync();
            var adminUserList = new List<AdminUserViewModel>();
            foreach (var item in list)
            {
                var dbUser = await _dbContext.GetUserAsync(item.UserId);
                var user = new AdminUserViewModel
                {
                    Id = item.Id,
                    ApiKey = item.APIKey,
                    TraderGroupId = item.TraderGroupId,
                    FullName = item.FullName,
                    IsTrader = await IsTraderAsync(item.Id)
                };
                adminUserList.Add(user);
            }
            return adminUserList;
        }

        public async Task<bool> IsTraderAsync(int id)
        {
            return await _dbContext.IsTraderAsync(id);
        }

        public async Task<List<int>> UpdateUserAsync(List<AdminUserViewModel> users)
        {
            return await _dbContext.UpdateUserAsync(users);
        }
    }
}