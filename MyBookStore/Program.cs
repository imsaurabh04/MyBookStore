using BulkyWeb.DataAccess.Data;
using BulkyWeb.DataAccess.Repository;
using BulkyWeb.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BulkyWeb.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using Stripe;
using BulkyWeb.DataAccess.DbInitializer;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var keyVaultURL = builder.Configuration.GetSection("keyVault:keyVaultURL");
var keyVaultClientId = builder.Configuration.GetSection("keyVault:ClientId");
var keyVaultClientSecret = builder.Configuration.GetSection("keyVault:ClientSecret");
var keyVaultDirectoryId = builder.Configuration.GetSection("keyVault:DirectoryId");

var credential = new ClientSecretCredential(keyVaultDirectoryId.Value!.ToString(),
    keyVaultClientId.Value!.ToString(), keyVaultClientSecret.Value!.ToString());

builder.Configuration.AddAzureKeyVault(keyVaultURL.Value!.ToString(),
    keyVaultClientId.Value!.ToString(), keyVaultClientSecret.Value.ToString(), new DefaultKeyVaultSecretManager());

var client = new SecretClient(new Uri(keyVaultURL.Value!.ToString()), credential);

if (builder.Environment.IsProduction())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(client.GetSecret("ProdConnection").Value.Value.ToString()));
}

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}


builder.Services.Configure<StripeSettings>(options =>
{
    options.SecretKey = client.GetSecret("STRIPESecretKey").Value.Value.ToString();
    options.PublishableKey = client.GetSecret("STRIPEPublishableKey").Value.Value.ToString();
});

builder.Services.AddIdentity<IdentityUser,IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = $"/Identity/Account/Login";
        options.LogoutPath = $"/Identity/Account/Logout";
        options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
    }
);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication().AddGitHub(option =>
{
    option.ClientId = client.GetSecret("GITHUBClientId").Value.Value.ToString();
    option.ClientSecret = client.GetSecret("GITHUBClientSecret").Value.Value.ToString();
});

builder.Services.AddScoped<IDbInitializer, DbInitializer>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

StripeConfiguration.ApiKey = client.GetSecret("STRIPESecretKey").Value.Value.ToString();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
SeedDatabase();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();



void SeedDatabase ()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        dbInitializer.Initialize();
    }
}
