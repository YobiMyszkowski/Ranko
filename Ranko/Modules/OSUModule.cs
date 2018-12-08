using System;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Ranko.Preconditions;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ranko.Modules
{
    [Name("Osu")]
    public class OSUModule : ModuleBase<SocketCommandContext>
    {
        string osukey = "dd34aef1c6c865512eeacebd4afb510370ff0e25";

        [Command("osu", RunMode = RunMode.Async)]
        [Remarks("search osu user by name")]
        [MinPermissions(AccessLevel.User)]
        public async Task oUser([Remainder]string name)
        {
            await OSUUser(name, GameMode.OSU);
        }
        [Command("osutaiko", RunMode = RunMode.Async)]
        [Remarks("search osu user by name")]
        [MinPermissions(AccessLevel.User)]
        public async Task otUser([Remainder]string name)
        {
            await OSUUser(name, GameMode.TAIKO);
        }
        [Command("osucatch", RunMode = RunMode.Async)]
        [Remarks("search osu user by name")]
        [MinPermissions(AccessLevel.User)]
        public async Task ocUser([Remainder]string name)
        {
            await OSUUser(name, GameMode.CATCHTHEBEAT);
        }
        [Command("osumania", RunMode = RunMode.Async)]
        [Remarks("search osu user by name")]
        [MinPermissions(AccessLevel.User)]
        public async Task omUser([Remainder]string name)
        {
            await OSUUser(name, GameMode.MANIA);
        }
        public async Task OSUUser(string name, GameMode type)
        {
            GameMode mode = type;
            WebClient c = new WebClient();
            byte[] b;

            b = await c.DownloadDataTaskAsync("https://osu.ppy.sh/" + "api/get_user?&k=" + osukey + "&u=" + name + "&m=" + (int)mode);
            if (b != null)
            {

                string result = Encoding.UTF8.GetString(b);
                try
                {
                    List<OsuPlayer> d = JsonConvert.DeserializeObject<List<OsuPlayer>>(result);
                    if (d.Count > 0)
                    {
                        if (d[0].user_id != "0")
                        {
                            d[0].success = true;

                            var icon = "https://osu.ppy.sh/images/flags/" + d[0].country + ".png";
                            var builder = new EmbedBuilder()
                            {
                                Author = new EmbedAuthorBuilder()
                                {
                                    Name = d[0].username + "(" + d[0].user_id + ")",
                                    Url = "https://osu.ppy.sh/u/" + d[0].user_id,
                                    IconUrl = "https://a.ppy.sh/" + d[0].user_id
                                },
                                Footer = new EmbedFooterBuilder()
                                {
                                    Text = $"Requested by {Context.User.Username}#{Context.User.Discriminator}",
                                    IconUrl = (Context.User.GetAvatarUrl())
                                },
                                ThumbnailUrl = icon,
                                Color = Discord.Color.Purple,
                                ImageUrl = "https://a.ppy.sh/" + d[0].user_id,
                            };
                            decimal dec = decimal.Parse(d[0].level, System.Globalization.CultureInfo.InvariantCulture);
                            decimal val = (decimal)Math.Round(dec);
                            builder.AddField(x =>
                            {
                                x.Name = "level";
                                x.Value = val;
                                x.IsInline = true;
                            });
                            val = decimal.Parse(d[0].total_score, System.Globalization.CultureInfo.InvariantCulture);
                            string tscore = val.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
                            builder.AddField(x =>
                            {
                                x.Name = "total score";
                                x.Value = tscore;
                                x.IsInline = true;
                            });
                            dec = decimal.Parse(d[0].accuracy, System.Globalization.CultureInfo.InvariantCulture);
                            val = (decimal)Math.Round(dec, 2);
                            builder.AddField(x =>
                            {
                                x.Name = "accuracy";
                                x.Value = val + "%";
                                x.IsInline = true;
                            });
                            val = decimal.Parse(d[0].playcount, System.Globalization.CultureInfo.InvariantCulture);
                            tscore = val.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
                            builder.AddField(x =>
                            {
                                x.Name = "play counts";
                                x.Value = tscore;
                                x.IsInline = true;
                            });
                            await Context.Channel.SendMessageAsync("", false, builder.Build());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("There is no user with such name");
            }
        }
        public enum GameMode
        {
            OSU, TAIKO, CATCHTHEBEAT, MANIA
        }
        public class OsuPlayer
        {
            public string user_id = "0";
            public string username = "not found";
            public string count300 = "0";
            public string count100 = "0";
            public string count50 = "0";
            public string playcount = "0";
            public string ranked_score = "0";
            public string total_score = "0";
            public string pp_rank = "0";
            public string level = "0";
            public string pp_raw = "0";
            public string accuracy = "0";
            public string count_rank_ss = "0";
            public string count_rank_s = "0";
            public string count_rank_a = "0";
            public string country = "US";
            public string pp_country_rank = "0";
            public List<string> events = new List<string>();

            public bool success;
        }
    }
}
