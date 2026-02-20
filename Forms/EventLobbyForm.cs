using Discord.WebSocket;
using MultiHost.Models;
using MultiHost.Services;

namespace MultiHost.Forms;

public partial class EventLobbyForm : Form
{
    private readonly DiscordBotService _botService;
    private readonly EventSessionManager _sessionManager;
    private ComboBox _guildComboBox = null!;
    private ComboBox _channelComboBox = null!;
    private TextBox _botTokenTextBox = null!;
    private Button _connectBotButton = null!;
    private Button _startEventButton = null!;
    private CheckBox _autoMuteCheckBox = null!;
    private CheckBox _autoDeafenCheckBox = null!;
    private Label _statusLabel = null!;
    private GroupBox _settingsGroupBox = null!;

    public EventLobbyForm(DiscordBotService botService, EventSessionManager sessionManager)
    {
        _botService = botService;
        _sessionManager = sessionManager;
        InitializeComponent();
        SetupEventHandlers();
    }

    private void InitializeComponent()
    {
        Text = "MultiHost - Event Lobby";
        Size = new Size(600, 500);
        StartPosition = FormStartPosition.CenterScreen;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(20)
        };

        var botSetupGroup = new GroupBox
        {
            Text = "Bot Setup",
            Height = 120,
            Dock = DockStyle.Top
        };

