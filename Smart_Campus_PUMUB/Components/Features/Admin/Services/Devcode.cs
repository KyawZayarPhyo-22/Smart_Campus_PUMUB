using Newtonsoft.Json;

namespace Smart_Campus_PUMUB.BlazorServer.Frontend.Services;

    public static class DevCode
    {
        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static decimal ToDecimal(this object obj)
        { return Convert.ToDecimal(obj); }
    }

    // devcode.tojson()
