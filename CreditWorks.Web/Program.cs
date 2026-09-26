using CreditWorks.Web.Data;
using CreditWorks.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<CreditWorksDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CreditWorks")));
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<VehicleService>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Vehicles}/{action=Index}/{id?}");

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CreditWorksDbContext>();
    await DbInitializer.InitializeAsync(db);
}

app.Run();

public partial class Program;
