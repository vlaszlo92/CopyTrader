using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CopyTrader.Web.Areas.Identity.Data;
using CopyTrader.Web.Interfaces;
using CopyTrader.Web.Models;

namespace CopyTrader.Web.Areas.Identity.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    public DbSet<AuthKey> AuthKeys { get; set; }
    public DbSet<UserAPI> UserAPIKeys { get; set; }
    public DbSet<UserBalance> UserBalances { get; set; }
    public DbSet<TraderGroup> TraderGroups { get; set; }
    public DbSet<Trader> Traders { get; set; }
    public DbSet<IdentityUserRole<string>> UserRoles { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);

        builder.ApplyConfiguration(new ApplicationUserEntityConfiguration());
        builder.ApplyConfiguration(new AuthKeyEntityConfiguration());
        builder.ApplyConfiguration(new UserAPIKeyEntityConfiguration());
        builder.ApplyConfiguration(new UserBalanceEntityConfiguration());
        builder.ApplyConfiguration(new TraderGroupEntityConfiguration());
        builder.ApplyConfiguration(new TraderEntityConfiguration());
        builder.ApplyConfiguration(new UserRoleEntityConfiguration());
    }

    public async Task<ApplicationUser> GetUserAsync(string id)
    {
        return await ApplicationUsers.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<bool> AuthKeyExistsAsync(string authKey)
    {
        return await AuthKeys.AnyAsync(k => k.Key == authKey);
    }

    public async Task<bool> DeleteAuthKeyAsync(string authenticationKey)
    {
        var key = await AuthKeys.SingleOrDefaultAsync(k => k.Key == authenticationKey);
        if (key != null)
        {
            AuthKeys.Remove(key);
            await SaveChangesAsync();
            return true;
        }
        return false;
    }


    public async Task<List<int>> UpdateUserAsync(List<AdminUserViewModel> users)
    {
        var invalidUsers = new List<int>();
        foreach (var user in users)
        {
            var isValidTraderGroup = await TraderGroups.AnyAsync(tg => tg.Id == user.TraderGroupId);
            if (isValidTraderGroup || user.TraderGroupId == null)
            {
                // Handle the case when the TraderGroup Id is not valid (e.g., display an error message or take appropriate action)
                // For example, you can throw an exception:

                var dbUser = await UserAPIKeys.FirstOrDefaultAsync(a => a.Id == user.Id);
                if (dbUser != null)
                {
                    dbUser.TraderGroupId = user.TraderGroupId;
                    await SaveChangesAsync();
                }
            } else
            {
                invalidUsers.Add(user.Id);
                user.TraderGroupId = UserAPIKeys.FirstAsync(u => u.Id == user.Id).Result.TraderGroupId;
                continue;
            }
        }
        return invalidUsers;
    }

    public async Task<List<UserAPI>> GetAllUserapisAsync()
    {
        return await UserAPIKeys.ToListAsync();
    }
    public async Task AddUserAPIKeyAsync(UserAPI apiKey)
    {
        await UserAPIKeys.AddAsync(apiKey);
        await SaveChangesAsync();
    }

    public async Task UpdateUserAPIKeyAsync(UserAPI apiKey)
    {
        var existingApiKey = await UserAPIKeys.FirstOrDefaultAsync(U => U.UserId == apiKey.UserId);

        if (existingApiKey != null)
        {
            existingApiKey.APIKey = apiKey.APIKey;
            existingApiKey.Secure = apiKey.Secure;
        }
        await SaveChangesAsync();
    }

    public async Task<bool> IsUserAPIKeyExistAsync(UserAPI apiKey)
    {
        var existingApiKey = await UserAPIKeys.FirstOrDefaultAsync(a => a.APIKey == apiKey.APIKey || a.Secure == apiKey.Secure);

        if (existingApiKey != null)
        {
            return true;
        }
        return false;
    }
    public async Task<bool> IsUserExistAsync(string userId)
    {
        return await UserAPIKeys.AnyAsync(u => u.UserId == userId);
    }

    public async Task<bool> ValidateUserAPIAsync(string userId)
    {
        var userAPI = await UserAPIKeys.FirstOrDefaultAsync(u => u.UserId == userId);
        if (userAPI != null)
        {
            if (userAPI.APIKey != null && userAPI.APIKey.Length > 0 && userAPI.Secure != null && userAPI.Secure.Length > 0)
            {
                return true;
            }
        }
        return false;
    }

    public async Task<int> GetAPIIdByAPIKeyAsync(string apiKey)
    {
        var userapi = await UserAPIKeys.SingleOrDefaultAsync(a => a.APIKey == apiKey);
        if (userapi != null)
        {
            return userapi.Id;
        }
        return -1;
    }

    public async Task AddUserBalanceAsync(int id, double bal, string type)
    {
        var balance = new UserBalance
        {
            UserAPIId = id,
            CurrencyAmount = bal,
            CurrentAmount = bal,
            CurrencyType = type
        };
        await UserBalances.AddAsync(balance);
        await SaveChangesAsync();
    }

    public async Task<List<TraderGroup>> GetTraderGroupAsync()
    {
        return await TraderGroups.ToListAsync();
    }
    public async Task<bool> IsTraderAsync(int id)
    {
        return await Traders.Where(t => t.UserApiId == id).AnyAsync();
    }

  
}

public class UserAPIKeyEntityConfiguration : IEntityTypeConfiguration<UserAPI>
{
    public void Configure(EntityTypeBuilder<UserAPI> builder)
    {
        builder.ToTable("UserAPIs");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Id)
              .ValueGeneratedOnAdd();

        builder.Property(k => k.APIKey)
               .HasMaxLength(450);

        builder.Property(k => k.Secure)
               .HasMaxLength(450);
    }
}

public class ApplicationUserEntityConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AspNetUsers");
    }
}

public class AuthKeyEntityConfiguration : IEntityTypeConfiguration<AuthKey>
{
    public void Configure(EntityTypeBuilder<AuthKey> builder)
    {
        builder.ToTable("AuthKeys");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Key)
               .HasMaxLength(100)
               .IsRequired();
    }
}

public class UserBalanceEntityConfiguration : IEntityTypeConfiguration<UserBalance>
{
    public void Configure(EntityTypeBuilder<UserBalance> builder)
    {
        builder.ToTable("Balances");
        builder.HasKey(k => k.Id);
    }
}

public class TraderGroupEntityConfiguration : IEntityTypeConfiguration<TraderGroup>
{
    public void Configure(EntityTypeBuilder<TraderGroup> builder)
    {
        builder.ToTable("TraderGroups");
        builder.HasKey(k => k.Id);
    }
}

public class TraderEntityConfiguration : IEntityTypeConfiguration<Trader>
{
    public void Configure(EntityTypeBuilder<Trader> builder)
    {
        builder.ToTable("Traders");
        builder.HasKey(k => k.Id);
    }
}

public class UserRoleEntityConfiguration : IEntityTypeConfiguration<IdentityUserRole<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder)
    {
        builder.ToTable("AspNetUserRoles");
        builder.HasKey(k => k.UserId);
    }
}