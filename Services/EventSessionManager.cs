using MultiHost.Models;

namespace MultiHost.Services;

public class EventSessionManager
{
    private readonly Dictionary<string, EventSession> _sessions = new();
    private readonly DiscordBotService _botService;

    public event EventHandler<EventSession>? SessionUpdated;
    public event EventHandler<AuditEntry>? AuditEntryAdded;

    public EventSessionManager(DiscordBotService botService)
    {
        _botService = botService;
        _botService.UserJoinedVoice += OnUserJoinedVoice;
        _botService.UserLeftVoice += OnUserLeftVoice;
        _botService.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
    }

    public EventSession CreateSession(ulong guildId, string guildName, ulong voiceChannelId, string voiceChannelName, ulong ownerId, string ownerUsername)
    {
        var session = new EventSession
        {
            GuildId = guildId,
            GuildName = guildName,
            VoiceChannelId = voiceChannelId,
            VoiceChannelName = voiceChannelName,
            Status = EventStatus.Starting
        };

        session.Hosts.Add(new Host
        {
            UserId = ownerId,
            Username = ownerUsername,
            Role = HostRole.Owner,
            IsOnline = true
        });

        _sessions[session.SessionId] = session;
        return session;
    }

    public async Task<bool> StartSessionAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        var joined = await _botService.JoinVoiceChannelAsync(session.GuildId, session.VoiceChannelId);
        if (!joined) return false;

        session.Status = EventStatus.Live;
        session.StartedAt = DateTime.UtcNow;

        var participants = _botService.GetParticipantsInChannel(session.GuildId, session.VoiceChannelId);
        session.Participants = participants;

        if (session.Policy.AutoMuteOnJoin)
        {
            foreach (var participant in participants)
            {
                await _botService.MuteUserAsync(session.GuildId, participant.UserId, true);
            }
        }

        if (session.Policy.AutoDeafenOnJoin)
        {
            foreach (var participant in participants)
            {
                await _botService.DeafenUserAsync(session.GuildId, participant.UserId, true);
            }
        }

