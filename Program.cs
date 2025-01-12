using av_motion_api.Data;
using av_motion_api.Factory;
using av_motion_api.Models;
using av_motion_api.Interfaces;
using av_motion_api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration; // Ensure this is here
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using Microsoft.Extensions.FileProviders;


var builder = WebApplication.CreateBuilder(args);

// Set the license context for EPPlus
ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // or LicenseContext.Commercial if you have a commercial license

// Configure the app environment
var configuration = builder.Configuration;

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    //.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false);
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true); // Add reloadOnChange

builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
{
    //config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true); // Add reloadOnChange
    config.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// CORS
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
}

// Add services to the container
builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

// SQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
     sqlServerOptions => sqlServerOptions.CommandTimeout(420)) // Set timeout to 300 seconds (5 minutes)
    );
builder.Services.AddScoped<IRepository, Repository>();

builder.Services.AddIdentity<User, Role>(options =>
                {
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireDigit = true;
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<Role>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Tokens:Issuer"],
                        ValidAudience = builder.Configuration["Tokens:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Tokens:Key"]))
                    };
                });

// Configure FormOptions for file uploads
builder.Services.Configure<FormOptions>(o =>
{
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartBodyLengthLimit = int.MaxValue;
    o.MemoryBufferThreshold = int.MaxValue;
});

builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, AppUserClaimsPrincipalFactory>();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(3));



// Register the OverdueSettings configuration section
builder.Services.Configure<QualifyingMembersService>(builder.Configuration.GetSection("QualifyingMembersService"));
// Register the AddHostedService hosted service
builder.Services.AddScoped<QualifyingMembersService>();

// Register the OverdueSettings configuration section
builder.Services.Configure<OverdueSettings>(builder.Configuration.GetSection("OverdueSettings"));

// Register the OrderStatusUpdater hosted service
builder.Services.AddHostedService<OrderStatusUpdater>();

// Register the DeletionSettings configuration section
builder.Services.Configure<DeletionSettings>(configuration.GetSection("DeletionSettings"));

// Register the UserDeletionService hosted service
builder.Services.AddHostedService<UserDeletionService>();


//Register the ContractExpirationService hosted service
builder.Services.AddHostedService<ContractExpirationService>();
builder.Services.Configure<ContractDeletionSettings>(configuration.GetSection("ContractDeletionSettings"));


builder.Services.AddHostedService<BillingService>();



builder.Services.Configure<VonageOptions>(builder.Configuration.GetSection("Vonage"));
builder.Services.AddScoped<SmsService>();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}

// Use CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Use(async (context, next) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Handling request: " + context.Request.Path);
    await next.Invoke();
    logger.LogInformation("Finished handling request.");
});

app.UseStaticFiles(); // Enables serving static files

// Set up a directory for static files, if it's not in the default wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(@"C:\Users\andas\source\repos\inf370systems-team43\WeeklyNewsImages"),
    RequestPath = "/WeeklyNewsImages"
});


app.Run();
