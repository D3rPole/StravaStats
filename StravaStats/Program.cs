using StravaStats.Components;
using StravaStats.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ActivityService>();
builder.Services.AddHostedService<StravaService>();

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
    private static IServiceProvider _provider;

    public static void Init(IServiceProvider provider) => _provider = provider;

    /// <summary>
    /// Safely resolve a Singleton service anywhere.
    /// </summary>
    public static T GetService<T>() => _provider.GetRequiredService<T>();

    /// <summary>
    /// Safely resolve a Scoped or Transient service by creating a temporary scope.
    /// </summary>
    public static IServiceScope CreateScope() => _provider.CreateScope();
}
