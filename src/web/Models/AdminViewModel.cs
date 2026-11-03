using CopyTrader.Web.Areas.Identity.Data;

namespace CopyTrader.Web.Models
{
    public class AdminViewModel
    {
        public string? UserId { get; set; }
        public List<AdminUserViewModel>? Users { get; set; }
        public List<TraderGroup>? TraderGroups { get; set; }

    }
}
