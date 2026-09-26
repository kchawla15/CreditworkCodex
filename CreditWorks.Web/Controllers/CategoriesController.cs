using CreditWorks.Web.Services;
using CreditWorks.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditWorks.Web.Controllers;

public sealed class CategoriesController(CategoryService categories, ILogger<CategoriesController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await categories.GetAllAsync(cancellationToken));

    [HttpGet]
    public IActionResult Create() => View(new CategoryCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryCreateViewModel model, CancellationToken cancellationToken)
    {
        if (DecimalPlaces(model.SplitAtKg) > 2) ModelState.AddModelError(nameof(model.SplitAtKg), "Use no more than two decimal places.");
        if (ModelState.IsValid)
        {
            try
            {
                await categories.CreateAsync(model.Name, model.Icon, model.SplitAtKg, cancellationToken);
                TempData["Success"] = "Category added and its range split successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessRuleException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Unable to create a category.");
                ModelState.AddModelError(string.Empty, "The category could not be saved. Check that its name is unique.");
            }
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var category = (await categories.GetAllAsync(cancellationToken)).SingleOrDefault(x => x.Id == id);
        return category is null ? NotFound() : View(CategoryEditViewModel.From(category));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryEditViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (DecimalPlaces(model.MinimumWeight) > 2 || model.MaximumWeight is { } max && DecimalPlaces(max) > 2)
            ModelState.AddModelError(string.Empty, "Category boundaries may have no more than two decimal places.");
        if (ModelState.IsValid)
        {
            try
            {
                await categories.UpdateAsync(id, model.Name, model.Icon, model.MinimumWeight, model.MaximumWeight, cancellationToken);
                TempData["Success"] = "Category updated. Existing vehicles now use the updated ranges.";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (BusinessRuleException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Unable to update a category.");
                ModelState.AddModelError(string.Empty, "The category could not be saved. Check that its name is unique.");
            }
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await categories.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "Category deleted. The adjacent category now covers its range.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (BusinessRuleException ex) { TempData["Error"] = ex.Message; }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Unable to delete a category.");
            TempData["Error"] = "The category could not be deleted. Please try again.";
        }
        return RedirectToAction(nameof(Index));
    }

    private static int DecimalPlaces(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0xFF;
}
