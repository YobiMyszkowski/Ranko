using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Ranko.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ranko
{
    /// <summary> Detect whether a message is a command, then execute it. </summary>
    public class CommandHandler
    {
        private DiscordSocketClient _client;
        private CommandService _cmds;
        private JsonSerializer jSerializer = new JsonSerializer();
        private IServiceProvider _provider;
        private MusicService _musicService;

        public CommandHandler(IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
        {
            InitializeLoader();
            _client = discord;
            _cmds = commands;
            _provider = provider;
            _musicService = _provider.GetService<MusicService>();
            _client.MessageReceived += HandleCommandAsync;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            await _cmds.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }

        public async Task InstallAsync(DiscordSocketClient c)
        {
            _client = c;                                                 // Save an instance of the discord client.
            _cmds = new CommandService();                                // Create a new instance of the commandservice.   
                                       
            

            await _cmds.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);    // Load all modules from the assembly.
            
            _client.MessageReceived += HandleCommandAsync;               // Register the messagereceived event to handle commands.
        }

        private void InitializeLoader()
        {
            jSerializer.Converters.Add(new JavaScriptDateTimeConverter());
            jSerializer.NullValueHandling = NullValueHandling.Ignore;
        }

        private async Task HandleCommandAsync(SocketMessage rawMessage)
        {
            var message = rawMessage as SocketUserMessage;

            if (!(rawMessage is SocketUserMessage)) return;
            if (rawMessage.Source != MessageSource.User) return;

            var context = new SocketCommandContext(_client, message);

            string prefix;
            prefix = Configuration.Load().Prefix;
            int argPos = prefix.Length - 1;
            if ( !(message.HasStringPrefix(prefix, ref argPos) ||
                  message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
                return;

            var result = await _cmds.ExecuteAsync(context, argPos, _provider);

            if (!result.IsSuccess)
            {
                //await context.Channel.SendMessageAsync(result.ToString());
                //await ratelimitService.checkRatelimit(context.User);
                // await _ratelimitService2.RateLimitMain(context.User);
                // _commandsRan++;
            }
        }
    }
}
