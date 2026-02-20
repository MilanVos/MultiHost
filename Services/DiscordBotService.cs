using Discord;
using Discord.Audio;
using Discord.WebSocket;
using MultiHost.Models;

namespace MultiHost.Services;

public class DiscordBotService
{
    private readonly DiscordSocketClient _client;
    private IAudioClient? _audioClient;
    private ulong? _currentVoiceChannelId;

    public event EventHandler<Participant>? UserJoinedVoice;
    public event EventHandler<ulong>? UserLeftVoice;
    public event EventHandler<Participant>? UserVoiceStateUpdated;
    public event EventHandler<string>? BotStatusChanged;

    public bool IsConnected => _client.ConnectionState == ConnectionState.Connected;
    public bool IsInVoiceChannel => _audioClient != null && _currentVoiceChannelId.HasValue;

    public DiscordBotService()
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates
        };
        _client = new DiscordSocketClient(config);

        _client.Log += Log;
        _client.Ready += OnReady;
        _client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private Task OnReady()
    {
        BotStatusChanged?.Invoke(this, "Bot is ready");
        return Task.CompletedTask;
    }

    public async Task StartAsync(string token)
    {
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_audioClient != null)
        {
            _audioClient.Dispose();
            _audioClient = null;
        }

        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    public async Task<bool> JoinVoiceChannelAsync(ulong guildId, ulong channelId)
    {
        try
        {
            if (_audioClient != null && _currentVoiceChannelId == channelId)
            {
                return true;
            }

            if (_audioClient != null)
            {
                _audioClient.Dispose();
                _audioClient = null;
            }

            var guild = _client.GetGuild(guildId);
            if (guild == null) return false;

            var voiceChannel = guild.GetVoiceChannel(channelId);
            if (voiceChannel == null) return false;

            _audioClient = await voiceChannel.ConnectAsync();
            _currentVoiceChannelId = channelId;

            BotStatusChanged?.Invoke(this, $"Connected to {voiceChannel.Name}");
            return true;
        }
        catch (Exception ex)
        {
            BotStatusChanged?.Invoke(this, $"Error joining voice: {ex.Message}");
            return false;
        }
    }

    public Task LeaveVoiceChannelAsync()
    {
        if (_audioClient != null)
        {
            _audioClient.Dispose();
            _audioClient = null;
            _currentVoiceChannelId = null;
            BotStatusChanged?.Invoke(this, "Disconnected from voice");
        }
        return Task.CompletedTask;
    }

    public async Task<bool> MuteUserAsync(ulong guildId, ulong userId, bool mute)
    {
        try
        {
            var guild = _client.GetGuild(guildId);
            var user = guild?.GetUser(userId);
            if (user == null || user.VoiceChannel == null) return false;

            await user.ModifyAsync(x => x.Mute = mute);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeafenUserAsync(ulong guildId, ulong userId, bool deafen)
    {
        try
        {
            var guild = _client.GetGuild(guildId);
            var user = guild?.GetUser(userId);
            if (user == null || user.VoiceChannel == null) return false;

            await user.ModifyAsync(x => x.Deaf = deafen);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> MoveUserAsync(ulong guildId, ulong userId, ulong targetChannelId)
    {
        try
        {
            var guild = _client.GetGuild(guildId);
            var user = guild?.GetUser(userId);
            if (user == null || user.VoiceChannel == null || guild == null) return false;

            await user.ModifyAsync(x => x.Channel = guild.GetVoiceChannel(targetChannelId));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DisconnectUserAsync(ulong guildId, ulong userId)
    {
        try
        {
            var guild = _client.GetGuild(guildId);
            var user = guild?.GetUser(userId);
            if (user == null || user.VoiceChannel == null) return false;

            await user.ModifyAsync(x => x.Channel = null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public List<SocketGuild> GetGuilds()
    {
        return _client.Guilds.ToList();
    }

    public List<SocketVoiceChannel> GetVoiceChannels(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        return guild?.VoiceChannels.ToList() ?? new List<SocketVoiceChannel>();
    }

    public List<Participant> GetParticipantsInChannel(ulong guildId, ulong channelId)
    {
        var guild = _client.GetGuild(guildId);
        var channel = guild?.GetVoiceChannel(channelId);
        if (channel == null) return new List<Participant>();

        return channel.Users
            .Where(u => !u.IsBot)
            .Select(u => new Participant
            {
                UserId = u.Id,
                Username = u.Username,
                IsServerMuted = u.IsMuted,
                IsServerDeafened = u.IsDeafened,
                IsSelfMuted = u.IsSelfMuted,
                IsSelfDeafened = u.IsSelfDeafened
            })
            .ToList();
    }

    private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (user.IsBot) return;
        if (!_currentVoiceChannelId.HasValue) return;

        if (before.VoiceChannel?.Id != _currentVoiceChannelId && after.VoiceChannel?.Id == _currentVoiceChannelId)
        {
            var participant = new Participant
            {
                UserId = user.Id,
                Username = user.Username,
                IsServerMuted = after.IsMuted,
                IsServerDeafened = after.IsDeafened,
                IsSelfMuted = after.IsSelfMuted,
                IsSelfDeafened = after.IsSelfDeafened
            };
            UserJoinedVoice?.Invoke(this, participant);
        }
        else if (before.VoiceChannel?.Id == _currentVoiceChannelId && after.VoiceChannel?.Id != _currentVoiceChannelId)
        {
            UserLeftVoice?.Invoke(this, user.Id);
        }
        else if (before.VoiceChannel?.Id == _currentVoiceChannelId && after.VoiceChannel?.Id == _currentVoiceChannelId)
        {
            var participant = new Participant
            {
                UserId = user.Id,
                Username = user.Username,
                IsServerMuted = after.IsMuted,
                IsServerDeafened = after.IsDeafened,
                IsSelfMuted = after.IsSelfMuted,
                IsSelfDeafened = after.IsSelfDeafened
            };
            UserVoiceStateUpdated?.Invoke(this, participant);
        }

        await Task.CompletedTask;
    }
}
