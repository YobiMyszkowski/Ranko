using Discord.Commands;
using Ranko.Preconditions;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Drawing;
using System.IO;
using System;
using System.Net;
using System.Diagnostics;
using Discord;

namespace Ranko
{
    [Name("General")]
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        [Command("botinfo")]
        [Remarks("Post info about bot")]
        [MinPermissions(AccessLevel.User)]
        public async Task botInfo()
        {
            using (var process = Process.GetCurrentProcess())
            {
                var embed = new EmbedBuilder();
                var application = await Context.Client.GetApplicationInfoAsync();
                embed.ImageUrl = application.IconUrl;
                embed.WithColor(new Discord.Color(0x4900ff))
                .AddField(y =>
                {
                    y.Name = "Author:";
                    y.Value = application.Owner.Username; application.Owner.Id.ToString();
                    y.IsInline = false;
                })
                .AddField(y =>
                {
                    y.Name = "Discord.net version:";
                    y.Value = DiscordConfig.Version;
                    y.IsInline = true;
                }).AddField(y =>
                {
                    y.Name = "Servers Amount:";
                    y.Value = (Context.Client as DiscordSocketClient).Guilds.Count.ToString();
                    y.IsInline = false;
                })
                .AddField(y =>
                {
                    y.Name = "Number Of Users:";
                    y.Value = (Context.Client as DiscordSocketClient).Guilds.Sum(g => g.Users.Count).ToString();
                    y.IsInline = false;
                })
                .AddField(y =>
                {
                    y.Name = "Channels:";
                    y.Value = (Context.Client as DiscordSocketClient).Guilds.Sum(g => g.Channels.Count).ToString();
                    y.IsInline = false;
                });
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }
    }
}
