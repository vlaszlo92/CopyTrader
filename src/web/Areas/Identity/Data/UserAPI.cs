namespace CopyTrader.Web.Areas.Identity.Data
{
    public class UserAPI
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? APIKey { get; set; }
        public string? Secure { get; set; }
        public bool IsActive { get; set; }
        public int? TraderGroupId { get; set; }
        public string? FullName { get; set; }
        public string BinanceUserId { get; set; }
    }
}
