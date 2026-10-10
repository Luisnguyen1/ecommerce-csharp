using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Services;
using ECommerce.Data.Json;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Admin", "/", "Admin");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.Name = "nimbus_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole(Roles.Admin));
});

// Persistence seam: JSON file today, EF Core tomorrow — one line swaps it.
var dataFile = builder.Configuration["DataFile"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "..", "..", "database.json");
builder.Services.AddSingleton(_ => new JsonDataStore(Path.GetFullPath(dataFile)));
builder.Services.AddSingleton(typeof(IRepository<>), typeof(JsonRepository<>));

builder.Services.AddSingleton<ICatalogService, CatalogService>();
builder.Services.AddSingleton<ICartService, CartService>();
builder.Services.AddSingleton<IOrderService, OrderService>();
builder.Services.AddSingleton<IPaymentService, MockPaymentService>();
builder.Services.AddSingleton<IReviewService, ReviewService>();
builder.Services.AddSingleton<IWishlistService, WishlistService>();
builder.Services.AddSingleton<ICouponService, CouponService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<IAuthService, AuthService>();

var app = builder.Build();

// Seed the demo database (and regenerate SVG imagery) on first run.
var store = app.Services.GetRequiredService<JsonDataStore>();
await store.EnsureSeededAsync(db => new DemoDataSeeder(app.Environment.WebRootPath ?? app.Environment.ContentRootPath).SeedAsync(db));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
