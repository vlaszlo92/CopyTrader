using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace CopyTrader.Web.Models
{
    public class UserAPIKeyViewModel
    {
        [Required, MinLength(20)]
        public string? Key { get; set; }
        [Required, MinLength(20)]
        public string? Secure { get; set; }
        public string? BinanceUserId { get; set; }
        public string? FullName { get; set; }
        public double Balance { get; set; }
        public string? CurrencyType { get; set; }
        public bool IsActive { get; set; }
    }
}