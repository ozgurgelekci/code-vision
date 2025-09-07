using CodeVision.UI.Components;
using CodeVision.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// AllowedHosts konfigürasyonu - Docker için disable et
builder.Configuration["AllowedHosts"] = "*";

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpClient ve API Service
builder.Services.AddHttpClient<IApiService, ApiService>();
builder.Services.AddScoped<IApiService, ApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // app.UseHsts(); // Docker HTTP için devre dışı
}

// app.UseHttpsRedirection(); // Docker HTTP için devre dışı

app.UseStaticFiles();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
