namespace Smart_Campus_PUMUB.WebApi.Models
{
    public class RuleCreateRequestModel
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Penalty { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class RuleUpdateRequestModel
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Penalty { get; set; }
        public string? ModifiedBy { get; set; }
    }
    public class ActionResponseModel
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    }
    public class RuleModel
    {
        public int RuleId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Penalty { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}