namespace WebApp.Models;

public class MenuItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public string? ParentGroup { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
}
