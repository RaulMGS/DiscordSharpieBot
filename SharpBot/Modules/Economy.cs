using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpBot {
    public class Economy : SharpBotModule { 
        private static Economy instance;
        public static Economy Instance {
            get {
                if (instance == null) instance = new Economy();
                return instance;
            }
        }

        private class EconomyUser {
            public ulong uid;
            public int credit;
            public string userName;

            public EconomyUser(ulong uid) {
                this.uid = uid;
                this.credit = 0;
            }
            public void UpdateUserData(string userName) {
                this.userName = userName;
            }
        }

        private Dictionary<ulong, EconomyUser> economyUsers;
        private void LogEconomyUser(SocketUser user) {
            if (!economyUsers.ContainsKey(user.Id)) {
                economyUsers.Add(user.Id, new EconomyUser(user.Id));
                economyUsers[user.Id].UpdateUserData(user.ToString());
            }
            else {
                economyUsers[user.Id].UpdateUserData(user.ToString());
            }
        }

        public override void RegisterTo(Dictionary<string, Action<SocketMessage>> actions, DiscordSocketClient client = null) {
            actions.Add("pay", s => Pay(s));
            actions.Add("bank", s => Bank(s));
            actions.Add("beg", s => Beg(s));
            economyUsers = new Dictionary<ulong, EconomyUser>();
        }

        public async void Bank(SocketMessage message) {
            // Bank of self
            LogEconomyUser(message.Author);
            var user = economyUsers[message.Author.Id];
            var userDisplayName = message.Author;
            // Bank of another one?
            if (message.MentionedUsers.Count > 0) {
                LogEconomyUser(message.MentionedUsers.ElementAt(0));
                user = economyUsers[message.MentionedUsers.ElementAt(0).Id];
                userDisplayName = message.MentionedUsers.ElementAt(0);
            }

            await message.Channel.SendMessageAsync(userDisplayName + " has " + user.credit + " credits");
        }
        public async void Beg(SocketMessage message) {
            LogEconomyUser(message.Author);
            var user = economyUsers[message.Author.Id];

            var random = new Random();
            var begSuccess = random.Next(0, 2) == 1;
            if (begSuccess) {
                var begValue = random.Next(5, 201);
                user.credit += begValue;
                await message.Channel.SendMessageAsync(message.Author + " got " + begValue + " from begging");
            }
            else {
                await message.Channel.SendMessageAsync(message.Author + " didn't beg nicely enough to get any money");
            }
        }
        public async void Pay(SocketMessage message) {
            LogEconomyUser(message.Author);
            var user = economyUsers[message.Author.Id];

            // Try get valid amount
            if (!int.TryParse(message.Content.Split(' ')[1], out int amount)) return;

            // Try get valid party user
            if (message.MentionedUsers.Count == 0) return;
            LogEconomyUser(message.MentionedUsers.ElementAt(0));
            var otherUser = economyUsers[message.MentionedUsers.ElementAt(0).Id];

            // Check if we are paying a correct person
            if (user.uid == otherUser.uid) {
                await message.Channel.SendMessageAsync("You can't pay yourself, dumdum");
                return;
            }

            // Handle transaction if we have enough money
            if (user.credit >= amount) {
                // Switch monye
                user.credit -= amount;
                otherUser.credit += amount;

                await message.Channel.SendMessageAsync(user.userName + " gives " + amount + " to " + otherUser.userName);
            }

            // Display invalid message if we dont have enough money
            else {
                await message.Channel.SendMessageAsync(user.userName + " is too poor to give pomanăăăăăăă");
            }
        }
    }
}
