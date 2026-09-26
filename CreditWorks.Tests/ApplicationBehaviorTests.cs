using CreditWorks.Web.Data;
using CreditWorks.Web.Models;
using CreditWorks.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace CreditWorks.Tests;

public sealed class ApplicationBehaviorTests
{
    private static CreditWorksDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CreditWorksDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new CreditWorksDbContext(options);
        db.Manufacturers.AddRange(
            new Manufacturer { Id = 1, Name = "Mazda" },
            new Manufacturer { Id = 2, Name = "Mercedes" },
            new Manufacturer { Id = 3, Name = "Honda" });
        db.Categories.AddRange(
            new VehicleCategory { Id = 1, Name = "Light", MinimumWeight = 0m, MaximumWeight = 500m, Icon = "car" },
            new VehicleCategory { Id = 2, Name = "Medium", MinimumWeight = 500m, MaximumWeight = 2500m, Icon = "van" },
            new VehicleCategory { Id = 3, Name = "Heavy", MinimumWeight = 2500m, MaximumWeight = null, Icon = "truck" });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Updating_a_boundary_updates_the_neighboring_range()
    {
        await using var db = CreateDb();
        var service = new CategoryService(db);

        await service.UpdateAsync(2, "Medium", "van", 500m, 2000m);

        var light = await db.Categories.SingleAsync(x => x.Name == "Light");
        var medium = await db.Categories.SingleAsync(x => x.Name == "Medium");
        var heavy = await db.Categories.SingleAsync(x => x.Name == "Heavy");
        Assert.Equal(500m, light.MaximumWeight);
        Assert.Equal(2000m, medium.MaximumWeight);
        Assert.Equal(2000m, heavy.MinimumWeight);
        Assert.Empty(CategoryRangeValidator.Validate([light, medium, heavy]));
    }

    [Fact]
    public async Task Existing_vehicle_uses_changed_category_and_keeps_its_weight()
    {
        await using var db = CreateDb();
        db.Vehicles.Add(new Vehicle { Id = 1, OwnerName = "Alex", ManufacturerId = 1, YearOfManufacture = 2019, WeightKg = 2200m });
        await db.SaveChangesAsync();
        var service = new CategoryService(db);

        await service.UpdateAsync(2, "Medium", "van", 500m, 2000m);
        var result = await new VehicleService(db).GetVehiclesAsync("owner", "asc");

        Assert.Equal("Heavy", Assert.Single(result).CategoryName);
        Assert.Equal(2200m, result[0].WeightKg);
    }

    [Fact]
    public async Task Creating_and_deleting_category_preserves_complete_coverage()
    {
        await using var db = CreateDb();
        var service = new CategoryService(db);

        await service.CreateAsync("Compact", "bus", 1000m);
        var afterCreate = await service.GetAllAsync();
        Assert.Equal(4, afterCreate.Count);
        Assert.Empty(CategoryRangeValidator.Validate(afterCreate));
        Assert.Equal("Compact", CategoryRangeValidator.FindCategory(afterCreate, 1000m)?.Name);

        await service.DeleteAsync(afterCreate.Single(x => x.Name == "Compact").Id);
        Assert.Empty(CategoryRangeValidator.Validate(await service.GetAllAsync()));
    }

    [Theory]
    [InlineData("", 1, 2019, 1200.00, "Owner name is required")]
    [InlineData("Casey", 99, 2019, 1200.00, "valid manufacturer")]
    [InlineData("Casey", 1, 1885, 1200.00, "Year must")]
    [InlineData("Casey", 1, 9999, 1200.00, "Year must")]
    [InlineData("Casey", 1, 2019, 0.00, "Weight must be positive")]
    [InlineData("Casey", 1, 2019, -3.00, "Weight must be positive")]
    [InlineData("Casey", 1, 2019, 10.123, "two decimal places")]
    public async Task Rejects_invalid_vehicle_data(string owner, int manufacturerId, int year, double weight, string expectedMessage)
    {
        await using var db = CreateDb();
        var service = new VehicleService(db);
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.AddAsync(new Vehicle
        {
            OwnerName = owner, ManufacturerId = manufacturerId, YearOfManufacture = year, WeightKg = (decimal)weight
        }));
        Assert.Contains(expectedMessage, error.Message);
        Assert.Empty(await db.Vehicles.ToListAsync());
    }

    public static IEnumerable<object[]> SortCases()
    {
        yield return ["owner", "asc", "Alex"];
        yield return ["owner", "desc", "Zoe"];
        yield return ["manufacturer", "asc", "Honda"];
        yield return ["manufacturer", "desc", "Mercedes"];
        yield return ["year", "asc", "Alex"];
        yield return ["year", "desc", "Zoe"];
        yield return ["weight", "asc", "Alex"];
        yield return ["weight", "desc", "Zoe"];
    }

    [Theory]
    [MemberData(nameof(SortCases))]
    public async Task Sorts_by_each_supported_field_in_both_directions(string sort, string direction, string expectedFirstValue)
    {
        await using var db = CreateDb();
        db.Vehicles.AddRange(
            new Vehicle { OwnerName = "Zoe", ManufacturerId = 2, YearOfManufacture = 2022, WeightKg = 2100m },
            new Vehicle { OwnerName = "Alex", ManufacturerId = 3, YearOfManufacture = 2016, WeightKg = 600m },
            new Vehicle { OwnerName = "Mo", ManufacturerId = 1, YearOfManufacture = 2020, WeightKg = 1500m });
        await db.SaveChangesAsync();

        var result = await new VehicleService(db).GetVehiclesAsync(sort, direction);

        Assert.Equal(expectedFirstValue, sort == "manufacturer" ? result[0].Manufacturer : result[0].OwnerName);
    }
}
