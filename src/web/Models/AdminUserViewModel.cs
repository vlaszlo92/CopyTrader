namespace CopyTrader.Web.Models
{
    public class AdminUserViewModel
    {
        public int Id { get; set; }
        public string? ApiKey { get; set; }
        public string? FullName { get; set; }
        public int? TraderGroupId { get; set; }
        public bool IsTrader { get; set; }
    }
}
