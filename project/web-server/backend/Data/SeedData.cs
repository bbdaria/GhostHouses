using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using WebServer.Models;
using WebServer.Models.Users;

namespace WebServer.Data;

public static class SeedData
{
    private const int NoStreetId = -1;
    private const string NoStreetName = "ללא שם רחוב";

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync(cancellationToken);

        Console.WriteLine("[SeedData] Database migrated.");

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            var hasher = new PasswordHasher<AppUser>();

            var admin = new AppUser
            {
                Username = "admin",
                Email = "admin@haifa.muni.il",
                Role = UserRole.Admin,
                TwoFactorSecret = Guid.NewGuid().ToString("N")
            };
            admin.PasswordHash = hasher.HashPassword(admin, "admin");
            context.Users.Add(admin);

            await context.SaveChangesAsync(cancellationToken);
        }

        var hasNoStreet = await context.Streets.AnyAsync(
            s => s.StreetId == NoStreetId,
            cancellationToken);
        if (!hasNoStreet)
        {
            context.Streets.Add(new Street { StreetId = NoStreetId, Name = NoStreetName });
            await context.SaveChangesAsync(cancellationToken);
        }
        await SyncBuildingIdSequenceAsync(context, cancellationToken);
    }

    private static async Task SyncBuildingIdSequenceAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT setval(
                pg_get_serial_sequence('"Buildings"', 'Id'),
                GREATEST(COALESCE((SELECT MAX("Id") FROM "Buildings"), 1), 1),
                true
            );
            """;
        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
