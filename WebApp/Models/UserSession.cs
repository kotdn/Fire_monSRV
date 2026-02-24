namespace WebApp.Models;

public class UserSession
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public bool IsAdmin { get; set; }
}
