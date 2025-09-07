using CodeVision.UI.Components;
using CodeVision.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Railway/Prod: Kestrel'i dynamic PORT ile dinle
if (builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        options.ListenAnyIP(int.Parse(port));
        Console.WriteLine($"🚀 Railway UI Kestrel - Listening on ANY IP, PORT: {port}");
    });
    builder.Configuration["AllowedHosts"] = "*";
}
else
{
    // Development: tüm hostlara izin ver
    builder.Configuration["AllowedHosts"] = "*";
}

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
