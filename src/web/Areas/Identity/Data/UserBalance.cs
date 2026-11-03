namespace CopyTrader.Web.Areas.Identity.Data
{
    public class UserBalance
    {
        public int Id { get; set; }
        public int UserAPIId { get; set; }
        public double CurrencyAmount { get; set; }
        public double CurrentAmount { get; set; }
        public string? CurrencyType { get; set; }
    }
}
