using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingApi.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
            var passwordHasher = new PasswordHasher();

            await context.Database.MigrateAsync();

            if (!await roleManager.RoleExistsAsync(UserRole.Admin))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(UserRole.Admin));
                await roleManager.CreateAsync(new IdentityRole<Guid>(UserRole.Teller));
                await roleManager.CreateAsync(new IdentityRole<Guid>(UserRole.Customer));
            }

            if (!await userManager.Users.AnyAsync())
            {
                var adminIdentity = new IdentityUser<Guid>
                {
                    Id = Guid.NewGuid(),
                    UserName = "admin@bankingapi.local",
                    Email = "admin@bankingapi.local",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(adminIdentity, "Admin@12345!");
                await userManager.AddToRoleAsync(adminIdentity, UserRole.Admin);

                var tellerIdentity = new IdentityUser<Guid>
                {
                    Id = Guid.NewGuid(),
                    UserName = "teller@bankingapi.local",
                    Email = "teller@bankingapi.local",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(tellerIdentity, "Teller@12345!");
                await userManager.AddToRoleAsync(tellerIdentity, UserRole.Teller);

                var customerIdentity = new IdentityUser<Guid>
                {
                    Id = Guid.NewGuid(),
                    UserName = "customer@bankingapi.local",
                    Email = "customer@bankingapi.local",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(customerIdentity, "Customer@12345!");
                await userManager.AddToRoleAsync(customerIdentity, UserRole.Customer);

                var adminUser = new ApplicationUser
                {
                    Id = adminIdentity.Id,
                    FirstName = "Admin",
                    LastName = "User",
                    NationalIdentityNumber = "00000000001",
                    Email = adminIdentity.Email!,
                    PhoneNumber = "08000000001",
                    PasswordHash = passwordHasher.Hash("Admin@12345!"),
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var tellerUser = new ApplicationUser
                {
                    Id = tellerIdentity.Id,
                    FirstName = "Teller",
                    LastName = "User",
                    NationalIdentityNumber = "00000000002",
                    Email = tellerIdentity.Email!,
                    PhoneNumber = "08000000002",
                    PasswordHash = passwordHasher.Hash("Teller@12345!"),
                    Role = UserRole.Teller,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var customerUser = new ApplicationUser
                {
                    Id = customerIdentity.Id,
                    FirstName = "Customer",
                    LastName = "User",
                    NationalIdentityNumber = "00000000003",
                    Email = customerIdentity.Email!,
                    PhoneNumber = "08000000003",
                    PasswordHash = passwordHasher.Hash("Customer@12345!"),
                    Role = UserRole.Customer,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                context.ApplicationUsers.AddRange(adminUser, tellerUser, customerUser);
                await context.SaveChangesAsync();

                var savingsAccount = new Account
                {
                    Id = Guid.NewGuid(),
                    AccountNumber = "0580000001",
                    AccountType = Domain.Enums.AccountType.Savings,
                    Balance = 10000.00m,
                    Status = AccountStatus.Active,
                    DailyWithdrawalLimit = 200000.00m,
                    TodayWithdrawnAmount = 0,
                    OwnerId = customerUser.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                var currentAccount = new Account
                {
                    Id = Guid.NewGuid(),
                    AccountNumber = "0580000002",
                    AccountType = Domain.Enums.AccountType.Current,
                    Balance = 50000.00m,
                    Status = AccountStatus.Active,
                    DailyWithdrawalLimit = 500000.00m,
                    TodayWithdrawnAmount = 0,
                    OwnerId = customerUser.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                context.Accounts.AddRange(savingsAccount, currentAccount);
                await context.SaveChangesAsync();
            }
        }
    }
}
