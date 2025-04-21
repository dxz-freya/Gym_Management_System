using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using GymManagement.Helpers;
using GymManagement.Data;
using GymManagement.Models;
using GymManagement.Services;
using Microsoft.Extensions.FileProviders;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Runtime.InteropServices;

var context = new CustomAssemblyLoadContext();
var builder = WebApplication.CreateBuilder(args);

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
  var wkhtmltoxPath = Path.Combine(builder.Environment.WebRootPath, "lib", "pdf", "libwkhtmltox.dll");
  context.LoadUnmanagedLibrary(wkhtmltoxPath);
}





// Register services
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

builder.Services.AddDbContext<AppDbContext>(options =>
{
  options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
  options.EnableSensitiveDataLogging(); // 🔍 显示 SQL 参数，调试 LINQ 异常
});


builder.Services.AddIdentity<User, IdentityRole>(options =>
{
  options.Password.RequireDigit = true;
  options.Password.RequiredLength = 6;
  options.Password.RequireNonAlphanumeric = true;
  options.Password.RequireUppercase = true;
  options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
  options.LoginPath = "/Account/Login";
  options.AccessDeniedPath = "/Account/AccessDenied";
  options.ExpireTimeSpan = TimeSpan.FromDays(14);
  options.SlidingExpiration = true;
  options.Cookie.IsEssential = true;

  // 自动清除失效用户 Cookie
  options.Events.OnValidatePrincipal = async context =>
  {
    if (context.Principal == null)
    {
      context.RejectPrincipal();
      await context.HttpContext.SignOutAsync();
      return;
    }
    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
    var user = await userManager.GetUserAsync(context.Principal);
    if (user == null)
    {
      context.RejectPrincipal();
      await context.HttpContext.SignOutAsync();
    }
  };
});


builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
      options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
      options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
      options.CallbackPath = "/signin-google";
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
builder.Services.AddScoped<PdfService>();

var app = builder.Build();

if (args.Length > 0 && args[0].ToLower() == "seeddata")
{
  bool clearSessions = args.Contains("--clearsessions");

  using var scope = app.Services.CreateScope();
  var services = scope.ServiceProvider;

  await SeedUsers.InitializeAsync(services);
  await SeedSessions.InitializeAsync(services, clearBeforeSeed: clearSessions);

  Console.WriteLine($"✅ Seeding completed {(clearSessions ? "with session cleanup" : "without session cleanup")}.");
  return;
}


// Web app setup
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}




app.UseHttpsRedirection();
app.UseStaticFiles();

// Add static file access for profile uploads + disable cache
app.UseStaticFiles(new StaticFileOptions
{
  FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath, "uploads", "profile")),
  RequestPath = "/uploads/profile",
  OnPrepareResponse = ctx =>
  {
    ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
    ctx.Context.Response.Headers.Append("Pragma", "no-cache");
    ctx.Context.Response.Headers.Append("Expires", "0");
  }
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Dashboard}/{id?}");

app.MapControllerRoute(
    name: "customer",
    pattern: "Customer/{action=Dashboard}/{id?}",
    defaults: new { controller = "Customer" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
