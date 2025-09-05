using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using JsonBlazer;
using JsonBlazer.Services;
using MudBlazor.Services;
using BlazorMonaco;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add MudBlazor services
builder.Services.AddMudServices();

// Add custom services
builder.Services.AddScoped<ThemeService>();
builder.Services.AddSingleton<JsonHighlighter>();
builder.Services.AddScoped<ClipboardService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<JsonValidationService>();
builder.Services.AddScoped<MonacoEditorService>();
builder.Services.AddScoped<JsonPathService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ApiTestingService>();
builder.Services.AddScoped<JsonDiffService>();
builder.Services.AddScoped<PerformanceService>();
builder.Services.AddScoped<DocumentationService>();
builder.Services.AddScoped<KeyboardShortcutService>();
builder.Services.AddScoped<SearchReplaceService>();
builder.Services.AddScoped<CommandPaletteService>();
builder.Services.AddScoped<VirtualScrollService>();
builder.Services.AddScoped<AccessibilityService>();
builder.Services.AddScoped<AutoCompletionService>();

// Parquet & Delta Lake Services
builder.Services.AddScoped<ParquetService>();
builder.Services.AddScoped<DeltaLakeService>();

await builder.Build().RunAsync();
