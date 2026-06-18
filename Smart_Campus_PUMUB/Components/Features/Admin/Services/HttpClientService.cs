using Newtonsoft.Json;
using System.Text;

namespace Smart_Campus_PUMUB.BlazorServer.Frontend.Services;

public class HttpClientService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpClientService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // ✨ ခေါ်သမျှ API တိုင်းကို Dynamic Model <T> ပြောင်းပေးမယ့် ဗဟို Method
    public async Task<T> ExecuteAsync<T>(string url, EnumHttpMethod method, object? obj = null)
    {
        HttpResponseMessage? responseMessage = null;
        HttpContent? content = null;
        

        if (obj != null)
        {
            // Object ကို Json စာသားပြောင်းခြင်း (မင်းရဲ့ .ToJson() Extension ရှိရင် ၎င်းကိုအစားထိုးသုံးပါ)
            var jsonStr = JsonConvert.SerializeObject(obj);
            content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
        }

        // Program.cs တွင် သတ်မှတ်ထားသော API Client ကို ဆွဲယူခြင်း
        var client = _httpClientFactory.CreateClient("SmartCampusApi");

        switch (method)
        {
            case EnumHttpMethod.Get: responseMessage = await client.GetAsync(url); break;
            case EnumHttpMethod.Post: responseMessage = await client.PostAsync(url, content); break;
            case EnumHttpMethod.Put: responseMessage = await client.PutAsync(url, content); break;
            case EnumHttpMethod.Patch: responseMessage = await client.PatchAsync(url, content); break;
            case EnumHttpMethod.Delete: responseMessage = await client.DeleteAsync(url); break;
            default: throw new Exception("Invalid HTTP Method");
        }

        if (responseMessage.IsSuccessStatusCode)
        {
            var resJson = await responseMessage.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(resJson)!;
        }

        // 💡 တကယ့် validation error message ကို ဖတ်ယူပြီး ပြပေးမည်
        var errorBody = await responseMessage.Content.ReadAsStringAsync();
        throw new Exception($"API Error: {responseMessage.StatusCode} — {errorBody}");
    }
    public async Task<T> ExecuteMultipartAsync<T>(string url, MultipartFormDataContent content)
    {
        var client = _httpClientFactory.CreateClient("SmartCampusApi");
        
        // Multipart/Form-data အတွက် POST ပို့ခြင်း
        var responseMessage = await client.PostAsync(url, content);

        if (responseMessage.IsSuccessStatusCode)
        {
            var resJson = await responseMessage.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(resJson)!;
        }

        // Error ဘာကြောင့်ဖြစ်လဲဆိုတာ ပိုသိရအောင် အသေးစိတ်ထုတ်ပေးခြင်း
        var errorContent = await responseMessage.Content.ReadAsStringAsync();
        throw new Exception($"API Error: {responseMessage.StatusCode} - {errorContent}");
    }
    
}

public enum EnumHttpMethod {None, Get, Post, Put, Patch, Delete }