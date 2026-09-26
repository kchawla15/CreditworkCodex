using System.ComponentModel.DataAnnotations;

namespace CreditWorks.Web.Models;

public sealed class Manufacturer
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Name { get; set; } = "";
    public List<Vehicle> Vehicles { get; set; } = [];
}
