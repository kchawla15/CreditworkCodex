using CreditWorks.Web.Data;
using CreditWorks.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditWorks.Web.Services;

public sealed class VehicleService(CreditWorksDbContext db)
{
    public async Task<List<VehicleListItem>> GetVehiclesAsync(string? sort, string? direction, CancellationToken cancellationToken = default)
    {
        var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        var query = from vehicle in db.Vehicles
                    join manufacturer in db.Manufacturers on vehicle.ManufacturerId equals manufacturer.Id
                    select new { Vehicle = vehicle, ManufacturerName = manufacturer.Name };
        query = (sort?.ToLowerInvariant(), descending) switch
        {
            ("owner", false) => query.OrderBy(x => x.Vehicle.OwnerName),
            ("owner", true) => query.OrderByDescending(x => x.Vehicle.OwnerName),
            ("manufacturer", false) => query.OrderBy(x => x.ManufacturerName),
            ("manufacturer", true) => query.OrderByDescending(x => x.ManufacturerName),
            ("year", false) => query.OrderBy(x => x.Vehicle.YearOfManufacture),
            ("year", true) => query.OrderByDescending(x => x.Vehicle.YearOfManufacture),
            ("weight", false) => query.OrderBy(x => x.Vehicle.WeightKg),
            ("weight", true) => query.OrderByDescending(x => x.Vehicle.WeightKg),
            _ => query.OrderBy(x => x.Vehicle.OwnerName)
        };

        var vehicles = await query.Select(x => new VehicleListItem(x.Vehicle.Id, x.Vehicle.OwnerName,
            x.ManufacturerName, x.Vehicle.YearOfManufacture, x.Vehicle.WeightKg, "", ""))
            .ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        return vehicles.Select(vehicle =>
        {
            var category = CategoryRangeValidator.FindCategory(categories, vehicle.WeightKg);
            if (category is null) throw new BusinessRuleException("The current category configuration does not cover this vehicle's weight.");
            return vehicle with { CategoryName = category.Name, CategoryIcon = category.Icon };
        }).ToList();
    }

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(vehicle.OwnerName)) errors.Add("Owner name is required.");
        if (vehicle.OwnerName is { Length: > 100 }) errors.Add("Owner name cannot exceed 100 characters.");
        if (vehicle.YearOfManufacture < 1886 || vehicle.YearOfManufacture > DateTime.Today.Year)
            errors.Add($"Year must be between 1886 and {DateTime.Today.Year}.");
        if (vehicle.WeightKg <= 0 || DecimalPlaces(vehicle.WeightKg) > 2)
            errors.Add("Weight must be positive and have no more than two decimal places.");
        if (!await db.Manufacturers.AnyAsync(x => x.Id == vehicle.ManufacturerId, cancellationToken))
            errors.Add("Select a valid manufacturer.");
        var categories = await db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var categoryErrors = CategoryRangeValidator.Validate(categories);
        if (categoryErrors.Count > 0) errors.Add("Vehicle categories are not configured correctly.");
        else if (CategoryRangeValidator.FindCategory(categories, vehicle.WeightKg) is null)
            errors.Add("The vehicle weight is not covered by a category.");
        if (errors.Count > 0) throw new BusinessRuleException(string.Join(" ", errors));

        vehicle.OwnerName = vehicle.OwnerName.Trim();
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static int DecimalPlaces(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0xFF;
}

public sealed record VehicleListItem(int Id, string OwnerName, string Manufacturer, int YearOfManufacture,
    decimal WeightKg, string CategoryName, string CategoryIcon);
