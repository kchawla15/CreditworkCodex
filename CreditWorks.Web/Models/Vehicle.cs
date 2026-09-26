using System.ComponentModel.DataAnnotations;

namespace CreditWorks.Web.Models;

public sealed class Vehicle
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string OwnerName { get; set; } = "";
    [Range(1, int.MaxValue)] public int ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; } = null!;
    [Range(1886, 9999)] public int YearOfManufacture { get; set; }
    [Range(typeof(decimal), "0.01", "99999999.99")] public decimal WeightKg { get; set; }
}
