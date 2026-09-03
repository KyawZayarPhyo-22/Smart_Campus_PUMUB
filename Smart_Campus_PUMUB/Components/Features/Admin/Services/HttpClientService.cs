//using Newtonsoft.Json;
//using System.Text;

//namespace Smart_Campus_PUMUB.BlazorServer.Frontend.Services;

//public class HttpClientService
//{
//    private readonly IHttpClientFactory _httpClientFactory;

//    public HttpClientService(IHttpClientFactory httpClientFactory)
//    {
//        _httpClientFactory = httpClientFactory;
//    }

//    // ✨ ခေါ်သမျှ API တိုင်းကို Dynamic Model <T> ပြောင်းပေးမယ့် ဗဟို Method
//    public async Task<T> ExecuteAsync<T>(string url, EnumHttpMethod method, object? obj = null)
//    {
//        HttpResponseMessage? responseMessage = null;
//        HttpContent? content = null;


//        if (obj != null)
//        {
//            // Object ကို Json စာသားပြောင်းခြင်း (မင်းရဲ့ .ToJson() Extension ရှိရင် ၎င်းကိုအစားထိုးသုံးပါ)
//            var jsonStr = JsonConvert.SerializeObject(obj);
//            content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
//        }

//        // Program.cs တွင် သတ်မှတ်ထားသော API Client ကို ဆွဲယူခြင်း
//        var client = _httpClientFactory.CreateClient("SmartCampusApi");

//        switch (method)
//        {
//            case EnumHttpMethod.Get: responseMessage = await client.GetAsync(url); break;
//            case EnumHttpMethod.Post: responseMessage = await client.PostAsync(url, content); break;
//            case EnumHttpMethod.Put: responseMessage = await client.PutAsync(url, content); break;
//            case EnumHttpMethod.Patch: responseMessage = await client.PatchAsync(url, content); break;
//            case EnumHttpMethod.Delete: responseMessage = await client.DeleteAsync(url); break;
//            default: throw new Exception("Invalid HTTP Method");
//        }

//        if (responseMessage.IsSuccessStatusCode)
//        {
//            var resJson = await responseMessage.Content.ReadAsStringAsync();
//            return JsonConvert.DeserializeObject<T>(resJson)!;
//        }

//        throw new Exception($"API Error: {responseMessage.StatusCode}");
//    }
//    public async Task<T> ExecuteMultipartAsync<T>(string url, MultipartFormDataContent content)
//    {
//        var client = _httpClientFactory.CreateClient("SmartCampusApi");

//        // Multipart/Form-data အတွက် POST ပို့ခြင်း
//        var responseMessage = await client.PostAsync(url, content);

//        if (responseMessage.IsSuccessStatusCode)
//        {
//            var resJson = await responseMessage.Content.ReadAsStringAsync();
//            return JsonConvert.DeserializeObject<T>(resJson)!;
//        }

//        // Error ဘာကြောင့်ဖြစ်လဲဆိုတာ ပိုသိရအောင် အသေးစိတ်ထုတ်ပေးခြင်း
//        var errorContent = await responseMessage.Content.ReadAsStringAsync();
//        throw new Exception($"API Error: {responseMessage.StatusCode} - {errorContent}");
//    }

//}

//public enum EnumHttpMethod {None, Get, Post, Put, Patch, Delete }

using Newtonsoft.Json;
using System.Text;
using Smart_Campus_PUMUB.Components.Features.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Smart_Campus_PUMUB.Components.Features.Services;
using Microsoft.JSInterop;

namespace Smart_Campus_PUMUB.BlazorServer.Frontend.Services;

