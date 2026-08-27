using GeekShopping.Web.Services;
using GeekShopping.Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// MVC

builder.Services.AddControllersWithViews();

// Authentication

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie(
    "Cookies",
    options =>
    {
        options.ExpireTimeSpan =
            TimeSpan.FromMinutes(10);
    })
.AddOpenIdConnect(
    "oidc",
    options =>
    {
        options.Authority =
            builder.Configuration[
                "ServiceUrls:IdentityServer"];

        options.RequireHttpsMetadata = false;

        options.GetClaimsFromUserInfoEndpoint = true;

        options.ClientId = "geek_shopping";

        options.ClientSecret = "my_super_secret";

        options.ResponseType = "code";

        options.ClaimActions.MapJsonKey(
            "role",
            "role",
            "role");

        options.ClaimActions.MapJsonKey(
            "sub",
            "sub",
            "sub");

        options.TokenValidationParameters.NameClaimType =
            "name";

        options.TokenValidationParameters.RoleClaimType =
            "role";

        options.Scope.Add("geek_shopping");

        options.SaveTokens = true;
    });

// ProductAPI

builder.Services.AddHttpClient<IProductService, ProductService>(
    client =>
    {
        client.BaseAddress =
            new Uri(
                builder.Configuration[
                    "ServiceUrls:ProductAPI"]);
    });

// CartAPI

builder.Services.AddHttpClient<ICartService, CartService>(
    client =>
    {
        client.BaseAddress =
            new Uri(
                builder.Configuration[
                    "ServiceUrls:CartAPI"]);
    });

// CouponAPI

builder.Services.AddHttpClient<ICouponService, CouponService>(
    client =>
    {
        client.BaseAddress =
            new Uri(
                builder.Configuration[
                    "ServiceUrls:CouponAPI"]);
    });

var app = builder.Build();

// HTTP pipeline

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();