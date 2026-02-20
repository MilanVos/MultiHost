# MultiHost - Discord Multi-Host Event Management

A Windows desktop application for managing Discord voice events with multiple hosts. Built with .NET 8.0 and Windows Forms, MultiHost enables collaborative event moderation with real-time synchronization and smart concurrency control.

## Features

### 🎯 Multi-Host Support
- **Multiple simultaneous hosts** can control the same event
- **Three role types**: Owner, Co-Host, and Moderator with granular permissions
- **Soft lock mechanism** prevents conflicts when multiple hosts interact with the same participant (10-second automatic lock)
- **Real-time synchronization** ensures all hosts see the same state

### 🎙️ Voice Channel Moderation
- **Server mute/unmute** participants
- **Server deafen/undeafen** participants
- **Move participants** between channels
- **Disconnect participants** from voice
- **Auto-mute/deafen** new joiners (configurable policy)

### 📊 Real-Time Monitoring
- **Live participant list** with current status (muted, deafened, self-muted)
- **Action feed** showing all moderation actions in real-time
- **Audit log** tracking who did what, when, and whether it succeeded
- **Bot status** indicator showing connection state

### 🔒 Authorization & Security
- **Role-based access control** for all moderation actions
- **Permission validation** before executing Discord API calls
- **Lock status visibility** showing which host is managing each participant
- **Audit trail** for accountability

## Requirements

- **Windows 10/11** (net8.0-windows)
- **.NET 8.0 SDK** or later
- **Discord Bot Token** (see setup instructions below)
- **Visual Studio 2022** (recommended) or .NET CLI

## Discord Bot Setup

### 1. Create a Discord Application
1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Click "New Application" and give it a name
3. Navigate to the "Bot" tab
4. Click "Add Bot"
5. Under "Privileged Gateway Intents", enable:
   - ✅ **Presence Intent**
   - ✅ **Server Members Intent**
   - ✅ **Message Content Intent** (optional)

