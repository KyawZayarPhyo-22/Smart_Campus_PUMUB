using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Smart_Campus_PUMUB.Components.Features.Services;

namespace Smart_Campus_PUMUB.Components.Features.Services;

/// <summary>
/// Blazor Server Circuit Handler - fires the moment the SignalR circuit (WebSocket) connects.
///
/// WHY THIS IS NEEDED:
/// When a page loads after login, Blazor first renders on the SERVER (SSR) without a JS/SignalR
/// circuit. ProtectedSessionStorage (which relies on JS) cannot be read yet, so
/// GetAuthenticationStateAsync() returns "anonymous" — hiding all permission-based UI
/// (AuthorizeView, sidebar items, CRUD buttons, etc.).
///
/// Once the browser connects via SignalR, this handler fires OnCircuitOpenedAsync(),
/// reads the session storage (now accessible via JS), and broadcasts the real auth state
/// to all AuthorizeView components — making permissions visible WITHOUT a page reload.
///
/// IMPORTANT: We inject AuthenticationStateProvider (the interface) NOT CustomAuthStateProvider
/// directly. This ensures we get the SAME scoped instance that CascadingAuthenticationState uses,
/// so our NotifyAuthenticationStateChanged call actually reaches all AuthorizeView components.
/// </summary>
public class AuthCircuitHandler : CircuitHandler
{
    private readonly CustomAuthStateProvider _authStateProvider;

    public AuthCircuitHandler(AuthenticationStateProvider authStateProvider)
    {
        // Cast to CustomAuthStateProvider to access NotifyAuthStateChangedAsync().
        // This is the SAME instance injected everywhere else in the circuit scope.
        _authStateProvider = (CustomAuthStateProvider)authStateProvider;
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Circuit is now open = JS + SignalR ready = ProtectedSessionStorage is readable.
        // Re-notify all AuthorizeView / CascadingAuthenticationState with real auth state.
        await _authStateProvider.NotifyAuthStateChangedAsync();
    }
}