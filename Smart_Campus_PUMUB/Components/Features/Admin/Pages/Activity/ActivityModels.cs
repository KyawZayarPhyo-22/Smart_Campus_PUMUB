using System;

namespace Smart_Campus_PUMUB.WebApi.Models;

public class ActivityCreateRequestModel
{
    public string ActivityTitle { get; set; } = null!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    // 💡 Frontend တွင် Base64 စာသား သို့မဟုတ် File Stream ဖြင့်င်တွယ်ရန် String အဖြစ် ထားရှိခြင်း
    public string? ImageBase64 { get; set; }
    public string? ImageFileName { get; set; }
}

public class ActivityCreateResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

public class ActivityUpdateRequestModel
{
    public string? ActivityTitle { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Image { get; set; } // API သို့ လှမ်းပို့မည့် လက်ရှိ ပုံအမည် သို့မဟုတ် ပုံအသစ်
    public string? ImageBase64 { get; set; }
    public string? ImageFileName { get; set; }
}

public class ActivityUpdateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public ActivityModel? Data { get; set; }
}

public class ActivityDeleteResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

public class ActivityModel
{
    public int ActivityId { get; set; }
    public string ActivityTitle { get; set; } = null!;
    public string? Image { get; set; } // API မှ ပြန်လာမည့် ပုံ URL သို့မဟုတ် ရုပ်ထွက်လမ်းကြောင်း
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }

    // ဖန်တီးသူ (လိုအပ်ပါက)
    public string? CreatedBy { get; set; }


}