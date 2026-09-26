using CreditWorks.Web.Data;
using CreditWorks.Web.Models;
using CreditWorks.Web.Services;
using CreditWorks.Web.ViewModels;
using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CreditWorks.Web.Controllers;

public sealed class VehiclesController(CreditWorksDbContext db, VehicleService vehicles, ILogger<VehiclesController> logger) : Controller
{
    public async Task<IActionResult> Index(string? sort = "owner", string? direction = "asc", CancellationToken cancellationToken = default)
    {
        try
        {
            ViewBag.Sort = sort is "owner" or "manufacturer" or "year" or "weight" ? sort : "owner";
            ViewBag.Direction = direction?.ToLowerInvariant() == "desc" ? "desc" : "asc";
            return View(await vehicles.GetVehiclesAsync(ViewBag.Sort, ViewBag.Direction, cancellationToken));
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Unable to load the vehicle list.");
            TempData["Error"] = "Vehicles could not be loaded. Check the database connection and try again.";
            return View(new List<VehicleListItem>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(await PopulateManufacturersAsync(new VehicleCreateViewModel { YearOfManufacture = DateTime.Today.Year }, cancellationToken));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleCreateViewModel model, CancellationToken cancellationToken)
    {
        if (model.YearOfManufacture > DateTime.Today.Year)
            ModelState.AddModelError(nameof(model.YearOfManufacture), "Year of manufacture cannot be in the future.");
        if (DecimalPlaces(model.WeightKg) > 2)
            ModelState.AddModelError(nameof(model.WeightKg), "Weight may have no more than two decimal places.");

        if (ModelState.IsValid)
        {
            try
            {
                await vehicles.AddAsync(new Vehicle
                {
                    OwnerName = model.OwnerName, ManufacturerId = model.ManufacturerId,
                    YearOfManufacture = model.YearOfManufacture, WeightKg = model.WeightKg
                }, cancellationToken);
                TempData["Success"] = "Vehicle added.";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessRuleException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Unable to save a vehicle.");
                ModelState.AddModelError(string.Empty, "The vehicle could not be saved. Check the details and try again.");
            }
        }

        await PopulateManufacturersAsync(model, cancellationToken);
        return View(model);
    }

    private async Task<VehicleCreateViewModel> PopulateManufacturersAsync(VehicleCreateViewModel model, CancellationToken cancellationToken)
    {
        model.Manufacturers = await db.Manufacturers.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == model.ManufacturerId)).ToListAsync(cancellationToken);
        return model;
    }

    private static int DecimalPlaces(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0xFF;
}
