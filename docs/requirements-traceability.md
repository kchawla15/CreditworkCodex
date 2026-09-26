# Assignment requirements traceability

This checklist maps the assignment requirements to implementation and verification. `Verified` means the behavior is covered by a successful automated test or inspected configuration; SQL Server runtime verification remains a separate environment check.

| Requirement | Implementation | Automated verification | Status |
|---|---|---|---|
| Vehicle fields, manufacturer lookup, year/weight validation | `Models/Vehicle.cs`, `ViewModels/VehicleCreateViewModel.cs`, `Services/VehicleService.cs`, `Controllers/VehiclesController.cs` | `ApplicationBehaviorTests.Rejects_invalid_vehicle_data` | Verified |
| Seed the five initial manufacturers | `Data/DbInitializer.cs` | Seed configuration inspected; database startup requires SQL Server | Configured |
| Initial weight categories and icons | `Data/DbInitializer.cs` | `CategoryRangeValidatorTests.Default_configuration_is_valid` | Verified |
| Complete coverage, no gaps/overlaps, one open-ended final category | `Services/CategoryRangeValidator.cs`, `Services/CategoryService.cs` | Gap, overlap, zero-origin, range and final-category tests | Verified |
| Boundary behavior | `Services/CategoryRangeValidator.cs` | `Category_boundaries_are_lower_inclusive_and_upper_exclusive` | Verified |
| Create/edit/delete category while preserving coverage | `Controllers/CategoriesController.cs`, `Services/CategoryService.cs` | `Creating_and_deleting_category_preserves_complete_coverage`, `Updating_a_boundary_updates_the_neighboring_range` | Verified |
| Existing vehicles recalculate after category edits without weight change | `Services/VehicleService.cs` | `Existing_vehicle_uses_changed_category_and_keeps_its_weight` | Verified |
| Vehicle list and four ascending/descending sorts | `Controllers/VehiclesController.cs`, `Services/VehicleService.cs`, `Views/Vehicles/Index.cshtml` | `Sorts_by_each_supported_field_in_both_directions` | Verified |
| SQL Server relational persistence, migrations, decimal precision | `Data/CreditWorksDbContext.cs`, `Data/Migrations/`, `appsettings.json` | Migration applied to LocalDB; app startup and seed path completed | Verified |
| MVC Razor UI, validation, anti-forgery, safe error handling | `Controllers/`, `Views/`, `Program.cs` | Build/test and source inspection | Verified by inspection |
| xUnit tests and run documentation | `CreditWorks.Tests/`, root `README.md` | Full test suite | Verified: 32 passing |
| Git repository and clean generated files | Root `.gitignore`; generated output ignored | Local commit `0b5dc71` pushed to `origin/main` | Verified |
