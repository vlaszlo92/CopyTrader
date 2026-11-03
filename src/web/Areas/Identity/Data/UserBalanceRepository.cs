using Microsoft.EntityFrameworkCore;
using CopyTrader.Web.Interfaces;

namespace CopyTrader.Web.Areas.Identity.Data
{
    public class UserBalanceRepository: IUserBalanceRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public UserBalanceRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddUserBalanceAsync(int id, double balance, string type)
        {
            await _dbContext.AddUserBalanceAsync(id, balance, type);
            return;
        }

        public Task<UserBalance> GetUserBalanceAsync(string userAPIId)
        {
            throw new NotImplementedException();
        }
    }
}
