using CreditWorks.Web.Models;

namespace CreditWorks.Web.Services;

public static class CategoryRangeValidator
{
    private static readonly HashSet<string> AllowedIcons = new(StringComparer.OrdinalIgnoreCase)
        { "car", "van", "truck", "bus", "motorcycle" };

    public static IReadOnlyCollection<string> Icons => AllowedIcons;

    public static IReadOnlyList<string> Validate(IReadOnlyCollection<VehicleCategory> categories)
    {
        var errors = new List<string>();
        if (categories.Count == 0) return ["At least one category is required."];

        foreach (var category in categories)
        {
            if (string.IsNullOrWhiteSpace(category.Name)) errors.Add("Every category needs a name.");
            if (category.Name?.Length > 60) errors.Add("Category names may not exceed 60 characters.");
            if (string.IsNullOrWhiteSpace(category.Icon) || !AllowedIcons.Contains(category.Icon))
                errors.Add($"Category '{category.Name}' must use a supported icon.");
            if (category.MinimumWeight < 0 || DecimalPlaces(category.MinimumWeight) > 2)
                errors.Add($"Category '{category.Name}' has an invalid minimum weight.");
            if (category.MaximumWeight is { } max &&
                (max <= category.MinimumWeight || DecimalPlaces(max) > 2))
                errors.Add($"Category '{category.Name}' must have a maximum above its minimum, with at most two decimals.");
        }

        if (categories.Select(x => x.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != categories.Count)
            errors.Add("Category names must be unique.");

        var ordered = categories.OrderBy(x => x.MinimumWeight).ToList();
        if (ordered[0].MinimumWeight != 0m) errors.Add("The first category must begin at 0 kg.");
        for (var i = 0; i < ordered.Count - 1; i++)
        {
            if (ordered[i].MaximumWeight is null)
                errors.Add("Only the final category may have an open-ended maximum.");
            else if (ordered[i].MaximumWeight != ordered[i + 1].MinimumWeight)
                errors.Add($"'{ordered[i].Name}' and '{ordered[i + 1].Name}' must meet at the same boundary (no gaps or overlaps).");
        }
        if (ordered[^1].MaximumWeight is not null)
            errors.Add("The final category must have an open-ended maximum.");

        return errors;
    }

    public static VehicleCategory? FindCategory(IEnumerable<VehicleCategory> categories, decimal weightKg) =>
        categories.SingleOrDefault(category => weightKg >= category.MinimumWeight &&
            (category.MaximumWeight is null || weightKg < category.MaximumWeight.Value));

    private static int DecimalPlaces(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0xFF;
}
