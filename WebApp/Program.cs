using Microsoft.EntityFrameworkCore;
using WebApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Database configuration
var databaseType = builder.Configuration.GetValue<string>("DatabaseType") ?? "sqlite";
if (databaseType == "mssql")
{
    var mssqlConnection = builder.Configuration.GetConnectionString("MssqlConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(mssqlConnection));
}
else
{
    var sqliteConnection = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(sqliteConnection));
}

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(db);
}

app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action}/{id?}",
    defaults: new { controller = "Account", action = "Login" });

app.MapGet("/", context =>
{
    context.Response.Redirect("/account/login");
    return Task.CompletedTask;
});

app.Run();
