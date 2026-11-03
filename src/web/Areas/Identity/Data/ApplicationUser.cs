using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace CopyTrader.Web.Areas.Identity.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public bool IsTrader { get; set; }
    public bool IsActive { get; set; }
    public DateTime RegisteredDate { get; set; }
    public double CurrencyAmount { get; set; }
    public string? CurrencyType { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}