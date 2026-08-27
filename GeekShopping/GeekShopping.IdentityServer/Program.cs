using Duende.IdentityServer.Services;
using GeekShopping.IdentityServer.Configuration;
using GeekShopping.IdentityServer.Initializer;
using GeekShopping.IdentityServer.Model;
using GeekShopping.IdentityServer.Model.Context;
using GeekShopping.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers / Views

builder.Services.AddControllersWithViews();

// MySQL

var connection =
    builder.Configuration["MySQLConnection:MySQLConnectionString"];

builder.Services.AddDbContext<MySQLContext>(
    options => options.UseMySql(
        connection,
        new MySqlServerVersion(
            new Version(8, 3, 0)))
);

// ASP.NET Identity

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<MySQLContext>()
    .AddDefaultTokenProviders();

// IdentityServer

var builderServices =
    builder.Services.AddIdentityServer(options =>
    {
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;

        options.EmitStaticAudienceClaim = true;
    })
    .AddInMemoryIdentityResources(
        IdentityConfiguration.IdentityResources)
    .AddInMemoryApiScopes(
        IdentityConfiguration.ApiScopes)
    .AddInMemoryClients(
        IdentityConfiguration.Clients)
    .AddAspNetIdentity<ApplicationUser>();

builder.Services.AddScoped<IDbInitializer, DbInitializer>();

builder.Services.AddScoped<IProfileService, ProfileService>();

builderServices.AddDeveloperSigningCredential();

var app = builder.Build();

var initializer =
    app.Services
        .CreateScope()
        .ServiceProvider
        .GetService<IDbInitializer>();

// HTTP pipeline

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseIdentityServer();

app.UseAuthorization();

// Initialize database

initializer.Initialize();

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();