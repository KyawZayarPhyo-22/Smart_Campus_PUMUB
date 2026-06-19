// using Microsoft.AspNetCore.Components.Authorization;
// using Microsoft.EntityFrameworkCore;
// using Smart_Campus_PUMUB.BlazorServer.Frontend.Services; // 💡 HttpClientService ရှိမည့် Namespace အား လှမ်းခေါ်ခြင်း
// using Smart_Campus_PUMUB.Components;
// using Smart_Campus_PUMUB.Components.Features.Services;
// using Smart_Campus_PUMUB.Database.AppDbContext;
// using System.Globalization;

// var cultureInfo = new CultureInfo("en-GB"); // en-GB က dd-MM-yyyy ကို သုံးပါတယ်
// CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
// CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowBlazorServer", policy =>
//     {
//         policy.WithOrigins("https://localhost:7017") // 💡 ကိုကို့ရဲ့ Blazor Server Run တဲ့ Port နံပါတ်
//               .AllowAnyMethod()
//               .AllowAnyHeader()
//               .AllowCredentials();
//     });
// });

// // Add services to the container.
// builder.Services.AddRazorComponents()
//     .AddInteractiveServerComponents();

// // ==========================================
// // 🛠️ ဤနေရာတွင် Services များကို စနစ်တကျ ရေးရပါမည် (app = builder.Build() မတိုင်မီ)
// // ==========================================

// // ၁။ Base API URL အား "SmartCampusApi" အမည်ဖြင့် သတ်မှတ်ခြင်း
// builder.Services.AddHttpClient("SmartCampusApi", client =>
// {
//     // မင်းရဲ့ API Server သို့ တိုက်ရိုက်လမ်းကြောင်း (End-slash ပါရပါမည်)
//     client.BaseAddress = new Uri("https://localhost:7297/api/"); 
// });

// // ၂။ ဗဟို API Engine ဖြစ်သော HttpClientService အား Register လုပ်ခြင်း
// builder.Services.AddScoped<HttpClientService>();

// // ==========================================

// builder.Services.AddDbContext<SmartCampusDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddAuthenticationCore();
// builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
// builder.Services.AddCascadingAuthenticationState();
// //builder.Services.AddScoped<Smart_Campus_PUMUB.BlazorServer.Frontend.Services.RegistrationState>();
// builder.Services.AddScoped<Smart_Campus_PUMUB.BlazorServer.Frontend.Services.StudentRegistrationState>();

// var app = builder.Build();
// app.UseCors("AllowBlazorServer");

// // Configure the HTTP request pipeline.
// if (!app.Environment.IsDevelopment())
// {
//     app.UseExceptionHandler("/Error", createScopeForErrors: true);
//     app.UseHsts();
// }

// app.UseHttpsRedirection();
// app.UseStaticFiles();
// app.UseAntiforgery();

// app.MapRazorComponents<App>()
//     .AddInteractiveServerRenderMode();

// app.Run();

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services; // 💡 HttpClientService ရှိမည့် Namespace အား လှမ်းခေါ်ခြင်း
using Smart_Campus_PUMUB.Components;
using Smart_Campus_PUMUB.Components.Admin.Services;
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

// builder.Services.AddAuthenticationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddAuthenticationCore();
// Register the Circuit Handler — fires when SignalR circuit opens, pushes real auth state
// to all AuthorizeView components so permissions show without needing a page reload
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, AuthCircuitHandler>();


// Program.cs ထဲတွင် ဤအတိုင်း အစားထိုးပါ
// builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
//     .AddCookie(options =>
//     {
//         // 💡 Login မဝင်ရသေးရင် သွားရမည့် စာမျက်နှာ
//         options.LoginPath = "/login";

//         // 💡 Login တော့ဝင်ထားတယ်၊ ဒါပေမယ့် Permission မရှိရင် သွားရမည့် စာမျက်နှာ (Access Denied)
//         options.AccessDeniedPath = "/";
//     });

using (var serviceProvider = builder.Services.BuildServiceProvider())
{
    using (var scope = serviceProvider.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<SmartCampusDbContext>();

        try
        {
            // ၁။ Database ထဲမှ Permission နာမည်များအားလုံးကို ဆွဲထုတ်ခြင်း
            var permissions = dbContext.Permissions
                                       .Select(p => p.PermissionName)
                                       .Distinct()
                                       .ToList();

            // ၂။ Authorization ကို Register လုပ်ပြီး Policy များကို Loop ပတ်၍ ထည့်ခြင်း
            builder.Services.AddAuthorizationCore(options =>
            {
                foreach (var permission in permissions)
                {
                    // ဥပမာ - "Create_Student" ဆိုတဲ့ Policy အတွက် "Permission" Claim ထဲမှာ "Create_Student" ပါရမည် ဟု သတ်မှတ်ခြင်း
                    options.AddPolicy(permission, policy =>
                        policy.RequireClaim("Permission", permission));
                }
                // (Option) Default Policy တစ်ခုခုထားချင်ရင် ဒီမှာ ထပ်ထည့်နိုင်ပါတယ်
            });
        }
        catch (Exception ex)
        {
            // ⚠️ အရေးကြီးသည် - Database မရှိသေးချိန် (သို့) Migration အသစ်လုပ်ချိန်တွင် Error မတက်စေရန်
            Console.WriteLine($"Policy Register လုပ်ရာတွင် အမှားဖြစ်နေပါသည်: {ex.Message}");

            // Database Error ဖြစ်နေရင်တောင် App ဆက်ပွင့်အောင် အလွတ်တစ်ခု Register လုပ်ပေးထားရမည်
            builder.Services.AddAuthorizationCore();
        }
    }
}

//builder.Services.AddScoped<Smart_Campus_PUMUB.BlazorServer.Frontend.Services.RegistrationState>();
builder.Services.AddScoped<Smart_Campus_PUMUB.BlazorServer.Frontend.Services.StudentRegistrationState>();

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
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();