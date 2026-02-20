namespace MultiHost.Models;

public class EventPolicy
{
    public bool AutoMuteOnJoin { get; set; }
    public bool AutoDeafenOnJoin { get; set; }
    public int ParticipantLockDurationSeconds { get; set; } = 10;
}
