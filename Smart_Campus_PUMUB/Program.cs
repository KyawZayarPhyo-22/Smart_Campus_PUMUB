using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services; // 💡 HttpClientService ရှိမည့် Namespace အား လှမ်းခေါ်ခြင်း
using Smart_Campus_PUMUB.Components;
using Smart_Campus_PUMUB.Components.Features.Services;
using Smart_Campus_PUMUB.Database.AppDbContext;
using System.Globalization;

var cultureInfo = new CultureInfo("en-GB"); // en-GB က dd-MM-yyyy ကို သုံးပါတယ်
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorServer", policy =>
    {
        policy.WithOrigins("https://localhost:7017") // 💡 ကိုကို့ရဲ့ Blazor Server Run တဲ့ Port နံပါတ်
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ==========================================
// 🛠️ ဤနေရာတွင် Services များကို စနစ်တကျ ရေးရပါမည် (app = builder.Build() မတိုင်မီ)
// ==========================================

// ၁။ Base API URL အား "SmartCampusApi" အမည်ဖြင့် သတ်မှတ်ခြင်း
builder.Services.AddHttpClient("SmartCampusApi", client =>
{
    // မင်းရဲ့ API Server သို့ တိုက်ရိုက်လမ်းကြောင်း (End-slash ပါရပါမည်)
    client.BaseAddress = new Uri("https://localhost:7297/api/"); 
});

// ၂။ ဗဟို API Engine ဖြစ်သော HttpClientService အား Register လုပ်ခြင်း
builder.Services.AddScoped<HttpClientService>();

// ==========================================

builder.Services.AddDbContext<SmartCampusDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthenticationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();
app.UseCors("AllowBlazorServer");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();