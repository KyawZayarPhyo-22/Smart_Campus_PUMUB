namespace Smart_Campus_PUMUB.WebApi.Models;

// ──────────────────────────────────────────────────────────────
// RegisterAcc models — shared by Admin list page
// ──────────────────────────────────────────────────────────────

public class RegisterAccListItem
{
    public int RegisterAccId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? FormNo { get; set; }
    public string? ExamSeatNo { get; set; }
    public string? Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? ReviewedDateTime { get; set; }
    public string? ReviewedBy { get; set; }
}

public class RegisterAccActionRequest
{
    public string Status { get; set; } = null!;
    public string? RejectionReason { get; set; }
    public string? ReviewedBy { get; set; }
}

public class RegisterAccActionResponse
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public class RegisterAccPagedResponse
{
    public bool IsSuccess { get; set; }
    public List<RegisterAccListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
