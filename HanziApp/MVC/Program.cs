using DatabaseAccess;
using Interfaces.DatabaseAccess;
using Interfaces.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Services.CRUD;
using Services.FlashcardsPractice;
using Services.AddLists;
using Services;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// rate limiting
var configuration = builder.Configuration;
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// hendla DI
builder.Services.AddScoped<IExecuteSQLquery, ExecuteSQLquery>();
builder.Services.AddScoped<ISharedCRUDService, SharedCRUDService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAddListsService, AddListsService>();
builder.Services.AddScoped<IFlashcardService, FlashcardService>();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<IFlashcardPracticeService, FlashcardPracticeService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IEmailsService, EmailsService>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration); // DI u ExecuteSQLquery

builder.Services.AddSingleton<ConcurrentDictionary<string, IMemoryCache>>(); // progress bar
// ----------

// za autentikaciju
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = new PathString("/Authentication/Login");
        options.ExpireTimeSpan = TimeSpan.FromMinutes(720); // 12 hours
    });
// ----------

// za session data
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
});
// ----------

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseIpRateLimiting(); //

app.UseRouting();

app.UseAuthentication(); //

app.UseSession(); //

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
