using System;

namespace Smart_Campus_PUMUB.WebApi.Models;

// ========================================================
// ➕ ၁။ FACULTY အသစ်ဆောက်သည့်အခါ သုံးမည့် REQUEST MODEL
// ========================================================
public class FacultyCreateRequestModel 
{ 
    public string? FacultyName { get; set; } 
}

// ========================================================
// 📩 ၂။ FACULTY ဆောက်ပြီးပါက API မှ ပြန်လာမည့် RESPONSE MODEL
// ========================================================
public class FacultyCreateResponseModel 
{ 
    public bool IsSuccess { get; set; } 
    public string? Message { get; set; } 
}

// ========================================================
// 📝 ၃။ FACULTY ပြင်ဆင်သည့်အခါ သုံးမည့် REQUEST MODEL
// ========================================================
public class FacultyUpdateRequestModel 
{ 
    public string? FacultyName { get; set; } 
}

// ========================================================
// 💾 ၄။ FACULTY ပြင်ဆင်ပြီးပါက API မှ ပြန်လာမည့် RESPONSE MODEL
// ========================================================
public class FacultyUpdateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public FacultyModel? Data { get; set; }
}

// ========================================================
// 🗑️ ၅။ FACULTY ဖျက်သိမ်းပြီးပါက API မှ ပြန်လာမည့် RESPONSE MODEL
// ========================================================
public class FacultyDeleteResponseModel 
{ 
    public bool IsSuccess { get; set; } 
    public string? Message { get; set; } 
}

// ========================================================
// 🔍 ၆။ GET METHOD ဖြင့် ပြသရန်နှင့် ၎င်းကို ပတ်သုံးရန် VIEW MODEL
// ========================================================
public class FacultyModel
{
    public int FacultyId { get; set; }
    public string? FacultyName { get; set; }
}