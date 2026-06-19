using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace Smart_Campus_PUMUB.Components.Features.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public CustomAuthStateProvider(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    // 🔐 Browser Session ထဲကနေ Login အခြေအနေကို ဖတ်ပေးမယ့် Method ပါ
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var userSessionResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
            var userSession = userSessionResult.Success ? userSessionResult.Value : null;

            if (userSession == null)
                return await Task.FromResult(new AuthenticationState(_anonymous));

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, userSession.Username),
                new Claim(ClaimTypes.Role, userSession.Role),
                new Claim("UserId", userSession.UserId.ToString())
            }, "CustomAuth"));

            return await Task.FromResult(new AuthenticationState(claimsPrincipal));
        }
        catch
        {
            return await Task.FromResult(new AuthenticationState(_anonymous));
        }
    }

    // 🔄 Login ဝင်ချိန် သို့မဟုတ် Logout ထွက်ချိန် Session ကို Update လုပ်ပေးမယ့် Method ပါ
    public async Task UpdateAuthenticationState(UserSession? userSession)
    {
        ClaimsPrincipal claimsPrincipal;

        if (userSession != null)
        {
            await _sessionStorage.SetAsync("UserSession", userSession);
            claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, userSession.Username),
                new Claim(ClaimTypes.Role, userSession.Role),
                new Claim("UserId", userSession.UserId.ToString())
            }, "CustomAuth"));
        }
        else
        {
            await _sessionStorage.DeleteAsync("UserSession");
            claimsPrincipal = _anonymous;
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }
    // public async Task MarkUserAsLoggedOut()
    // {
    //     // AuthenticationState ကို Anonymous အဖြစ် ပြောင်းပေးခြင်း
    //     var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
    //     var authState = Task.FromResult(new AuthenticationState(anonymousUser));
    //     NotifyAuthenticationStateChanged(authState);
    // }
    public async Task MarkUserAsLoggedOut()
    {
        // ၁။ Session ကို တကယ် ဖျက်ပစ်ရပါမည်
        await _sessionStorage.DeleteAsync("UserSession");

        // ၂။ Anonymous အခြေအနေကို Notify လုပ်ပါ
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
    }
}

public class UserSession
{
    public string Username { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int UserId { get; set; }
}