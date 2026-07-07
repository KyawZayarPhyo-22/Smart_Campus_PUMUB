using Microsoft.AspNetCore.Authorization;

namespace Smart_Campus_PUMUB.WebApi.Filters;

// 💡 [Authorize(Policy="...")] အစား [Permission("...")] ဟု လွယ်ကူစွာသုံးရန်
public class PermissionAttribute : AuthorizeAttribute
{
    public PermissionAttribute(string permissionName)
    {
        // Policy နေရာတွင် ထည့်ပေးလိုက်သော Permission အမည်ကို တိုက်ရိုက်သတ်မှတ်ပေးသည်
        Policy = permissionName;
    }
}
