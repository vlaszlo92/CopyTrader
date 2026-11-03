using CopyTrader.Web.Areas.Identity.Data;

namespace CopyTrader.Web.Interfaces
{
    public interface ITraderGroupRepository
    {
        Task<List<TraderGroup>> GetTraderGroupsAsync();
    }
}