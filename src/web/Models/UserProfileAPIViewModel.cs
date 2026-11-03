using System.ComponentModel.DataAnnotations;

namespace CopyTrader.Web.Models
{
    public class UserProfileAPIViewModel
    {
        public string? FullName { get; set; }
        public string? BinanceUserId { get; set; }
        [Required, MinLength(20)]
        public string? Key { get; set; }
        public double Balance { get; set; }
        public string? CurrencyType { get; set; }
        public bool IsActive { get; set; }

        public string IsActiveString()
        {
            return IsActive ? "Igen" : "Nem";
        }
    }

}
