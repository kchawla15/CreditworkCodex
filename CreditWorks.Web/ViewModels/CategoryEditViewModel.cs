using System.ComponentModel.DataAnnotations;
using CreditWorks.Web.Models;
using CreditWorks.Web.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CreditWorks.Web.ViewModels;

public sealed class CategoryEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(60)] public string Name { get; set; } = "";
    [Required, RegularExpression("^(car|van|truck|bus|motorcycle)$", ErrorMessage = "Choose a supported icon.")]
    public string Icon { get; set; } = "car";
    [Range(typeof(decimal), "0", "99999999.99")] public decimal MinimumWeight { get; set; }
    [Range(typeof(decimal), "0", "99999999.99")] public decimal? MaximumWeight { get; set; }
    public IEnumerable<SelectListItem> Icons { get; set; } = CategoryRangeValidator.Icons.Select(x => new SelectListItem(x, x));

    public static CategoryEditViewModel From(VehicleCategory category) => new()
    {
        Id = category.Id, Name = category.Name, Icon = category.Icon,
        MinimumWeight = category.MinimumWeight, MaximumWeight = category.MaximumWeight
    };
}
