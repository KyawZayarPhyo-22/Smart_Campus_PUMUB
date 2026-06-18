using System.Collections.Generic;

namespace Smart_Campus_PUMUB.Components.Shared
{
    // 💡 Pagination Data ကို သိမ်းဆည်းရန် PaginatedResponse Generic Class
    // ဂျူနီယာ Developer များပါ နားလည်လွယ်အောင် ရိုးရှင်းစွာ ရေးသားထားပါသည်
    public class PaginatedResponse<T>
    {
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public List<T> Data { get; set; } = new List<T>();
    }
}
