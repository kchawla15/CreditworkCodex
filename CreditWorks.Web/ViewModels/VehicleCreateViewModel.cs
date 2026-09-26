using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CreditWorks.Web.ViewModels;

public sealed class VehicleCreateViewModel
{
    [Required, StringLength(100)] public string OwnerName { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Select a manufacturer.")] public int ManufacturerId { get; set; }
    [Range(1886, 9999, ErrorMessage = "Enter a valid year of manufacture.")] public int YearOfManufacture { get; set; }
    [Required, Range(typeof(decimal), "0.01", "99999999.99", ErrorMessage = "Weight must be a positive number.")]
    public decimal WeightKg { get; set; }
    public IEnumerable<SelectListItem> Manufacturers { get; set; } = [];
}
