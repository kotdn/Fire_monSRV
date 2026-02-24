namespace WebApp.Models;

public class LogAccess
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public bool Successful { get; set; }
}