public class HttpClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthenticationStateProvider _authStateProvider;
    private string? _cachedToken;

    public HttpClientService(
        IHttpClientFactory httpClientFactory, 
        IHttpContextAccessor httpContextAccessor, 
        IJSRuntime jsRuntime,
        AuthenticationStateProvider authStateProvider)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _jsRuntime = jsRuntime;
        _authStateProvider = authStateProvider;
    }

    private async Task AttachTokenAsync(HttpClient client)
    {
        try
        {
            string? token = _cachedToken;

            if (token == null && _authStateProvider is CustomAuthStateProvider customAuth && !string.IsNullOrEmpty(customAuth.CurrentToken))
            {
                token = customAuth.CurrentToken;
            }

            if (token == null && _httpContextAccessor.HttpContext != null)
            {
                token = _httpContextAccessor.HttpContext.Request.Cookies["authToken"];
            }

            if (token == null && _jsRuntime != null)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
                    token = await _jsRuntime.InvokeAsync<string>("cookieHelper.get", new object?[] { "authToken" }, cts.Token);
                }
                catch { }
            }

            _cachedToken = token ?? "";

            if (!string.IsNullOrEmpty(_cachedToken))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _cachedToken);
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    // 🔥 MAIN API CALL METHOD (SAFE VERSION)
    public async Task<T?> ExecuteAsync<T>(string url, EnumHttpMethod method, object? obj = null)
    {
        HttpContent? content = null;

        if (obj != null)
        {
            var jsonStr = JsonConvert.SerializeObject(obj);
            content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
        }

        var client = _httpClientFactory.CreateClient("SmartCampusApi");
        await AttachTokenAsync(client);

        HttpResponseMessage responseMessage = method switch
        {
            EnumHttpMethod.Get => await client.GetAsync(url),
            EnumHttpMethod.Post => await client.PostAsync(url, content),
            EnumHttpMethod.Put => await client.PutAsync(url, content),
            EnumHttpMethod.Patch => await client.PatchAsync(url, content),
            EnumHttpMethod.Delete => await client.DeleteAsync(url),
            _ => throw new Exception("Invalid HTTP Method")
        };

        // 🔥 IMPORTANT: always read response body (even 400/500)
        var resJson = await responseMessage.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(resJson))
            return default;

        try
        {
            // 🔥 THIS IS KEY: NEVER THROW FOR HTTP STATUS
            // we always try to read backend message
            return JsonConvert.DeserializeObject<T>(resJson);
        }
        catch
        {
            // if JSON invalid → return null safely
            return default;
        }
    }

    // 🔥 MULTIPART (UPLOAD)
    public async Task<T?> ExecuteMultipartAsync<T>(string url, MultipartFormDataContent content)
    {
        var client = _httpClientFactory.CreateClient("SmartCampusApi");
        await AttachTokenAsync(client);

        var responseMessage = await client.PostAsync(url, content);
        var resJson = await responseMessage.Content.ReadAsStringAsync();

        if (!responseMessage.IsSuccessStatusCode)
        {
            if (!string.IsNullOrWhiteSpace(resJson))
            {
                try
                {
                    var resultObj = JsonConvert.DeserializeObject<T>(resJson);
                    if (resultObj != null)
                    {
                        var msgCheck = typeof(T).GetProperty("Message")?.GetValue(resultObj)?.ToString();
                        if (!string.IsNullOrWhiteSpace(msgCheck)) return resultObj;
                    }
                }
                catch { }

                try
                {
                    var jObj = Newtonsoft.Json.Linq.JObject.Parse(resJson);
                    string? msg = jObj["message"]?.ToString() ?? jObj["Message"]?.ToString() ?? jObj["title"]?.ToString();
                    if (string.IsNullOrWhiteSpace(msg) && jObj["errors"] != null)
                    {
                        msg = jObj["errors"]?.ToString();
                    }

                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        var failObj = System.Activator.CreateInstance<T>();
                        typeof(T).GetProperty("Message")?.SetValue(failObj, msg);
                        typeof(T).GetProperty("IsSuccess")?.SetValue(failObj, false);
                        return failObj;
                    }
                }
                catch { }
            }

            var errObj = System.Activator.CreateInstance<T>();
            typeof(T).GetProperty("Message")?.SetValue(errObj, $"HTTP Error {(int)responseMessage.StatusCode} ({responseMessage.ReasonPhrase}): {(string.IsNullOrWhiteSpace(resJson) ? "No response details" : resJson)}");
            typeof(T).GetProperty("IsSuccess")?.SetValue(errObj, false);
            return errObj;
        }

        if (string.IsNullOrWhiteSpace(resJson))
            return default;

        try
        {
            return JsonConvert.DeserializeObject<T>(resJson);
        }
        catch
        {
            return default;
        }
    }
}

public enum EnumHttpMethod
{
    None,
    Get,
    Post,
    Put,
    Patch,
    Delete
}