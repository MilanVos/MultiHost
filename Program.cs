using MultiHost.Forms;
using MultiHost.Services;

namespace MultiHost
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var botService = new DiscordBotService();
            var sessionManager = new EventSessionManager(botService);

            var lobbyForm = new EventLobbyForm(botService, sessionManager);
            Application.Run(lobbyForm);
        }
    }
}