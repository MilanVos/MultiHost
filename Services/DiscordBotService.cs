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
    private TaskCompletionSource<bool>? _readyTaskSource;

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
        _readyTaskSource?.TrySetResult(true);
        return Task.CompletedTask;
    }

    public async Task StartAsync(string token, int timeoutSeconds = 30)
    {
        _readyTaskSource = new TaskCompletionSource<bool>();

        try
        {
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            var readyTask = _readyTaskSource.Task;
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

            var completedTask = await Task.WhenAny(readyTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException($"Bot failed to connect within {timeoutSeconds} seconds. Please check your token and internet connection.");
            }

            await readyTask;
        }
        catch (Exception ex)
        {
            _readyTaskSource = null;
            throw new Exception($"Failed to start bot: {ex.Message}", ex);
        }
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
                BotStatusChanged?.Invoke(this, "Already connected to this voice channel");
                return true;
            }

            if (_audioClient != null)
            {
                BotStatusChanged?.Invoke(this, "Disconnecting from current voice channel...");
                _audioClient.Dispose();
                _audioClient = null;
            }

            var guild = _client.GetGuild(guildId);
            if (guild == null)
            {
                BotStatusChanged?.Invoke(this, $"Error: Guild {guildId} not found");
                return false;
            }

            var voiceChannel = guild.GetVoiceChannel(channelId);
            if (voiceChannel == null)
            {
                BotStatusChanged?.Invoke(this, $"Error: Voice channel {channelId} not found in guild {guild.Name}");
                return false;
            }

            BotStatusChanged?.Invoke(this, $"Connecting to voice channel: {voiceChannel.Name}...");

            var botUser = guild.CurrentUser;
            var permissions = botUser.GetPermissions(voiceChannel);

            if (!permissions.Connect)
            {
                BotStatusChanged?.Invoke(this, $"Error: Bot lacks 'Connect' permission in {voiceChannel.Name}");
                return false;
            }

            if (!permissions.Speak)
            {
                BotStatusChanged?.Invoke(this, $"Warning: Bot lacks 'Speak' permission in {voiceChannel.Name}");
            }

            _audioClient = await voiceChannel.ConnectAsync();
            _currentVoiceChannelId = channelId;

            BotStatusChanged?.Invoke(this, $"✓ Connected to {voiceChannel.Name}");
            return true;
        }
        catch (Discord.Net.HttpException httpEx)
        {
            BotStatusChanged?.Invoke(this, $"HTTP Error joining voice: {httpEx.Reason} (Code: {httpEx.HttpCode})");
            return false;
        }
        catch (TimeoutException)
        {
            BotStatusChanged?.Invoke(this, "Timeout connecting to voice channel. Discord may be slow or unavailable.");
            return false;
        }
        catch (Exception ex)
        {
            BotStatusChanged?.Invoke(this, $"Error joining voice: {ex.GetType().Name} - {ex.Message}");
            Console.WriteLine($"Full exception: {ex}");
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
