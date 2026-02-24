namespace WebApp.Models;

public class AccessRule
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public ICollection<UserAccess> UserAccesses { get; set; } = new List<UserAccess>();
}
