using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebServer.Models;
using WebServer.Models.Users;

namespace WebServer.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync(cancellationToken);

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            var hasher = new PasswordHasher<AppUser>();
            var admin = new AppUser
            {
                Username = "admin",
                Email = "admin@haifa.gov",
                Role = UserRole.Admin,
                TwoFactorSecret = Guid.NewGuid().ToString("N")
            };
            admin.PasswordHash = hasher.HashPassword(admin, "ChangeMe!123");
            context.Users.Add(admin);

            // Seed baseline building for demo
            context.Buildings.Add(new Building
            {
                FldId = "GH-0001",
                BuildingName = "Old Port House",
                StreetName = "Herzl",
                HouseNumber = "5",
                Neighborhood = "Downtown",
                BldSivug = "Abandoned",
                ShikumStatus = "Pending Survey",
                StatusSummary = "Awaiting inspection",
                Complaints = "Graffiti and loitering",
                PhotoUrls = "https://placehold.co/600x400"
            });

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
