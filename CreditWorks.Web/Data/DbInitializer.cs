using CreditWorks.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditWorks.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(CreditWorksDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (!await db.Manufacturers.AnyAsync(cancellationToken))
        {
            db.Manufacturers.AddRange(new[] { "Mazda", "Mercedes", "Honda", "Ferrari", "Toyota" }
                .Select(name => new Manufacturer { Name = name }));
        }

        if (!await db.Categories.AnyAsync(cancellationToken))
        {
            db.Categories.AddRange(
                new VehicleCategory { Name = "Light", MinimumWeight = 0m, MaximumWeight = 500m, Icon = "car" },
                new VehicleCategory { Name = "Medium", MinimumWeight = 500m, MaximumWeight = 2500m, Icon = "van" },
                new VehicleCategory { Name = "Heavy", MinimumWeight = 2500m, MaximumWeight = null, Icon = "truck" });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
