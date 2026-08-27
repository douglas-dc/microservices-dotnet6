using AutoMapper;
using GeekShopping.CartAPI.Config;
using GeekShopping.CartAPI.Model.Context;
using GeekShopping.CartAPI.RabbitMQSender;
using GeekShopping.CartAPI.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// MySQL

var connection =
    builder.Configuration["MySQLConnection:MySQLConnectionString"];

builder.Services.AddDbContext<MySQLContext>(
    options => options.UseMySql(
        connection,
        new MySqlServerVersion(
            new Version(8, 3, 0))));

// AutoMapper

IMapper mapper =
    MappingConfig.RegisterMaps().CreateMapper();

builder.Services.AddSingleton(mapper);

builder.Services.AddAutoMapper(
    AppDomain.CurrentDomain.GetAssemblies());

// Repositories

builder.Services.AddScoped<ICartRepository, CartRepository>();

builder.Services.AddScoped<ICouponRepository, CouponRepository>();

// RabbitMQ

builder.Services.AddSingleton<IRabbitMQMessageSender, RabbitMQMessageSender>();

builder.Services.AddControllers();

// CouponAPI

builder.Services.AddHttpClient<ICouponRepository, CouponRepository>(
    client =>
    {
        client.BaseAddress =
            new Uri(
                builder.Configuration["ServiceUrls:CouponAPI"]);
    });

// Authentication

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority =
            builder.Configuration["ServiceUrls:IdentityServer"];

        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateAudience = false
            };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireClaim(
            "scope",
            "geek_shopping");
    });
});

// Swagger

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "GeekShopping.CartAPI",
            Version = "v1"
        });

    c.EnableAnnotations();

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                @"Enter 'Bearer' [space] and your token!",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header
                },
                new List<string>()
            }
        });
});

var app = builder.Build();

// Configure HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MySQLContext>();
    context.Database.Migrate();
}

app.Run();