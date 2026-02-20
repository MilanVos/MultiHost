using System.Runtime.InteropServices;

namespace MultiHost.Services;

public static class VoiceDiagnostics
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    public static (bool success, string message) CheckVoiceSupport()
    {
        var issues = new List<string>();
        var warnings = new List<string>();
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        var libsodiumPath = Path.Combine(baseDir, "libsodium.dll");
        var opusPath = Path.Combine(baseDir, "opus.dll");

        if (!File.Exists(libsodiumPath))
        {
            issues.Add("❌ libsodium.dll not found - Required for voice encryption");
        }
        else
        {
            try
            {
                var libsodiumHandle = LoadLibrary(libsodiumPath);
                if (libsodiumHandle == IntPtr.Zero)
                {
                    issues.Add("❌ libsodium.dll found but failed to load (wrong architecture?)");
                }
                else
                {
                    warnings.Add("✓ libsodium.dll found and loaded successfully");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"❌ Error loading libsodium: {ex.Message}");
            }
        }

        if (!File.Exists(opusPath))
        {
            issues.Add("❌ opus.dll not found - Required for voice codec");
        }
        else
        {
            try
            {
                var opusHandle = LoadLibrary(opusPath);
                if (opusHandle == IntPtr.Zero)
                {
                    issues.Add("❌ opus.dll found but failed to load (wrong architecture?)");
                }
                else
                {
                    warnings.Add("✓ opus.dll found and loaded successfully");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"❌ Error loading opus: {ex.Message}");
            }
        }

        if (issues.Count > 0)
        {
            var message = "Voice support check failed:\n\n" +
                         string.Join("\n", issues) + "\n\n" +
                         "SOLUTION:\n" +
                         "1. Download Discord.Net voice natives:\n" +
                         "   https://github.com/discord-net/Discord.Net/tree/dev/voice-natives\n\n" +
                         "2. Extract libsodium.dll and opus.dll\n\n" +
                         "3. Place them in the same folder as MultiHost.exe:\n" +
                         $"   {AppDomain.CurrentDomain.BaseDirectory}\n\n" +
                         "4. Restart the application\n\n" +
                         "See VOICE_SETUP.md for detailed instructions.";
            return (false, message);
        }

        return (true, string.Join("\n", warnings));
    }

    public static string GetDiagnosticInfo()
    {
        var info = new List<string>
        {
            $"OS: {RuntimeInformation.OSDescription}",
            $"Architecture: {RuntimeInformation.ProcessArchitecture}",
            $"Framework: {RuntimeInformation.FrameworkDescription}",
            $"Executable Path: {AppDomain.CurrentDomain.BaseDirectory}",
            ""
        };

        var (success, message) = CheckVoiceSupport();
        info.Add(message);

        return string.Join("\n", info);
    }
}
