using System.ComponentModel.DataAnnotations;
using CreditWorks.Web.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CreditWorks.Web.ViewModels;

public sealed class CategoryCreateViewModel
{
    [Required, StringLength(60)] public string Name { get; set; } = "";
    [Required, RegularExpression("^(car|van|truck|bus|motorcycle)$", ErrorMessage = "Choose a supported icon.")]
    public string Icon { get; set; } = "car";
    [Range(typeof(decimal), "0.01", "99999999.99", ErrorMessage = "Enter a valid split point.")]
    public decimal SplitAtKg { get; set; }
    public IEnumerable<SelectListItem> Icons { get; set; } = CategoryRangeValidator.Icons.Select(x => new SelectListItem(x, x));
}
