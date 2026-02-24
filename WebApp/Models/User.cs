namespace WebApp.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public bool IsAdmin { get; set; }
    public ICollection<UserAccess> UserAccesses { get; set; } = new List<UserAccess>();
}
