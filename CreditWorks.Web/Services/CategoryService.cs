using CreditWorks.Web.Data;
using CreditWorks.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditWorks.Web.Services;

public sealed class CategoryService(CreditWorksDbContext db)
{
    public Task<List<VehicleCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
        db.Categories.OrderBy(x => x.MinimumWeight).ToListAsync(cancellationToken);

    public async Task CreateAsync(string name, string icon, decimal splitAtKg, CancellationToken cancellationToken = default)
    {
        var current = await GetAllAsync(cancellationToken);
        var owner = current.SingleOrDefault(x => splitAtKg > x.MinimumWeight &&
            (x.MaximumWeight is null || splitAtKg < x.MaximumWeight.Value));
        if (owner is null) throw new BusinessRuleException("Choose a split point strictly inside an existing category range.");

        var category = new VehicleCategory
        {
            Name = name.Trim(), Icon = icon, MinimumWeight = splitAtKg, MaximumWeight = owner.MaximumWeight
        };
        var oldMaximum = owner.MaximumWeight;
        owner.MaximumWeight = splitAtKg;
        current.Add(category);
        EnsureValid(current);
        try
        {
            db.Categories.Add(category);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            owner.MaximumWeight = oldMaximum;
            throw;
        }
    }

    public async Task UpdateAsync(int id, string name, string icon, decimal minimumWeight,
        decimal? maximumWeight, CancellationToken cancellationToken = default)
    {
        var categories = await GetAllAsync(cancellationToken);
        var current = categories.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("Category not found.");
        var ordered = categories.OrderBy(x => x.MinimumWeight).ToList();
        var index = ordered.IndexOf(current);
        if (index == 0 && minimumWeight != 0m)
            throw new BusinessRuleException("The first category must continue to start at 0 kg.");
        if (index < ordered.Count - 1 && maximumWeight is null)
            throw new BusinessRuleException("Only the final category can have an open-ended maximum.");
        if (index == ordered.Count - 1 && maximumWeight is not null)
            throw new BusinessRuleException("The final category must have an open-ended maximum.");

        var oldName = current.Name;
        var oldIcon = current.Icon;
        var oldMinimum = current.MinimumWeight;
        var oldMaximum = current.MaximumWeight;
        var previous = index > 0 ? ordered[index - 1] : null;
        var next = index + 1 < ordered.Count ? ordered[index + 1] : null;
        var previousMaximum = previous?.MaximumWeight;
        var nextMinimum = next?.MinimumWeight;

        current.Name = name.Trim();
        current.Icon = icon;
        current.MinimumWeight = minimumWeight;
        current.MaximumWeight = maximumWeight;
        if (previous is not null) previous.MaximumWeight = minimumWeight;
        if (next is not null) next.MinimumWeight = maximumWeight!.Value;

        var errors = CategoryRangeValidator.Validate(categories);
        if (errors.Count > 0)
        {
            current.Name = oldName; current.Icon = oldIcon; current.MinimumWeight = oldMinimum; current.MaximumWeight = oldMaximum;
            if (previous is not null) previous.MaximumWeight = previousMaximum;
            if (next is not null) next.MinimumWeight = nextMinimum!.Value;
            throw new BusinessRuleException(string.Join(" ", errors));
        }

        try { await db.SaveChangesAsync(cancellationToken); }
        catch
        {
            current.Name = oldName; current.Icon = oldIcon; current.MinimumWeight = oldMinimum; current.MaximumWeight = oldMaximum;
            if (previous is not null) previous.MaximumWeight = previousMaximum;
            if (next is not null) next.MinimumWeight = nextMinimum!.Value;
            throw;
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var categories = await GetAllAsync(cancellationToken);
        var current = categories.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("Category not found.");
        if (categories.Count < 2) throw new BusinessRuleException("At least one category must remain.");
        var ordered = categories.OrderBy(x => x.MinimumWeight).ToList();
        var index = ordered.IndexOf(current);
        var neighbor = index > 0 ? ordered[index - 1] : ordered[1];
        if (index > 0) neighbor.MaximumWeight = current.MaximumWeight;
        else neighbor.MinimumWeight = 0m;
        db.Categories.Remove(current);
        var remaining = categories.Where(x => x.Id != id).ToList();
        EnsureValid(remaining);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureValid(IReadOnlyCollection<VehicleCategory> categories)
    {
        var errors = CategoryRangeValidator.Validate(categories);
        if (errors.Count > 0) throw new BusinessRuleException(string.Join(" ", errors));
    }
}
