using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using dotenv.net;

namespace DiscordVoiceNotifyBot
{
    public class Bot2
    {
        public static async Task Main(string[] args)
        {
            DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[]{"C:/First Bot/Bot/.env"}));
            var bot = new Bot2();
            await bot.RunAsync();
        }

        private readonly DiscordSocketClient _client;
        private readonly ulong _notifyCannelId;
        private readonly string _token;

        public Bot2()
        {
            _token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!;
            _notifyCannelId = ulong.Parse(Environment.GetEnvironmentVariable("NOTIFY_CHANNEL_ID")!);

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All//Guilds | GatewayIntents.GuildVoiceStates | GatewayIntents.GuildMembers
            });

            _client.Log += LogAsync;
            _client.Ready += OnReadyAsync;
            _client.UserVoiceStateUpdated += OnVoiceStateUpdateAsync;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("TOKEN: " + _token);
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private Task OnReadyAsync()
        {
            Console.WriteLine($"✅ Logged in as {_client.CurrentUser}");
            return Task.CompletedTask;
        }

        private async Task OnVoiceStateUpdateAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            Console.WriteLine($"[DEBUG] Voice event: {user.Username}");
            Console.WriteLine($"Before: {(before.VoiceChannel?.Name ?? "なし")} / After: {(after.VoiceChannel?.Name ?? "なし")}");
            var channel = _client.GetChannel(_notifyCannelId) as IMessageChannel;
            Console.WriteLine("GetChannel: " + channel);
            if (channel == null)
                return;

            if (before.VoiceChannel == null && after.VoiceChannel != null)
            {
                int memberCount = after.VoiceChannel.Users.Count;

                if (memberCount == 1)
                {
                    await channel.SendMessageAsync($"{user.Username} が {after.VoiceChannel.Name} で通話を開始");
                }
            }

            else if (before.VoiceChannel != null && after.VoiceChannel == null)
            {
                int remaining = before.VoiceChannel.Users.Count;
                if (remaining == 0)
                {
                    await channel.SendMessageAsync($"{before.VoiceChannel.Name} 通話終了");
                }
            }

            else if (before.VoiceChannel != null && after.VoiceChannel != null &&
                     before.VoiceChannel.Id == after.VoiceChannel.Id)
            {
                return;
            }
            Console.WriteLine("Def");
        }
    }
}