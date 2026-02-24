namespace WebApp.Models;

public class UserAccess
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AccessRuleId { get; set; }

    public User? User { get; set; }
    public AccessRule? AccessRule { get; set; }
}
