using Microsoft.EntityFrameworkCore;
using CopyTrader.Web.Interfaces;

namespace CopyTrader.Web.Areas.Identity.Data
{
    public class TraderGroupRepository : ITraderGroupRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public TraderGroupRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public Task<List<TraderGroup>> GetTraderGroupsAsync()
        {
            return _dbContext.GetTraderGroupAsync();
        }
    }
}
