using MudBlazor.Services;
using StravaStats.Components;
using StravaStats.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents(options =>
{
    options.DetailedErrors = true; 
});
builder.Services.AddMudServices();

builder.Services.AddSingleton<ActivityService>();
builder.Services.AddSingleton<AccountService>();
builder.Services.AddSingleton<StravaService>();

builder.Services.AddHostedService(provider => provider.GetRequiredService<AccountService>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

AppData.Init(app.Services);

app.Run();

public static class AppData
{
    public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StravaStats");
    public static string AccountsDirectory => Path.Combine(DataDirectory, "Accounts");

    public const string ActivitiesStravaFileLocation = "Strava";
    public const string ActivitiesValhallaFileLocation = "Valhalla";
    public const string ActivitiesCacheLocation = "Cache";

    private static IServiceProvider _provider;

    public static void Init(IServiceProvider provider)
    {
        if (!Directory.Exists(AppData.DataDirectory))
            Directory.CreateDirectory(AppData.DataDirectory);
        if (!Directory.Exists(AppData.DataDirectory))
            Directory.CreateDirectory(AppData.DataDirectory);

        _provider = provider;
    }

    public static T GetService<T>() => _provider.GetRequiredService<T>();

    public static IServiceScope CreateScope() => _provider.CreateScope();
}
