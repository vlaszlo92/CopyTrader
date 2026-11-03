using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using CopyTrader.Web.Interfaces;
using CopyTrader.Web.Models;

namespace CopyTrader.Web.Areas.Identity.Data
{
    public class UserAPIKeyRepository : IUserAPIKeyRepository
    {
        private readonly ApplicationDbContext _appDbContext;

        public UserAPIKeyRepository(ApplicationDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<bool> AddUserAPIKeyAsync(UserAPI apiKey)
        {
                await _appDbContext.AddUserAPIKeyAsync(apiKey);
                return true;
        }
        public async Task<bool> ModifyUserAPIKeyAsync(UserAPI apiKey)
        {
            if (!await _appDbContext.IsUserAPIKeyExistAsync(apiKey))
            {
                await _appDbContext.UpdateUserAPIKeyAsync(apiKey);
                return true;
            }
            return false;
        }
        public async Task<List<UserProfileAPIViewModel>> GetUserAPIKeysByUserIdAsync(string userId)
        {
            var userapis = await _appDbContext.UserAPIKeys.Where(a => a.UserId == userId).ToListAsync();
            List<UserProfileAPIViewModel> upavm = new List<UserProfileAPIViewModel>();
            foreach (var userapi in userapis)
            {
                var bal = await _appDbContext.UserBalances.FirstOrDefaultAsync(b => b.UserAPIId == userapi.Id);
                var api = new UserProfileAPIViewModel
                {
                    FullName = userapi.FullName,
                    Key = userapi.APIKey,
                    BinanceUserId = userapi.BinanceUserId,
                    Balance = bal.CurrencyAmount,
                    CurrencyType = bal.CurrencyType,
                    IsActive = userapi.IsActive
                };
                upavm.Add(api);
            }
            return upavm;
        }

        public async Task<bool> IsUserAPIExistAsync(string userId)
        {
            return await _appDbContext.ValidateUserAPIAsync(userId);
        }

        public async Task<int> GetAPIIdByAPIKeyAsync(string apikey)
        {
            return await _appDbContext.GetAPIIdByAPIKeyAsync(apikey);
        }
    }
}
