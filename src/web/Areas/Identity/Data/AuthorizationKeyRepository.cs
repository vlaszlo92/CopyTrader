using Microsoft.EntityFrameworkCore;
using CopyTrader.Web.Interfaces;

namespace CopyTrader.Web.Areas.Identity.Data
{
    public class AuthorizationKeyRepository : IAuthorizationKeyRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public AuthorizationKeyRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<AuthKey?> AuthKeyExistsAsync(string authKey)
        {
            var authKeyEntity = await _dbContext.AuthKeys.FirstOrDefaultAsync(a => a.Key == authKey);
            if(authKeyEntity == null)
            {
                return null;
            }
            return new AuthKey
            {
                Id = authKeyEntity.Id,
                Key = authKeyEntity.Key,
                CurrencyPool = authKeyEntity?.CurrencyPool ??0.0f,
                CurrencyType = authKeyEntity?.CurrencyType ??"BUSD",
            };
        }

        public async Task<bool> DeleteAuthKeyAsync(string authKey)
        {
            return await _dbContext.DeleteAuthKeyAsync(authKey);
        }
    }
}
