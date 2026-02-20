namespace MultiHost.Models;

public class Participant
{
    public ulong UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsServerMuted { get; set; }
    public bool IsServerDeafened { get; set; }
    public bool IsSelfMuted { get; set; }
    public bool IsSelfDeafened { get; set; }
    public ulong? LockedByHostId { get; set; }
    public DateTime? LockExpiry { get; set; }

    public bool IsLocked()
    {
        return LockedByHostId.HasValue && LockExpiry.HasValue && LockExpiry.Value > DateTime.UtcNow;
    }
}