        SessionUpdated?.Invoke(this, session);
        return true;
    }

    public async Task EndSessionAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return;

        session.Status = EventStatus.Ending;
        session.EndedAt = DateTime.UtcNow;

        await _botService.LeaveVoiceChannelAsync();

        SessionUpdated?.Invoke(this, session);
        _sessions.Remove(sessionId);
    }

    public EventSession? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    public bool TryLockParticipant(string sessionId, ulong participantId, ulong hostId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        var participant = session.Participants.FirstOrDefault(p => p.UserId == participantId);
        if (participant == null)
            return false;

        if (participant.IsLocked() && participant.LockedByHostId != hostId)
            return false;

        participant.LockedByHostId = hostId;
        participant.LockExpiry = DateTime.UtcNow.AddSeconds(session.Policy.ParticipantLockDurationSeconds);
        
        SessionUpdated?.Invoke(this, session);
        return true;
    }

    public void UnlockParticipant(string sessionId, ulong participantId, ulong hostId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return;

        var participant = session.Participants.FirstOrDefault(p => p.UserId == participantId);
        if (participant == null || participant.LockedByHostId != hostId)
            return;

        participant.LockedByHostId = null;
        participant.LockExpiry = null;

        SessionUpdated?.Invoke(this, session);
    }

    public async Task<bool> MuteParticipantAsync(string sessionId, ulong hostId, string hostUsername, ulong participantId, bool mute)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        var participant = session.Participants.FirstOrDefault(p => p.UserId == participantId);
        if (participant == null)
        {
            session.AddAuditEntry(hostId, hostUsername, mute ? ModerationType.Mute : ModerationType.Unmute, 
                participantId, "Unknown", false, "Participant not in voice channel");
            return false;
        }

        if (!session.IsHostAuthorized(hostId, mute ? ModerationType.Mute : ModerationType.Unmute))
        {
            session.AddAuditEntry(hostId, hostUsername, mute ? ModerationType.Mute : ModerationType.Unmute, 
                participantId, participant.Username, false, "Unauthorized");
            return false;
        }

        var success = await _botService.MuteUserAsync(session.GuildId, participantId, mute);
        
        if (success)
        {
            participant.IsServerMuted = mute;
        }

        session.AddAuditEntry(hostId, hostUsername, mute ? ModerationType.Mute : ModerationType.Unmute, 
            participantId, participant.Username, success, success ? null : "Discord API error");

        var entry = session.AuditLog.Last();
        AuditEntryAdded?.Invoke(this, entry);
        SessionUpdated?.Invoke(this, session);

        return success;
    }

    public async Task<bool> DeafenParticipantAsync(string sessionId, ulong hostId, string hostUsername, ulong participantId, bool deafen)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        var participant = session.Participants.FirstOrDefault(p => p.UserId == participantId);
        if (participant == null)
        {
            session.AddAuditEntry(hostId, hostUsername, deafen ? ModerationType.Deaf : ModerationType.Undeaf, 
                participantId, "Unknown", false, "Participant not in voice channel");
            return false;
        }

        if (!session.IsHostAuthorized(hostId, deafen ? ModerationType.Deaf : ModerationType.Undeaf))
        {
            session.AddAuditEntry(hostId, hostUsername, deafen ? ModerationType.Deaf : ModerationType.Undeaf, 
                participantId, participant.Username, false, "Unauthorized");
            return false;
        }

        var success = await _botService.DeafenUserAsync(session.GuildId, participantId, deafen);
        
        if (success)
        {
            participant.IsServerDeafened = deafen;
        }

        session.AddAuditEntry(hostId, hostUsername, deafen ? ModerationType.Deaf : ModerationType.Undeaf, 
            participantId, participant.Username, success, success ? null : "Discord API error");

        var entry = session.AuditLog.Last();
        AuditEntryAdded?.Invoke(this, entry);
        SessionUpdated?.Invoke(this, session);

        return success;
    }

    public async Task<bool> DisconnectParticipantAsync(string sessionId, ulong hostId, string hostUsername, ulong participantId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        var participant = session.Participants.FirstOrDefault(p => p.UserId == participantId);
        if (participant == null)
        {
            session.AddAuditEntry(hostId, hostUsername, ModerationType.Disconnect, 
                participantId, "Unknown", false, "Participant not in voice channel");
            return false;
        }

        if (!session.IsHostAuthorized(hostId, ModerationType.Disconnect))
        {
            session.AddAuditEntry(hostId, hostUsername, ModerationType.Disconnect, 
                participantId, participant.Username, false, "Unauthorized");
            return false;
        }

        var success = await _botService.DisconnectUserAsync(session.GuildId, participantId);

        session.AddAuditEntry(hostId, hostUsername, ModerationType.Disconnect, 
            participantId, participant.Username, success, success ? null : "Discord API error");

        var entry = session.AuditLog.Last();
        AuditEntryAdded?.Invoke(this, entry);

        if (success)
        {
            session.Participants.Remove(participant);
        }

        SessionUpdated?.Invoke(this, session);
        return success;
    }

    public bool AddHost(string sessionId, ulong requesterId, ulong newHostId, string newHostUsername, HostRole role)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        if (!session.CanManageHosts(requesterId))
            return false;

        if (session.Hosts.Any(h => h.UserId == newHostId))
            return false;

        session.Hosts.Add(new Host
        {
            UserId = newHostId,
            Username = newHostUsername,
            Role = role,
            IsOnline = true
        });

        SessionUpdated?.Invoke(this, session);
        return true;
    }

    public bool RemoveHost(string sessionId, ulong requesterId, ulong hostIdToRemove)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        if (!session.CanManageHosts(requesterId))
            return false;

        var hostToRemove = session.Hosts.FirstOrDefault(h => h.UserId == hostIdToRemove);
        if (hostToRemove == null || hostToRemove.Role == HostRole.Owner)
            return false;

        session.Hosts.Remove(hostToRemove);
        SessionUpdated?.Invoke(this, session);
        return true;
    }

    private void OnUserJoinedVoice(object? sender, Participant participant)
    {
        var session = _sessions.Values.FirstOrDefault(s => 
            s.Status == EventStatus.Live && 
            _botService.IsInVoiceChannel);

        if (session == null) return;

        session.Participants.Add(participant);
        SessionUpdated?.Invoke(this, session);
    }

    private void OnUserLeftVoice(object? sender, ulong userId)
    {
        var session = _sessions.Values.FirstOrDefault(s => 
            s.Status == EventStatus.Live && 
            _botService.IsInVoiceChannel);

        if (session == null) return;

        var participant = session.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant != null)
        {
            session.Participants.Remove(participant);
            SessionUpdated?.Invoke(this, session);
        }
    }

    private void OnUserVoiceStateUpdated(object? sender, Participant participant)
    {
        var session = _sessions.Values.FirstOrDefault(s => 
            s.Status == EventStatus.Live && 
            _botService.IsInVoiceChannel);

        if (session == null) return;

        var existingParticipant = session.Participants.FirstOrDefault(p => p.UserId == participant.UserId);
        if (existingParticipant != null)
        {
            existingParticipant.IsServerMuted = participant.IsServerMuted;
            existingParticipant.IsServerDeafened = participant.IsServerDeafened;
            existingParticipant.IsSelfMuted = participant.IsSelfMuted;
            existingParticipant.IsSelfDeafened = participant.IsSelfDeafened;
            SessionUpdated?.Invoke(this, session);
        }
    }
}
