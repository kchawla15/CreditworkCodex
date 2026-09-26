using CreditWorks.Web.Models;
using CreditWorks.Web.Services;

namespace CreditWorks.Tests;

public sealed class CategoryRangeValidatorTests
{
    private static List<VehicleCategory> Defaults() =>
    [
        new() { Id = 1, Name = "Light", MinimumWeight = 0m, MaximumWeight = 500m, Icon = "car" },
        new() { Id = 2, Name = "Medium", MinimumWeight = 500m, MaximumWeight = 2500m, Icon = "van" },
        new() { Id = 3, Name = "Heavy", MinimumWeight = 2500m, MaximumWeight = null, Icon = "truck" }
    ];

    [Fact]
    public void Default_configuration_is_valid() => Assert.Empty(CategoryRangeValidator.Validate(Defaults()));

    [Theory]
    [InlineData(499.99, "Light")]
    [InlineData(500.00, "Medium")]
    [InlineData(500.01, "Medium")]
    [InlineData(2499.99, "Medium")]
    [InlineData(2500.00, "Heavy")]
    [InlineData(2500.01, "Heavy")]
    [InlineData(999999.99, "Heavy")]
    public void Category_boundaries_are_lower_inclusive_and_upper_exclusive(double value, string expected)
    {
        var category = CategoryRangeValidator.FindCategory(Defaults(), (decimal)value);
        Assert.Equal(expected, category?.Name);
    }

    [Fact]
    public void Detects_a_gap()
    {
        var categories = Defaults();
        categories[0].MaximumWeight = 400m;
        Assert.Contains(CategoryRangeValidator.Validate(categories), error => error.Contains("no gaps or overlaps"));
    }

    [Fact]
    public void Detects_an_overlap()
    {
        var categories = Defaults();
        categories[0].MaximumWeight = 600m;
        Assert.Contains(CategoryRangeValidator.Validate(categories), error => error.Contains("no gaps or overlaps"));
    }

    [Fact]
    public void Requires_the_first_category_to_start_at_zero()
    {
        var categories = Defaults();
        categories[0].MinimumWeight = 0.01m;
        Assert.Contains(CategoryRangeValidator.Validate(categories), error => error.Contains("begin at 0"));
    }

    [Fact]
    public void Rejects_invalid_minimum_and_maximum_ranges()
    {
        var categories = Defaults();
        categories[1].MinimumWeight = -1m;
        categories[1].MaximumWeight = -1m;
        var errors = CategoryRangeValidator.Validate(categories);
        Assert.Contains(errors, error => error.Contains("invalid minimum"));
        Assert.Contains(errors, error => error.Contains("maximum above its minimum"));
    }

    [Fact]
    public void Requires_a_single_open_ended_final_category()
    {
        var categories = Defaults();
        categories[1].MaximumWeight = null;
        Assert.Contains(CategoryRangeValidator.Validate(categories), error => error.Contains("Only the final category"));

        categories = Defaults();
        categories[2].MaximumWeight = 9999m;
        Assert.Contains(CategoryRangeValidator.Validate(categories), error => error.Contains("final category must have an open-ended"));
    }

    [Fact]
    public void Requires_category_data_and_supported_icons()
    {
        Assert.Contains(CategoryRangeValidator.Validate([]), error => error.Contains("At least one category"));
        var categories = Defaults();
        categories[0].Name = " "; categories[0].Icon = "<script>";
        var errors = CategoryRangeValidator.Validate(categories);
        Assert.Contains(errors, error => error.Contains("needs a name"));
        Assert.Contains(errors, error => error.Contains("supported icon"));
    }
}
