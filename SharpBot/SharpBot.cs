using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordSharpBot {
    public class SharpBot {
        private readonly DiscordSocketClient botClient; 
        private Dictionary<string, Action<SocketMessage>> botActions;

        public SharpBot() {
            // Register commands
            botActions = new Dictionary<string, Action<SocketMessage>>();
            Economy.Instance.RegisterTo(botActions, botClient);
            Music.Instance.RegisterTo(botActions, botClient);

            // It is recommended to Dispose of a client when you are finished
            // using it, at the end of your app's lifetime.
            botClient = new DiscordSocketClient();
            botClient.Log += LogAsync;
            botClient.Ready += ReadyAsync;
            botClient.MessageReceived += MessageReceivedAsync;
        }

        public async Task MainAsync(string botId) {
            // Tokens should be considered secret data, and never hard-coded.
            await botClient.LoginAsync(TokenType.Bot, botId);
            await botClient.StartAsync();

            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }
        private Task LogAsync(LogMessage log) {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
        private Task ReadyAsync() {
            Console.WriteLine($"{botClient.CurrentUser} is connected!");
            return Task.CompletedTask;
        }
        private async Task MessageReceivedAsync(SocketMessage message) {
            // The bot should never respond to itself.
            if (message.Author.Id == botClient.CurrentUser.Id)
                return;

            var messageAsCommand = message.Content.Split(' ')[0];
            if (messageAsCommand.StartsWith("$") && botActions.ContainsKey(messageAsCommand.Substring(1))) {
                botActions[messageAsCommand.Substring(1)].Invoke(message);
            }
        }
    }
}
