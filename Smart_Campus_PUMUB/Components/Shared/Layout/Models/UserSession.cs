public class UserSession
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
    
    // 💡 ဤစာကြောင်း အတိအကျ ပါရပါမည်
    public List<string> Permissions { get; set; } = new List<string>(); 
    public string Token { get; set; } = string.Empty;
}