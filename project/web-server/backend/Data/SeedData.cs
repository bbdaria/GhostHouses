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
        var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        await context.Database.MigrateAsync(cancellationToken);

        Console.WriteLine("[SeedData] Database migrated.");

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
            admin.PasswordHash = hasher.HashPassword(admin, "admin");
            context.Users.Add(admin);

            var editor = new AppUser
            {
                Username = "editor",
                Email = "editor@haifa.gov",
                Role = UserRole.Editor,
                TwoFactorSecret = Guid.NewGuid().ToString("N")
            };
            editor.PasswordHash = hasher.HashPassword(editor, "editor");
            context.Users.Add(editor);

            var viewer = new AppUser
            {
                Username = "viewer",
                Email = "viewer@haifa.gov",
                Role = UserRole.Viewer,
                TwoFactorSecret = Guid.NewGuid().ToString("N")
            };
            viewer.PasswordHash = hasher.HashPassword(viewer, "viewer");
            context.Users.Add(viewer);

            await context.SaveChangesAsync(cancellationToken);
        }

        var seedFilePath = Path.Combine(hostEnvironment.ContentRootPath, "Data", "BuildingsSeedData.xlsx");

        if (!await context.Streets.AnyAsync(cancellationToken))
        {
            Console.WriteLine("[SeedData] Seeding streets from Excel...");
            await StreetsExcelImporter.SeedFromFileAsync(context, seedFilePath, cancellationToken);
        }
        else
        {
            Console.WriteLine("[SeedData] Streets already present, skipping street seed.");
        }

        await EnsureNoStreetAsync(context, cancellationToken);

        if (!await context.Buildings.AnyAsync(cancellationToken))
        {
            Console.WriteLine("[SeedData] Seeding buildings from Excel...");
            await BuildingsExcelImporter.SeedFromFileAsync(context, seedFilePath, cancellationToken);
        }
        else
        {
            Console.WriteLine("[SeedData] Buildings already present, skipping building seed.");
        }
    }

    private static async Task EnsureNoStreetAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Streets.AnyAsync(s => s.StreetId == NoStreetId, cancellationToken))
        {
            return;
        }

        context.Streets.Add(new Street
        {
            StreetId = NoStreetId,
            Name = NoStreetName
        });
        await context.SaveChangesAsync(cancellationToken);
        Console.WriteLine("[SeedData] Added 'ללא שם רחוב' to streets list.");
    }
}
