namespace MultiHost.Models;

public class EventSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public ulong GuildId { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public ulong VoiceChannelId { get; set; }
    public string VoiceChannelName { get; set; } = string.Empty;
    public EventStatus Status { get; set; }
    public List<Host> Hosts { get; set; } = new();
    public List<Participant> Participants { get; set; } = new();
    public List<AuditEntry> AuditLog { get; set; } = new();
    public EventPolicy Policy { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public Host? GetOwner()
    {
        return Hosts.FirstOrDefault(h => h.Role == HostRole.Owner);
    }

    public bool IsHostAuthorized(ulong userId, ModerationType action)
    {
        var host = Hosts.FirstOrDefault(h => h.UserId == userId);
        if (host == null) return false;

        return action switch
        {
            ModerationType.Mute or ModerationType.Unmute or ModerationType.Deaf or ModerationType.Undeaf => 
                host.Role is HostRole.Owner or HostRole.CoHost or HostRole.Moderator,
            ModerationType.Disconnect => 
                host.Role is HostRole.Owner or HostRole.CoHost or HostRole.Moderator,
            ModerationType.Move => 
                host.Role is HostRole.Owner or HostRole.CoHost,
            _ => false
        };
    }

    public bool CanManageHosts(ulong userId)
    {
        var host = Hosts.FirstOrDefault(h => h.UserId == userId);
        return host?.Role == HostRole.Owner;
    }

    public void AddAuditEntry(ulong hostId, string hostUsername, ModerationType action, ulong targetUserId, string targetUsername, bool success, string? errorMessage = null)
    {
        AuditLog.Add(new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            HostId = hostId,
            HostUsername = hostUsername,
            Action = action,
            TargetUserId = targetUserId,
            TargetUsername = targetUsername,
            Success = success,
            ErrorMessage = errorMessage
        });
    }
}
