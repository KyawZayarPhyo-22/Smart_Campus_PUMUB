using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.Database.AppDbContext;

namespace Smart_Campus_PUMUB.Components.Features.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJSRuntime _jsRuntime;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NavigationManager _navigationManager;
    private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
    private System.Threading.Timer? _validationTimer;

    private ClaimsPrincipal _currentUser;

    public CustomAuthStateProvider(
        IHttpContextAccessor httpContextAccessor, 
        IJSRuntime jsRuntime,
        IServiceScopeFactory scopeFactory,
        NavigationManager navigationManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _jsRuntime = jsRuntime;
        _scopeFactory = scopeFactory;
        _navigationManager = navigationManager;
        _currentUser = _anonymous;

        // Run validation check every 5 seconds (5000ms)
        _validationTimer = new System.Threading.Timer(ValidateSessionState, null, 5000, 5000);
    }

    private void ValidateSessionState(object? state)
    {
        if (_currentUser == null || !_currentUser.Identity?.IsAuthenticated == true)
        {
            return;
        }

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SmartCampusDbContext>();

                // Check for New Student Accounts first
                var newStudentAccIdClaim = _currentUser.FindFirst("NewStudentAccId")?.Value;
                if (!string.IsNullOrEmpty(newStudentAccIdClaim) && int.TryParse(newStudentAccIdClaim, out int newStudentAccId))
                {
                    var acc = db.NewStudentAccs.FirstOrDefault(x => x.NewStudentAccId == newStudentAccId);
                    if (acc == null || acc.AccountStatus != "Active")
                    {
                        ForceLogoutSession();
                        return;
                    }
                }
                else
                {
                    // Check for standard Users
                    var userIdClaim = _currentUser.FindFirst("UserId")?.Value
                                   ?? _currentUser.FindFirst("User_Id")?.Value
                                   ?? _currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? _currentUser.FindFirst("id")?.Value;

                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                    {
                        var user = db.Users.FirstOrDefault(x => x.UserId == userId);
                        if (user == null || user.IsDelete == true || user.Status != "Active")
                        {
                            ForceLogoutSession();
                            return;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during background session validation: {ex.Message}");
        }
    }

    private async void ForceLogoutSession()
    {
        try
        {
            // Suspend timer
            _validationTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            // Reset current user state and notify views
            _currentUser = _anonymous;
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));

            // Clear browser cookies
            await _jsRuntime.InvokeVoidAsync("authFunctions.logout");

            // Perform page redirection to refresh UI
            _navigationManager.NavigateTo("/login", forceLoad: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to force logout session: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _validationTimer?.Dispose();
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["authToken"];

            if (!string.IsNullOrEmpty(token))
            {
                var claims = ParseClaimsFromJwt(token);
                var identity = new ClaimsIdentity(claims, "Cookies");
                _currentUser = new ClaimsPrincipal(identity);
            }
            else
            {
                _currentUser = _anonymous;
            }

            return Task.FromResult(new AuthenticationState(_currentUser));
        }
        catch
        {
            _currentUser = _anonymous;
            return Task.FromResult(new AuthenticationState(_currentUser));
        }
    }

    public void NotifyUserAuthentication(string token)
    {
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "Cookies");
        var authenticatedUser = new ClaimsPrincipal(identity);
        _currentUser = authenticatedUser;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(authenticatedUser)));
    }

    public void NotifyUserLogout()
    {
        _currentUser = _anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    public async Task NotifyAuthStateChangedAsync()
    {
        var state = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    // 🚪 Clear cookies on logout
    public async Task MarkUserAsLoggedOut()
    {
        _currentUser = _anonymous;
        try
        {
            await _jsRuntime.InvokeVoidAsync("authFunctions.logout");
        }
        catch
        {
            // Fallback for SSR/Prerender when JS is not available
        }
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        try
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs != null)
            {
                foreach (var kvp in keyValuePairs)
                {
                    string type = kvp.Key;
                    if (type.Equals("unique_name", StringComparison.OrdinalIgnoreCase) || 
                        type.Equals("name", StringComparison.OrdinalIgnoreCase) ||
                        type.Equals(ClaimTypes.Name, StringComparison.OrdinalIgnoreCase)) 
                    {
                        type = ClaimTypes.Name;
                    }

                    if (type.Equals("role", StringComparison.OrdinalIgnoreCase) || 
                        type.Equals("roles", StringComparison.OrdinalIgnoreCase) ||
                        type.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)) 
                    {
                        type = ClaimTypes.Role;
                    }

                    if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            claims.Add(new Claim(type, item.ToString()!));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(type, kvp.Value.ToString()!));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing JWT: {ex.Message}");
        }
        return claims;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}