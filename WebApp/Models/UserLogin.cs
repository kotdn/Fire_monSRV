namespace WebApp.Models;

public class UserLogin
{
    public int Id { get; set; }
    public required string Ip { get; set; }
    public DateTime DateTime { get; set; }
    public required string Username { get; set; }
    public bool Successful { get; set; }
}
