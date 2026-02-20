namespace MultiHost.Models;

public class Host
{
    public ulong UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public HostRole Role { get; set; }
    public bool IsOnline { get; set; }
}
