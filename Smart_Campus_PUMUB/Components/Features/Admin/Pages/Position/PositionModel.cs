using System;

namespace Smart_Campus_PUMUB.WebApi.Models;

// ========================================================
// ➕ ၁။ POSITION အသစ်ဆောက်သည့်အခါ သုံးမည့် REQUEST MODEL
// ========================================================
public class PositionCreateRequestModel 
{ 
    public string? PositionName { get; set; } 
}

// ========================================================
// 📩 ၂။ POSITION ဆောက်ပြီးပါက API မှ ပြန်လာမည့် RESPONSE MODEL
// ========================================================
public class PositionCreateResponseModel 
{ 
    public bool IsSuccess { get; set; } 
    public string? Message { get; set; } 
}

// ========================================================
// 📝 ၃။ POSITION ပြင်ဆင်သည့်အခါ သုံးမည့် REQUEST MODEL
// ========================================================
public class PositionUpdateRequestModel 
{ 
    public string? PositionName { get; set; } 
}

// ========================================================
// 💾 ၄။ POSITION ပြင်ဆင်ပြီးပါက API မှ ပြန်လာမည့် RESPONSE MODEL
// ========================================================
public class PositionUpdateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public PositionModel? Data { get; set; }
}

// ========================================================
// 🗑️ ၅။ POSITION ဖျက်သိမ်းပြီးပါက API မှ ပြန်လာမည့် RESPONSE MODEL
// ========================================================
public class PositionDeleteResponseModel 
{ 
    public bool IsSuccess { get; set; } 
    public string? Message { get; set; } 
}

// ========================================================
// 🔍 ၆။ GET METHOD ဖြင့် ပြသရန်နှင့် ၎င်းကို ပတ်သုံးရန် VIEW MODEL
// ========================================================
public class PositionModel
{
    public int PositionId { get; set; }
    public string? PositionName { get; set; }
}