### 2. Get Your Bot Token
1. In the Bot tab, click "Reset Token"
2. Copy the token (you'll need this for the application)
3. **Keep this token secret!** Never share it or commit it to version control

### 3. Invite Bot to Your Server
1. Go to the "OAuth2" → "URL Generator" tab
2. Select scopes:
   - ✅ `bot`
3. Select bot permissions:
   - ✅ `Mute Members`
   - ✅ `Deafen Members`
   - ✅ `Move Members`
   - ✅ `Connect` (to voice channels)
   - ✅ `Speak` (optional, for audio features)
4. Copy the generated URL and open it in your browser
5. Select your server and authorize the bot

## Usage

### 1. Launch the Application
```bash
dotnet run
```

Or run the executable from:
```
bin\Debug\net8.0-windows\MultiHost.exe
```

### 2. Connect Your Bot
1. In the **Event Lobby**, paste your Discord Bot Token
2. Click **Connect Bot**
3. Wait for the "Bot connected successfully!" message

### 3. Configure Your Event
1. Select your **Discord Server** from the dropdown
2. Select the **Voice Channel** for the event
3. Configure **Event Policy** (optional):
   - ☑️ Auto-mute participants on join
   - ☑️ Auto-deafen participants on join

### 4. Start the Event
1. Click **Start Event**
2. The bot will join the selected voice channel
3. The **Live Control Room** opens automatically

### 5. Moderate Participants
- **Right-click** any participant to open the context menu
- Select **Mute**, **Deafen**, or **Disconnect**
- Actions are executed immediately and logged in the action feed
- If another host is managing a participant, you'll see a lock indicator

### 6. End the Event
- Click **End Event** in the Live Control Room
- Confirm the action
- The bot will leave the voice channel and the session will close

## Architecture

### Project Structure
```
MultiHost/
├── Models/                 # Domain models
│   ├── EventSession.cs     # Central event state
│   ├── Host.cs             # Host user model
│   ├── Participant.cs      # Voice participant model
│   ├── AuditEntry.cs       # Action audit log entry
│   ├── EventPolicy.cs      # Event configuration
│   ├── EventStatus.cs      # Event lifecycle enum
│   ├── HostRole.cs         # Permission role enum
│   └── ModerationType.cs   # Moderation action enum
├── Services/               # Business logic
│   ├── DiscordBotService.cs        # Discord API wrapper
│   └── EventSessionManager.cs      # Session & concurrency management
├── Forms/                  # UI components
│   ├── EventLobbyForm.cs           # Server/channel selection
│   └── LiveControlRoomForm.cs      # Live event controls
└── Program.cs              # Application entry point
```

### Key Design Patterns

#### 1. Event Session as Single Source of Truth
Each event is represented by an `EventSession` object containing:
- Guild and voice channel information
- List of hosts with roles
- List of participants with current state
- Audit log of all actions
- Event policy configuration

#### 2. Soft Lock Concurrency Control
When a host interacts with a participant:
1. The participant is **locked** to that host for 10 seconds
2. Other hosts see the lock status and can't modify that participant
3. The lock **automatically expires** after 10 seconds
4. Prevents "button wars" between multiple hosts

#### 3. Event-Driven Architecture
- `DiscordBotService` fires events for voice state changes
- `EventSessionManager` subscribes and updates the session
- Forms subscribe to `SessionUpdated` and `AuditEntryAdded` events
- All hosts see changes in real-time

#### 4. Role-Based Authorization
```csharp
Owner       → Can start/stop events, manage hosts, all moderation actions
Co-Host     → All moderation actions (mute/deaf/move/disconnect)
Moderator   → Basic moderation (mute/unmute/deaf/disconnect)
```

## Role Permissions Matrix

| Action              | Owner | Co-Host | Moderator |
|---------------------|-------|---------|-----------|
| Start/Stop Event    | ✅    | ❌      | ❌        |
| Add/Remove Hosts    | ✅    | ❌      | ❌        |
| Mute/Unmute         | ✅    | ✅      | ✅        |
| Deafen/Undeafen     | ✅    | ✅      | ✅        |
| Disconnect          | ✅    | ✅      | ✅        |
| Move Participants   | ✅    | ✅      | ❌        |
| Mute All            | ✅    | ❌      | ❌        |

## Configuration

### Event Policy Options
- **Auto-mute on join**: Automatically mutes new participants when they join
- **Auto-deafen on join**: Automatically deafens new participants when they join
- **Lock duration**: Time (in seconds) a participant is locked when being managed (default: 10)

### Bot Configuration
- Configured via `DiscordSocketConfig` in `DiscordBotService`
- Gateway Intents: `Guilds` and `GuildVoiceStates`
- Supports one voice connection per guild at a time

## Troubleshooting

### ⚠️ "Failed to start event. Could not join voice channel"

This is the most common issue! **See [VOICE_SETUP.md](./VOICE_SETUP.md) for detailed solutions.**

**Quick fixes**:
1. **Check permissions**: Bot needs "Connect" permission in the voice channel
2. **Watch the status label**: It shows detailed error messages
3. **Native libraries**: Voice requires libsodium and opus libraries (may need manual install)

### Bot won't connect
- ✅ Verify your bot token is correct
- ✅ Check that the bot has the required privileged intents enabled
- ✅ Ensure your internet connection is stable

### Can't mute/deafen participants
- ✅ Verify the bot has "Mute Members" and "Deafen Members" permissions
- ✅ Check that the bot's role is higher than the target user's highest role
- ✅ Ensure the participant is actually in the voice channel

### Participants not showing up
- ✅ Make sure participants are in the same voice channel as the bot
- ✅ Verify the bot has "View Channels" permission
- ✅ Check the bot has the "Server Members Intent" enabled

### Lock conflicts
- Locks automatically expire after 10 seconds
- If a participant appears permanently locked, refresh by ending and restarting the event

### Native library errors (libsodium, opus)
- See [VOICE_SETUP.md](./VOICE_SETUP.md) for installation instructions
- These are required for voice channel connections
- Error messages will appear in the status label

## Dependencies

- **Discord.Net (v3.16.0)** - Discord API client library
- **.NET 8.0 Windows Forms** - UI framework
- **Microsoft.NETCore.App** - .NET runtime
- **Microsoft.WindowsDesktop.App.WindowsForms** - Windows Desktop runtime

## Future Enhancements

- [ ] Host management UI in Live Control Room
- [ ] "Mute All" / "Unmute All" quick actions
- [ ] Participant search/filter
- [ ] Export audit log to CSV/JSON
- [ ] Custom roles and permission sets
- [ ] Multi-language support
- [ ] Dark theme
- [ ] Host handoff feature
- [ ] Scheduled events
- [ ] Integration with Discord slash commands

## License

This project is provided as-is for educational and personal use.

## Support

For issues, questions, or feature requests, please open an issue on the repository.

---

**Built with ❤️ using .NET 8.0 and Discord.Net**
