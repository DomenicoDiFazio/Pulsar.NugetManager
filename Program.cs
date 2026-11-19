using Pulsar.NugetManager.Components;
using Pulsar.NugetManager.Services;
using Pulsar.NugetManager.Services.Interfaces;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IDllScannerService, DllScannerService>();

builder.Services.AddScoped<INugetPackageService, NugetPackageService>();

builder.Services.AddScoped<IAzureDevOpsService, AzureDevOpsService>();

builder.Services.AddScoped<ISolutionMigrationService, SolutionMigrationService>();

builder.Services.AddSingleton<ILoggingService, LoggingService>();

builder.Host.UseNLog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
