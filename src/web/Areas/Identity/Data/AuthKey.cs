namespace CopyTrader.Web.Areas.Identity.Data
{
    public class AuthKey
    {
        public int Id { get; set; }
        public string? Key { get; set; }
        public double CurrencyPool { get; set; }
        public string? CurrencyType { get; set; }
    }
}