        var botLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(10)
        };

        var tokenLabel = new Label { Text = "Bot Token:", AutoSize = true };
        _botTokenTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            UseSystemPasswordChar = true
        };

        _connectBotButton = new Button
        {
            Text = "Connect Bot",
            Dock = DockStyle.Fill,
            Height = 30
        };

        botLayout.Controls.Add(tokenLabel, 0, 0);
        botLayout.Controls.Add(_botTokenTextBox, 1, 0);
        botLayout.Controls.Add(_connectBotButton, 1, 1);
        botSetupGroup.Controls.Add(botLayout);

        var selectionGroup = new GroupBox
        {
            Text = "Event Configuration",
            Height = 150,
            Dock = DockStyle.Top
        };

        var selectionLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(10)
        };

        var guildLabel = new Label { Text = "Select Server:", AutoSize = true };
        _guildComboBox = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Enabled = false
        };

        var channelLabel = new Label { Text = "Select Voice Channel:", AutoSize = true };
        _channelComboBox = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Enabled = false
        };

        selectionLayout.Controls.Add(guildLabel, 0, 0);
        selectionLayout.Controls.Add(_guildComboBox, 1, 0);
        selectionLayout.Controls.Add(channelLabel, 0, 1);
        selectionLayout.Controls.Add(_channelComboBox, 1, 1);
        selectionGroup.Controls.Add(selectionLayout);

        _settingsGroupBox = new GroupBox
        {
            Text = "Event Policy",
            Height = 100,
            Dock = DockStyle.Top,
            Enabled = false
        };

        var settingsLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(10)
        };

        _autoMuteCheckBox = new CheckBox { Text = "Auto-mute participants on join" };
        _autoDeafenCheckBox = new CheckBox { Text = "Auto-deafen participants on join" };

        settingsLayout.Controls.Add(_autoMuteCheckBox);
        settingsLayout.Controls.Add(_autoDeafenCheckBox);
        _settingsGroupBox.Controls.Add(settingsLayout);

        _startEventButton = new Button
        {
            Text = "Start Event",
            Height = 40,
            Dock = DockStyle.Top,
            Enabled = false
        };

        _statusLabel = new Label
        {
            Text = "Status: Not connected",
            Dock = DockStyle.Bottom,
            AutoSize = false,
            Height = 30,
            TextAlign = ContentAlignment.MiddleLeft
        };

        mainLayout.Controls.Add(botSetupGroup, 0, 0);
        mainLayout.Controls.Add(selectionGroup, 0, 1);
        mainLayout.Controls.Add(_settingsGroupBox, 0, 2);
        mainLayout.Controls.Add(_startEventButton, 0, 3);
        mainLayout.Controls.Add(_statusLabel, 0, 4);

        Controls.Add(mainLayout);
    }

    private void SetupEventHandlers()
    {
        _connectBotButton.Click += async (s, e) => await ConnectBot();
        _guildComboBox.SelectedIndexChanged += OnGuildSelected;
        _startEventButton.Click += async (s, e) => await StartEvent();
        _botService.BotStatusChanged += (s, status) =>
        {
            if (InvokeRequired)
            {
                Invoke(() => _statusLabel.Text = $"Status: {status}");
            }
            else
            {
                _statusLabel.Text = $"Status: {status}";
            }
        };

        // Skip voice check at startup - will check when actually needed
        _statusLabel.Text = "Status: Ready (voice check deferred)";
    }

    private void CheckVoiceSupport()
    {
        try
        {
            var (success, message) = VoiceDiagnostics.CheckVoiceSupport();
            
            if (!success)
            {
                _statusLabel.Text = "Status: Voice libraries issue (see popup)";
                var result = MessageBox.Show(
                    message + "\n\nDo you want to continue anyway?\n\n" +
                    "Note: Voice channel features may not work, but you can still use other features.",
                    "Voice Support Issue",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                
                if (result == DialogResult.Yes)
                {
                    _statusLabel.Text = "Status: Running without voice support";
                }
            }
            else
            {
                _statusLabel.Text = "Status: Voice support ready";
            }
        }
        catch (BadImageFormatException)
        {
            _statusLabel.Text = "Status: Voice DLLs have wrong architecture";
            MessageBox.Show(
                "Voice libraries were found but have the wrong architecture (32-bit vs 64-bit).\n\n" +
                "This is a known issue with Discord.Net voice on some systems.\n\n" +
                "WORKAROUND:\n" +
                "The application will work, but voice channel joining may fail.\n\n" +
                "You can still:\n" +
                "• Connect the bot to Discord\n" +
                "• View servers and channels\n" +
                "• Test other features\n\n" +
                "For full voice support, you may need to run on a different system or use an alternative approach.",
                "Voice Architecture Mismatch",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Status: Voice check error - {ex.Message}";
        }
    }

    private async Task ConnectBot()
    {
        if (string.IsNullOrWhiteSpace(_botTokenTextBox.Text))
        {
            MessageBox.Show("Please enter a bot token.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _connectBotButton.Enabled = false;
        _statusLabel.Text = "Status: Connecting...";

        try
        {
            await _botService.StartAsync(_botTokenTextBox.Text, timeoutSeconds: 30);

            var guilds = _botService.GetGuilds();
            _guildComboBox.Items.Clear();
            foreach (var guild in guilds)
            {
                _guildComboBox.Items.Add(guild);
            }

            _guildComboBox.DisplayMember = "Name";
            _guildComboBox.Enabled = true;
            _settingsGroupBox.Enabled = true;
            _statusLabel.Text = "Status: Connected";

            MessageBox.Show("Bot connected successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Status: Connection failed - {ex.Message}";
            _connectBotButton.Enabled = true;
            MessageBox.Show($"Failed to connect bot: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnGuildSelected(object? sender, EventArgs e)
    {
        if (_guildComboBox.SelectedItem is not SocketGuild guild)
            return;

        var channels = _botService.GetVoiceChannels(guild.Id);
        _channelComboBox.Items.Clear();

        foreach (var channel in channels)
        {
            _channelComboBox.Items.Add(channel);
        }

        _channelComboBox.DisplayMember = "Name";
        _channelComboBox.Enabled = true;
        _startEventButton.Enabled = channels.Count > 0;
    }

    private async Task StartEvent()
    {
        if (_guildComboBox.SelectedItem is not SocketGuild guild ||
            _channelComboBox.SelectedItem is not SocketVoiceChannel channel)
        {
            MessageBox.Show("Please select a server and voice channel.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _startEventButton.Enabled = false;
        _statusLabel.Text = "Status: Starting event...";

        string? lastBotStatus = null;
        EventHandler<string>? statusHandler = (s, status) =>
        {
            lastBotStatus = status;
            if (InvokeRequired)
            {
                Invoke(() => _statusLabel.Text = $"Status: {status}");
            }
            else
            {
                _statusLabel.Text = $"Status: {status}";
            }
        };

        _botService.BotStatusChanged += statusHandler;

        try
        {
            var currentUser = _botService.GetGuilds().First().CurrentUser;
            var session = _sessionManager.CreateSession(
                guild.Id,
                guild.Name,
                channel.Id,
                channel.Name,
                currentUser.Id,
                currentUser.Username
            );

            session.Policy.AutoMuteOnJoin = _autoMuteCheckBox.Checked;
            session.Policy.AutoDeafenOnJoin = _autoDeafenCheckBox.Checked;

            var started = await _sessionManager.StartSessionAsync(session.SessionId);

            if (started)
            {
                var controlRoomForm = new LiveControlRoomForm(_sessionManager, session.SessionId, currentUser.Id, currentUser.Username);
                controlRoomForm.Show();
                Hide();
            }
            else
            {
                var detailedError = lastBotStatus ?? "Unknown error";
                
                MessageBox.Show(
                    $"Failed to start event. Could not join voice channel.\n\n" +
                    $"Error Details:\n{detailedError}\n\n" +
                    "Common causes:\n" +
                    "• Bot lacks 'Connect' permission in the voice channel\n" +
                    "• Voice channel is full\n" +
                    "• Native libraries (libsodium, opus) not installed\n" +
                    "• Discord voice servers are unavailable\n\n" +
                    "See VOICE_SETUP.md for detailed troubleshooting.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                _startEventButton.Enabled = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting event: {ex.Message}\n\n{ex.GetType().Name}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _startEventButton.Enabled = true;
            _statusLabel.Text = $"Status: Error - {ex.Message}";
        }
        finally
        {
            _botService.BotStatusChanged -= statusHandler;
        }
    }
}
