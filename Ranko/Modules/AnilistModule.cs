using System;
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
using System.Collections.Generic;
using System.IO;
using GraphQL;
using GraphQL.Client;
using GraphQL.Common.Request;

namespace Ranko.Modules
{

    [Name("Anilist")]

    public class AnilistModule : ModuleBase<SocketCommandContext>
    {
        enum AnilistType
        {
            MANGA = 0,
            ANIME = 1
        };
        [Command("anime", RunMode = RunMode.Async)]
        [Remarks("search anime by title")]
        [MinPermissions(AccessLevel.User)]
        public async Task anime([Remainder]string title)
        {

            try
            {
                var msg = await Context.Channel.SendMessageAsync(":arrows_counterclockwise: Searching...");
                string tempName = await AniInfo(title, msg, AnilistType.ANIME);
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
            catch (Exception s)
            {
                Console.WriteLine(s.Message);
            }
        }
        [Command("manga", RunMode = RunMode.Async)]
        [Remarks("search manga by title")]
        [MinPermissions(AccessLevel.User)]
        public async Task manga([Remainder]string title)
        {
            try
            {
                var msg = await Context.Channel.SendMessageAsync(":arrows_counterclockwise: Searching...");
                string tempName = await AniInfo(title, msg, AnilistType.MANGA);
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
            catch (Exception s)
            {
                Console.WriteLine(s.Message);
            }
        }
        private InteractiveService _interactive;

        public AnilistModule(InteractiveService inter)
        {
            _interactive = inter;
        }

        public struct animeinfo
        {
            public string romaji;
            public int id;
            public string siteURL;
        }
        private async Task showChoice(IUserMessage msg, string choice)
        {
            await msg.ModifyAsync(
                                x => { x.Content = ":musical_note: Successfully found."; });

            await Context.Channel.SendMessageAsync(choice);
        }
        private async Task<string> AniInfo([Remainder]string text, Discord.Rest.RestUserMessage msg, AnilistType type)
        {
            var client = new GraphQLClient("https://graphql.anilist.co");
            List<animeinfo> ch = new List<animeinfo>();
            var info = new animeinfo();
            string stype = "";
            if(type == AnilistType.ANIME)
            {
                stype = "ANIME";
            }
            else
            {
                stype = "MANGA";
            }
            var request = new GraphQLRequest
            {            
 
                Query = @"
query($id: Int, $page: Int, $perPage: Int, $search: String) {
                    Page(page: $page, perPage: $perPage) {
                        pageInfo {
                          total
                          currentPage
                          lastPage
                          hasNextPage
                          perPage
                        }
                        media(type: "+ stype + @", id: $id, search: $search, isAdult: false) {
                            id
                            title {
                                romaji
                            }
                            siteUrl 
                        }
                    }
                }
",
                Variables = new
                {
                    search = text,
                    page = 1,
                    perPage = 20
                }
                
            };

            var response = await client.PostAsync(request);

            if (response.Data.Page.pageInfo.total.ToObject<int>() > 0)
            {
                if (response.Data.Page.pageInfo.total.ToObject<int>() > response.Data.Page.pageInfo.perPage.ToObject<int>())
                {
                    for (int i = 0; i < 20; i++)
                    {
                        info.id = response.Data.Page.media[i].id;
                        info.romaji = response.Data.Page.media[i].title.romaji;
                        info.siteURL = response.Data.Page.media[i].siteUrl;
                        ch.Add(info);
                    }
                }
                else
                {
                    for (int i = 0; i < response.Data.Page.pageInfo.total.ToObject<int>(); i++)
                    {
                        info.id = response.Data.Page.media[i].id;
                        info.romaji = response.Data.Page.media[i].title.romaji;
                        info.siteURL = response.Data.Page.media[i].siteUrl;
                        ch.Add(info);
                    }
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
                    vids += $"**{count}.** {v.romaji}\n";
                    count++;
                }
                eb.Description = vids;
                var del = await Context.Channel.SendMessageAsync("", false, eb.Build());
                var response2 = await _interactive.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(20));
                await del.DeleteAsync();
                if (response2 == null)
                {
                    await msg.ModifyAsync(x =>
                    {
                        x.Content = $":no_entry_sign: Answer timed out {Context.User.Mention} (≧д≦ヾ)";
                    });
                    return "f2";
                }
                if (!Int32.TryParse(response2.Content, out index))
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
            return ch[index - 1].siteURL;
        }
    }
}


