using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot1
{
    public class Bot
    {
        private readonly DiscordSocketClient _client;
        private readonly ulong _notifyChannelId;
        private readonly string _token;
        private readonly HashSet<ulong> _activeCalls = [];

        public Bot()
        {
            _token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!;
            _notifyChannelId = ulong.Parse(Environment.GetEnvironmentVariable("NOTIFY_CHANNEL_ID")!);

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildVoiceStates
            });

            _client.Log += LogAsync;
            _client.Ready += OnReadyAsync;
            _client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
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

        private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            // ボイスチャンネルに参加したとき
            if (before.VoiceChannel == null && after.VoiceChannel != null)
            {
                var channel = after.VoiceChannel;
                
                // 少し待ってキャッシュ更新を待つ
                await Task.Delay(500);
                
                //現在の参加者数を確認
                int userCount = channel.Guild.Users.Count(u => u.VoiceChannel?.Id == channel.Id);
                
                if (!_activeCalls.Contains(channel.Id) && userCount == 1)
                {
                    _activeCalls.Add(channel.Id);
                    Console.WriteLine($"通話開始: {channel.Name}");
                    var textChannel = _client.GetChannel(_notifyChannelId) as IMessageChannel;
                    if (textChannel != null)
                    {
                        await textChannel.SendMessageAsync(
                            $"通話が **{channel.Name}** で開始されました");
                    }
                }
            }

            // ボイスチャンネルから退出
            else if (before.VoiceChannel != null && after.VoiceChannel == null)
            {
                var channel = before.VoiceChannel;
                await Task.Delay(500);
                var updated = channel.Guild.GetVoiceChannel(channel.Id);
                int userCount = channel.Guild.Users.Count(u => u.VoiceChannel?.Id == channel.Id);
                Console.WriteLine($"参加終了: {_activeCalls.Count}");
                if (_activeCalls.Contains(channel.Id) && userCount == 0)
                {
                    _activeCalls.Remove(channel.Id);
                    Console.WriteLine($"通話終了: {channel.Name}");
                    var textChannel = _client.GetChannel(_notifyChannelId) as IMessageChannel;
                    if (textChannel != null)
                    {
                        await textChannel.SendMessageAsync(
                            $"**{channel.Name}** の通話が終了");
                    }
                }
            }

            //チャンネル間の移動
            else if (before.VoiceChannel != null && after.VoiceChannel != null && before.VoiceChannel.Id != after.VoiceChannel.Id)
            {
                Console.WriteLine($"{user.Username} が {before.VoiceChannel.Name} ➤ {after.VoiceChannel.Name} に移動");
                await Task.Delay(500);
                var oldChannel = before.VoiceChannel.Guild.GetVoiceChannel(before.VoiceChannel.Id);
                int userCount = oldChannel.Guild.Users.Count(u => u.VoiceChannel?.Id == oldChannel.Id);
                if (_activeCalls.Contains(before.VoiceChannel.Id) && userCount == 0)
                {
                    _activeCalls.Remove(before.VoiceChannel.Id);
                    var textChannel = _client.GetChannel(_notifyChannelId) as IMessageChannel;
                    if (textChannel != null)
                        await textChannel.SendMessageAsync($" **{oldChannel.Name}** の通話が終了");
                }
                
                // 移動先チャンネルの開始チェック
                var newChannel = after.VoiceChannel.Guild.GetVoiceChannel(after.VoiceChannel.Id);
                userCount = newChannel.Guild.Users.Count(u => u.VoiceChannel?.Id == newChannel.Id);
                if (!_activeCalls.Contains(after.VoiceChannel.Id) && userCount == 1)
                {
                    _activeCalls.Add(after.VoiceChannel.Id);
                    var textChannel = _client.GetChannel(_notifyChannelId) as IMessageChannel;
                    if (textChannel != null)
                        await textChannel.SendMessageAsync($"通話が **{newChannel.Name}** で開始されました");
                }
            }
        }
    }
}