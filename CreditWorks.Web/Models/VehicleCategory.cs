using System.ComponentModel.DataAnnotations;

namespace CreditWorks.Web.Models;

public sealed class VehicleCategory
{
    public int Id { get; set; }
    [Required, StringLength(60)] public string Name { get; set; } = "";
    [Range(typeof(decimal), "0", "99999999.99")] public decimal MinimumWeight { get; set; }
    [Range(typeof(decimal), "0", "99999999.99")] public decimal? MaximumWeight { get; set; }
    [Required, StringLength(32)] public string Icon { get; set; } = "";
}
