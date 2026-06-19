using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Features.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public CustomAuthStateProvider(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    // 🔐 Browser Session ထဲကနေ Login အခြေအနေကို ဖတ်ပေးမယ့် Method ပါ
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Pre-rendering အချိန်မှာ Storage ဖတ်လို့မရရင် Error တက်ပြီး Catch ထဲရောက်သွားပါမယ်
            var userSessionResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
            var userSession = userSessionResult.Success ? userSessionResult.Value : null;

            if (userSession == null)
                return await Task.FromResult(new AuthenticationState(_anonymous));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userSession.Username ?? string.Empty),
                new Claim(ClaimTypes.Role, userSession.Role ?? string.Empty),
                new Claim("UserId", userSession.UserId.ToString())
            };

            // 💡 Null Reference Exception ကို ကာကွယ်ရန် Permissions ကို စစ်ပြီးမှ ထည့်ခြင်း
            if (userSession.Permissions != null && userSession.Permissions.Any())
            {
                foreach (var permission in userSession.Permissions)
                {
                    if (!string.IsNullOrEmpty(permission))
                    {
                        claims.Add(new Claim("Permission", permission));
                    }
                }
            }

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "CustomAuth"));
            return await Task.FromResult(new AuthenticationState(claimsPrincipal));
        }
        catch
        {
            // JS Interop အလုပ်မလုပ်သေးတဲ့ Pre-rendering အချိန်မှာ Anonymous အဖြစ် ယာယီသတ်မှတ်မည်
            return await Task.FromResult(new AuthenticationState(_anonymous));
        }
    }

    // 🔄 Login ဝင်ချိန် သို့မဟုတ် အခြေအနေပြောင်းလဲချိန် Session ကို Update လုပ်ပေးမယ့် Method ပါ
    public async Task UpdateAuthenticationState(UserSession? userSession)
    {
        ClaimsPrincipal claimsPrincipal;

        if (userSession != null)
        {
            await _sessionStorage.SetAsync("UserSession", userSession);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userSession.Username ?? string.Empty),
                new Claim(ClaimTypes.Role, userSession.Role ?? string.Empty),
                new Claim("UserId", userSession.UserId.ToString())
            };

            // 💡 Permissions ပါဝင်လာပါက Claims ထဲသို့ ထည့်သွင်းခြင်း
            if (userSession.Permissions != null && userSession.Permissions.Any())
            {
                foreach (var permission in userSession.Permissions)
                {
                    if (!string.IsNullOrEmpty(permission))
                    {
                        claims.Add(new Claim("Permission", permission));
                    }
                }
            }

            claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "CustomAuth"));
        }
        else
        {
            await _sessionStorage.DeleteAsync("UserSession");
            claimsPrincipal = _anonymous;
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }

    // 🚪 Logout ထွက်သည့်အချိန်တွင် Session ကိုဖျက်ပြီး Anonymous အဖြစ် သတ်မှတ်ပေးမယ့် Method ပါ
    public async Task MarkUserAsLoggedOut()
    {
        // ၁။ Session ကို တကယ် ဖျက်ပစ်ရပါမည်
        await _sessionStorage.DeleteAsync("UserSession");

        // ၂။ Anonymous အခြေအနေကို App တစ်ခုလုံးသိအောင် Notify လုပ်ပါ
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    // 🔄 လက်ရှိ Auth State ကို အတင်းအကျပ် Manual ပြန်လည်စစ်ဆေးခိုင်းရန် (လိုအပ်လျှင် သုံးနိုင်သည်)
    public async Task NotifyAuthStateChangedAsync()
    {
        var state = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }
}

public class UserSession
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int UserId { get; set; }
    public List<string> Permissions { get; set; } = new List<string>();
}