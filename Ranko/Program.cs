using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.InteractiveCommands;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ranko.Services;
using Ranko.Modules;

namespace Ranko
{
    class Program
    {
        static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();
        private DiscordSocketClient _client;
        public async Task StartAsync()
        {
            Configuration.EnsureExists();

            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,              // Specify console verbose information level.
                MessageCacheSize = 1000,                      // Tell discord.net how long to store messages (per channel).   
            });

            _client.Log += (l)                               // Register the console log event.
                => Console.Out.WriteLineAsync(l.ToString());


            var services = ConfigureServices();
            await services.GetRequiredService<CommandHandler>().InitializeAsync(services);

            AudioModule mod = new AudioModule(services.GetService<MusicService>(), services.GetService<InteractiveService>());
            GBFModule mod2 = new GBFModule(services.GetService<InteractiveService>());
            AnilistModule mod3 = new AnilistModule(services.GetService<InteractiveService>());

            await _client.LoginAsync(TokenType.Bot, Configuration.Load().Token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {

            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<EmojiModule>()
                .AddSingleton<WeatherModule>()
                .AddSingleton<GeneralModule>()
                .AddSingleton<AnilistModule>()
                .AddSingleton<OSUModule>()
                .AddSingleton<MusicService>()
                .AddSingleton<YTService>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<GBFModule>()
                .BuildServiceProvider();
        }
    }
}
