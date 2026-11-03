using CopyTrader.Web.Areas.Identity.Data;

namespace CopyTrader.Web.Interfaces
{
    public interface IUserBalanceRepository
    {
        Task AddUserBalanceAsync(int id, double balance, string type);
        Task<UserBalance> GetUserBalanceAsync(string userAPIId);
    }
}
