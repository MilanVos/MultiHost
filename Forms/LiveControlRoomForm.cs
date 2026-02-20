using MultiHost.Models;
using MultiHost.Services;

namespace MultiHost.Forms;

public partial class LiveControlRoomForm : Form
{
    private readonly EventSessionManager _sessionManager;
    private readonly string _sessionId;
    private readonly ulong _currentHostId;
    private readonly string _currentHostUsername;
    
    private ListView _participantsListView = null!;
    private ListBox _auditLogListBox = null!;
    private Label _eventInfoLabel = null!;
    private Label _hostsOnlineLabel = null!;
    private Button _endEventButton = null!;
    private Button _addHostButton = null!;
    private System.Windows.Forms.Timer _refreshTimer = null!;

    public LiveControlRoomForm(EventSessionManager sessionManager, string sessionId, ulong currentHostId, string currentHostUsername)
    {
        _sessionManager = sessionManager;
        _sessionId = sessionId;
        _currentHostId = currentHostId;
        _currentHostUsername = currentHostUsername;
        
        InitializeComponent();
        SetupEventHandlers();
        LoadSessionData();
    }

    private void InitializeComponent()
    {
        Text = "MultiHost - Live Control Room";
        Size = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterScreen;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(10)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

        var leftPanel = CreateLeftPanel();
        var rightPanel = CreateRightPanel();

        mainLayout.Controls.Add(leftPanel, 0, 0);
        mainLayout.Controls.Add(rightPanel, 1, 0);

        Controls.Add(mainLayout);

        _refreshTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000
        };
        _refreshTimer.Tick += (s, e) => RefreshLocks();
        _refreshTimer.Start();
    }

    private Panel CreateLeftPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };

        _eventInfoLabel = new Label
        {
            Text = "Event: Loading...",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        _hostsOnlineLabel = new Label
        {
            Text = "Hosts online: 0",
            Dock = DockStyle.Top,
            Height = 25
        };

        var participantsGroup = new GroupBox
        {
            Text = "Participants",
            Dock = DockStyle.Fill
        };

        _participantsListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };

        _participantsListView.Columns.Add("Username", 200);
        _participantsListView.Columns.Add("Status", 150);
        _participantsListView.Columns.Add("Actions", 200);

        participantsGroup.Controls.Add(_participantsListView);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(5)
        };

        _endEventButton = new Button
        {
            Text = "End Event",
            Width = 100,
            Height = 35,
            BackColor = Color.IndianRed
        };

        _addHostButton = new Button
        {
            Text = "Manage Hosts",
            Width = 120,
            Height = 35
        };

        buttonPanel.Controls.Add(_endEventButton);
        buttonPanel.Controls.Add(_addHostButton);

        layout.Controls.Add(_eventInfoLabel, 0, 0);
        layout.Controls.Add(_hostsOnlineLabel, 0, 1);
        layout.Controls.Add(participantsGroup, 0, 2);
        layout.Controls.Add(buttonPanel, 0, 3);

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        panel.Controls.Add(layout);
        return panel;
    }

    private Panel CreateRightPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        
        var auditGroup = new GroupBox
        {
            Text = "Action Feed",
            Dock = DockStyle.Fill
        };

        _auditLogListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9)
        };

        auditGroup.Controls.Add(_auditLogListBox);
        panel.Controls.Add(auditGroup);

        return panel;
    }

    private void SetupEventHandlers()
    {
        _sessionManager.SessionUpdated += OnSessionUpdated;
        _sessionManager.AuditEntryAdded += OnAuditEntryAdded;
        _participantsListView.MouseClick += OnParticipantClick;
        _endEventButton.Click += async (s, e) => await EndEvent();
        _addHostButton.Click += (s, e) => ShowManageHostsDialog();

        FormClosing += (s, e) =>
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
        };
    }

    private void LoadSessionData()
    {
        var session = _sessionManager.GetSession(_sessionId);
        if (session == null)
        {
            MessageBox.Show("Session not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
        }

        UpdateUI(session);
    }

    private void OnSessionUpdated(object? sender, EventSession session)
    {
        if (session.SessionId != _sessionId) return;

        if (InvokeRequired)
        {
            Invoke(() => UpdateUI(session));
        }
        else
        {
            UpdateUI(session);
        }
    }

    private void OnAuditEntryAdded(object? sender, AuditEntry entry)
    {
        if (InvokeRequired)
        {
            Invoke(() => AddAuditEntry(entry));
        }
        else
        {
            AddAuditEntry(entry);
        }
    }

    private void UpdateUI(EventSession session)
    {
        _eventInfoLabel.Text = $"Event: {session.GuildName} - {session.VoiceChannelName}";
        _hostsOnlineLabel.Text = $"Hosts online: {string.Join(", ", session.Hosts.Where(h => h.IsOnline).Select(h => $"{h.Username} ({h.Role})"))}";

        _participantsListView.Items.Clear();

        foreach (var participant in session.Participants)
        {
            var item = new ListViewItem(participant.Username);
            item.Tag = participant;

            var status = new List<string>();
            if (participant.IsServerMuted) status.Add("Muted");
            if (participant.IsServerDeafened) status.Add("Deafened");
            if (participant.IsSelfMuted) status.Add("Self-Muted");
            
            var lockInfo = "";
            if (participant.IsLocked())
            {
                var lockingHost = session.Hosts.FirstOrDefault(h => h.UserId == participant.LockedByHostId);
                lockInfo = $" [Locked by {lockingHost?.Username ?? "Unknown"}]";
            }

            item.SubItems.Add(status.Count > 0 ? string.Join(", ", status) + lockInfo : "Normal" + lockInfo);
            item.SubItems.Add("");

            _participantsListView.Items.Add(item);
        }
    }

    private void AddAuditEntry(AuditEntry entry)
    {
        var timestamp = entry.Timestamp.ToString("HH:mm:ss");
        var status = entry.Success ? "✓" : "✗";
        var message = $"[{timestamp}] {status} {entry.HostUsername} → {entry.Action} → {entry.TargetUsername}";
        
        if (!entry.Success && !string.IsNullOrEmpty(entry.ErrorMessage))
        {
            message += $" ({entry.ErrorMessage})";
        }

        _auditLogListBox.Items.Insert(0, message);

        if (_auditLogListBox.Items.Count > 100)
        {
            _auditLogListBox.Items.RemoveAt(100);
        }
    }

    private void OnParticipantClick(object? sender, MouseEventArgs e)
    {
        var item = _participantsListView.GetItemAt(e.X, e.Y);
        if (item?.Tag is not Participant participant) return;

        var session = _sessionManager.GetSession(_sessionId);
        if (session == null) return;

        if (participant.IsLocked() && participant.LockedByHostId != _currentHostId)
        {
            var lockingHost = session.Hosts.FirstOrDefault(h => h.UserId == participant.LockedByHostId);
            MessageBox.Show($"This participant is currently being managed by {lockingHost?.Username ?? "another host"}.", 
                "Locked", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ShowParticipantContextMenu(participant, e.Location);
    }

    private void ShowParticipantContextMenu(Participant participant, Point location)
    {
        var menu = new ContextMenuStrip();

        var muteItem = new ToolStripMenuItem(participant.IsServerMuted ? "Unmute" : "Mute");
        muteItem.Click += async (s, e) => await ToggleMute(participant);
        menu.Items.Add(muteItem);

        var deafenItem = new ToolStripMenuItem(participant.IsServerDeafened ? "Undeafen" : "Deafen");
        deafenItem.Click += async (s, e) => await ToggleDeafen(participant);
        menu.Items.Add(deafenItem);

        menu.Items.Add(new ToolStripSeparator());

        var disconnectItem = new ToolStripMenuItem("Disconnect");
        disconnectItem.Click += async (s, e) => await DisconnectParticipant(participant);
        menu.Items.Add(disconnectItem);

        menu.Show(_participantsListView, location);
    }

    private async Task ToggleMute(Participant participant)
    {
        _sessionManager.TryLockParticipant(_sessionId, participant.UserId, _currentHostId);
        await _sessionManager.MuteParticipantAsync(_sessionId, _currentHostId, _currentHostUsername, 
            participant.UserId, !participant.IsServerMuted);
        await Task.Delay(500);
        _sessionManager.UnlockParticipant(_sessionId, participant.UserId, _currentHostId);
    }

    private async Task ToggleDeafen(Participant participant)
    {
        _sessionManager.TryLockParticipant(_sessionId, participant.UserId, _currentHostId);
        await _sessionManager.DeafenParticipantAsync(_sessionId, _currentHostId, _currentHostUsername, 
            participant.UserId, !participant.IsServerDeafened);
        await Task.Delay(500);
        _sessionManager.UnlockParticipant(_sessionId, participant.UserId, _currentHostId);
    }

    private async Task DisconnectParticipant(Participant participant)
    {
        var result = MessageBox.Show($"Are you sure you want to disconnect {participant.Username}?", 
            "Confirm Disconnect", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _sessionManager.TryLockParticipant(_sessionId, participant.UserId, _currentHostId);
            await _sessionManager.DisconnectParticipantAsync(_sessionId, _currentHostId, _currentHostUsername, participant.UserId);
        }
    }

    private async Task EndEvent()
    {
        var result = MessageBox.Show("Are you sure you want to end this event?", 
            "Confirm End Event", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            await _sessionManager.EndSessionAsync(_sessionId);
            MessageBox.Show("Event ended successfully.", "Event Ended", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
    }

    private void ShowManageHostsDialog()
    {
        MessageBox.Show("Host management interface coming soon!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RefreshLocks()
    {
        var session = _sessionManager.GetSession(_sessionId);
        if (session != null)
        {
            UpdateUI(session);
        }
    }
}
