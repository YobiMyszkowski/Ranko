using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.Addons.InteractiveCommands;
using Discord.WebSocket;
using Ranko.Preconditions;
using Ranko;
using System.Net;

struct ch_info
{
    public string name;
    public string page;
}

namespace Ranko.Modules
{
    [Name("GranblueFantasy")]
    public class GBFModule : ModuleBase<SocketCommandContext>
    {
        [Command("gbfinfo", RunMode = RunMode.Async), Alias("ch")]
        [Remarks("Search gbf character info")]
        [MinPermissions(AccessLevel.User)]
        public async Task ChInfo_([Remainder]string text)
        {
            var msg =
            await Context.Channel.SendMessageAsync(
                ":arrows_counterclockwise: Searching...");

            string tempName = await ChInfo(text, msg);
            if (tempName == "f")
            {
                await msg.ModifyAsync(x =>
                {
                    x.Content = ":no_entry_sign: Failed to get any results!";
                });
                return;
            }
            else if (tempName == "f2")
            {
                return;
            }

            await showChoice(msg, tempName);
        }

        private async Task showChoice(IUserMessage msg, string choice)
        {
            await msg.ModifyAsync(
                                x => { x.Content = ":musical_note: Successfully found."; });

            await Context.Channel.SendMessageAsync(choice);
        }


        private InteractiveService _interactive;

        public GBFModule(InteractiveService inter)
        {
            _interactive = inter;
        }


        private async Task<string> ChInfo([Remainder]string text, Discord.Rest.RestUserMessage msg)
        {
            List<ch_info> ch = new List<ch_info>();
            var info = new ch_info();
            var client = new WebClient();

            string pageSourceCode = client.DownloadString(string.Format("https://gbf.wiki/index.php?title=Special:Search&profile=default&fulltext=Search&search={0}", text));

            System.Text.RegularExpressions.MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(pageSourceCode, "<div class=(.*?)><a href=\"(.*?)\" title=\"(.*?)\" data-serp-pos=\"(.*?)\">");
            if (mc.Count > 0)
            {
                foreach (System.Text.RegularExpressions.Match match in mc)
                {
                    info.name = match.Groups[3].Value;
                    info.page = match.Groups[2].Value;
                    ch.Add(info);
                }
            }
            else
                return "f";

            int index;
            if (ch.Count > 1 && ch != null)
            {
                var eb = new EmbedBuilder()
                {
                    Color = new Color(4, 97, 247),
                    Title = "Enter the index of character u wanna see.",
                };
                string vids = "";
                int count = 1;
                foreach (var v in ch)
                {
                    vids += $"**{count}.** {v.name}\n";
                    count++;
                }
                eb.Description = vids;
                var del = await Context.Channel.SendMessageAsync("", false, eb.Build());
                var response = await _interactive.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(20));
                await del.DeleteAsync();
                if (response == null)
                {
                    await msg.ModifyAsync(x =>
                    {
                        x.Content = $":no_entry_sign: Answer timed out {Context.User.Mention} (≧д≦ヾ)";
                    });
                    return "f2";
                }
                if (!Int32.TryParse(response.Content, out index))
                {
                    await msg.ModifyAsync(x =>
                    {
                        x.Content = $":no_entry_sign: Only add the Index";
                    });
                    return "f2";
                }
                if (index > (ch.Count) || index < 1)
                {
                    await msg.ModifyAsync(x =>
                    {
                        x.Content = $":no_entry_sign: Invalid Number";
                    });
                    return "f2";
                }

            }
            else
            {
                index = 1;
            }
            return $"https://gbf.wiki" + ch[index - 1].page;
        }
    }
}
