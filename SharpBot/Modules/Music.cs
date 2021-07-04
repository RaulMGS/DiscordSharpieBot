using Discord.WebSocket;
using SharpLink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpBot {
    public class Music : SharpBotModule {
        private static Music instance;
        public static Music Instance {
            get {
                if (instance == null) instance = new Music();
                return instance;
            }
        }

        private Dictionary<SocketUser, SocketVoiceChannel> musicUsers;
        private List<LavalinkTrack> musicQueue;
        private LavalinkManager lavalinkManager;
        private ulong lavalinkGuildId;
        private SocketVoiceChannel lavalinkCurrentChannel;

        public override void RegisterTo(Dictionary<string, Action<SocketMessage>> actions, DiscordSocketClient client = null) {

            if (client == null) return;

            musicUsers = new Dictionary<SocketUser, SocketVoiceChannel>();
            musicQueue = new List<LavalinkTrack>();

            lavalinkManager = new LavalinkManager(client, new LavalinkManagerConfig {
                RESTHost = "localhost",
                RESTPort = 2333,
                WebSocketHost = "localhost",
                WebSocketPort = 2333,
                Authorization = "youshallnotpass",
                TotalShards = 1
            });
            lavalinkManager.TrackEnd += OnTrackEnd;
            client.Ready += async () => {
                await lavalinkManager.StartAsync();
            };
            client.UserVoiceStateUpdated += OnVoiceChange;
             
            actions.Add("play", s => PlaySong(s));
            actions.Add("skip", s => SkipSong(s));
        }
        
        // Discord Callbacks
        private async Task OnTrackEnd(LavalinkPlayer player, LavalinkTrack track, string reason) {

            if (musicQueue.Count > 0) {
                var nextTrack = musicQueue[0];
                musicQueue.RemoveAt(0);
                await player.PlayAsync(nextTrack);
            }
            else {
                await lavalinkManager.LeaveAsync(lavalinkGuildId);
            }
        } 
        private Task OnVoiceChange(SocketUser user, SocketVoiceState before, SocketVoiceState after) {
            if (!musicUsers.ContainsKey(user)) musicUsers.Add(user, after.VoiceChannel);
            else musicUsers[user] = after.VoiceChannel;

            if (musicUsers.Any(x => x.Value != lavalinkCurrentChannel) && lavalinkGuildId > 0) {
                lavalinkManager.LeaveAsync(lavalinkGuildId);
            }
            return Task.CompletedTask;
        }

        // Bot Commands
        private async void PlaySong(SocketMessage message) {
            SocketVoiceChannel voiceChannel;

            // Log user
            if (!musicUsers.ContainsKey(message.Author)) musicUsers.Add(message.Author, null);
            voiceChannel = musicUsers[message.Author];
            lavalinkCurrentChannel = voiceChannel;

            if (voiceChannel == null) return;
            var guild = message.Author.MutualGuilds.First().Id;
            lavalinkGuildId = guild;
            // Get LavalinkPlayer for our Guild and if it's null then join a voice channel.
            LavalinkPlayer player = lavalinkManager.GetPlayer(guild) ?? await lavalinkManager.JoinAsync(voiceChannel);

            // Now that we have a player we can go ahead and grab a track and play it
            LoadTracksResponse response = await lavalinkManager.GetTracksAsync($"ytsearch:{message.Content.Substring(message.Content.IndexOf(' '))}");

            // Check if any track was found, bail out otherwise
            if(response.Tracks.Count == 0) { 
                await message.Channel.SendMessageAsync("No track found");
                return;
            }

            // Get the first track that was found and play or add to queue depending on context.
            LavalinkTrack track = response.Tracks.First();
            if (player.Playing) {
                await message.Channel.SendMessageAsync("Playing - " + track.Title);
                musicQueue.Add(track);
            }
            else {
                await message.Channel.SendMessageAsync("Playing - " + track.Title);
                await player.PlayAsync(track);
            }
        } 
        private async void SkipSong(SocketMessage message) {
            var guild = message.Author.MutualGuilds.First().Id;
            LavalinkPlayer player = lavalinkManager.GetPlayer(guild);
            if (player == null) return;

            await player.StopAsync();
            message.Channel.SendMessageAsync("Skipped current song");
        } 
    }
}
