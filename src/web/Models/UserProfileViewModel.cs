namespace CopyTrader.Web.Models
{
    public class UserProfileViewModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public List<UserProfileAPIViewModel> APIs { get; set; }
        public double Balance { get; set; }
        public string CurrencyType { get; set; }
        public bool IsActive { get; set; }
        public string GetSumBalanceString()
        {
            double sum = 0;
            string type = " USDT";
            foreach (var api in APIs)
            {
                sum += api.Balance;
            }
            return sum.ToString() + type;
        }
    }
   
}
