using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpBot {
    public abstract class SharpBotModule { 
        public abstract void RegisterTo(Dictionary<string, Action<SocketMessage>> actions, DiscordSocketClient client = null);
    }
}
