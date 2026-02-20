# Voice Channel Setup Guide

## Required Dependencies for Voice Support

Discord.Net requires **native libraries** for voice channel connections. Without these, you'll get errors when trying to join voice channels.

### Windows Setup

#### Option 1: Install via NuGet (Recommended)

Add these packages to your project:

```bash
dotnet add package Discord.Net.WebSocket
dotnet add package Discord.Net.Audio
```

Or manually edit `MultiHost.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Discord.Net" Version="3.16.0" />
  <PackageReference Include="Discord.Net.WebSocket" Version="3.16.0" />
</ItemGroup>
```

#### Option 2: Manual Installation

If voice still doesn't work, you may need to install native libraries manually:

1. **Download libsodium**:
   - Download from: https://github.com/discord-net/Discord.Net/tree/dev/voice-natives
   - Or use: https://github.com/RogueException/Discord.Net/tree/dev/voice-natives/windows
   - Place `libsodium.dll` in the same folder as your executable

2. **Download Opus**:
   - Download from the same repository
   - Place `opus.dll` in the same folder as your executable

3. **Restart the application**

### Linux Setup

```bash
sudo apt-get install libsodium-dev libopus-dev
```

### macOS Setup

```bash
brew install libsodium opus
```

## Common Voice Connection Errors

### Error: "Could not join voice channel"

**Check these:**

1. ✅ **Bot has Connect permission** in the voice channel
   - Go to Discord → Right-click voice channel → Edit Channel → Permissions
   - Find your bot's role → Enable "Connect"

2. ✅ **Bot has Speak permission** (optional but recommended)
   - Same location as above → Enable "Speak"

3. ✅ **Voice channel isn't full**
   - Check user limit on the voice channel
   - Make sure there's space for the bot

4. ✅ **Bot's role hierarchy**
   - Bot's role should be above regular user roles
   - Server Settings → Roles → Drag bot role higher

### Error: "Missing native library"

```
Could not load file or assembly 'libsodium' or one of its dependencies
```

**Solution**: Install native libraries (see above)

### Error: "HTTP 403 Forbidden"

**Cause**: Bot lacks permissions

**Solution**:
- Check bot has "Connect" permission in the channel
- Verify bot's role has sufficient permissions
- Re-invite bot with correct permissions

### Error: "Timeout connecting to voice"

**Causes**:
- Discord voice servers may be down
- Network/firewall blocking UDP ports (typically 50000-65535)
- Rate limiting

**Solutions**:
- Check [Discord Status](https://discordstatus.com)
- Ensure UDP ports aren't blocked by firewall
- Wait a few minutes and try again

## Testing Voice Connection

### Step-by-step test:

1. **Start the application**
2. **Connect bot** with your token
3. **Watch the status label** - it will show detailed error messages:
   - "Connecting to voice channel: [name]..."
   - "✓ Connected to [name]" ← Success!
   - "Error: Bot lacks 'Connect' permission" ← Fix permissions
   - "Error joining voice: [details]" ← Check details

4. If you see **permission errors**:
   ```
   Discord → Server → Voice Channel → Edit → Permissions → Add your bot role
   Enable: Connect, Speak, Mute Members, Deafen Members, Move Members
   ```

5. If you see **library errors**:
   - Install native libraries (libsodium, opus)
   - Restart application

## Discord Bot Permissions Checklist

When inviting your bot, make sure it has these permissions:

### Required for Basic Functionality:
- ✅ View Channels
- ✅ Connect (to voice)
- ✅ Speak (in voice)

### Required for Moderation:
- ✅ Mute Members
- ✅ Deafen Members
- ✅ Move Members

### Optional but Recommended:
- ✅ Manage Channels (to see all channels)
- ✅ Administrator (for full control)

## Invite URL Generator

Use this template for your bot invite URL:

```
https://discord.com/api/oauth2/authorize?client_id=YOUR_BOT_CLIENT_ID&permissions=17564736&scope=bot
```

Replace `YOUR_BOT_CLIENT_ID` with your actual bot's client ID.

**Permissions value breakdown:**
- `17564736` = View Channels + Connect + Speak + Mute Members + Deafen Members + Move Members

## Still Having Issues?

### Enable Detailed Logging

The application now shows detailed status messages in the **Status Label**. Watch for:

```
Status: Connecting to voice channel: General...
Status: Error: Bot lacks 'Connect' permission in General
```

This will tell you exactly what's wrong!

### Check Console Output

Run from terminal to see full exception details:

```bash
dotnet run
```

Look for exceptions like:
- `DllNotFoundException` → Install native libraries
- `UnauthorizedAccessException` → Fix bot permissions
- `TimeoutException` → Network/Discord issues

### Test Bot Permissions Manually

Join Discord, go to the server, and type:

```
!test-voice
```

(If you implement a simple test command to verify bot can see and join voice)

---

**Need help?** Check the main [README.md](./README.md) or open an issue!
