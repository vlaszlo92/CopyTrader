using System;
using System.Collections.Generic;

namespace CopyTrader.Web.Areas.Identity.Data;

public partial class AspNetUser
{
    public string Id { get; set; } = null!;

    public string? UserName { get; set; }

    public string? NormalizedUserName { get; set; }

    public string? Email { get; set; }

    public string? NormalizedEmail { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public bool LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public bool? IsTrader { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? RegisteredDate { get; set; }

    public double? CurrencyAmount { get; set; }

    public string? CurrencyType { get; set; }

    public virtual ICollection<AspNetRole> Roles { get; } = new List<AspNetRole>();

    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}
