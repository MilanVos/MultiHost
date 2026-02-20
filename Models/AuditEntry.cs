namespace MultiHost.Models;

public class AuditEntry
{
    public DateTime Timestamp { get; set; }
    public ulong HostId { get; set; }
    public string HostUsername { get; set; } = string.Empty;
    public ModerationType Action { get; set; }
    public ulong TargetUserId { get; set; }
    public string TargetUsername { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
