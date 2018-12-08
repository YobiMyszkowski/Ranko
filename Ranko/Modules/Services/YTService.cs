using Discord;
using Discord.Commands;
using Discord.Addons.InteractiveCommands;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeSearch;
using System.Net.Http;
using Newtonsoft.Json;
using Discord.WebSocket;

namespace Ranko.Services
{
    public class YTService
    {
        private string ytApiKey = "";
        public YTService()
        {
            var configDict = Configuration.Load().YTToken;
            if (configDict == null)
            {
                Console.WriteLine("COULDN'T GET YT API KEY FROM CONFIG!");
            }
        }
        public async Task GetYTResults(SocketCommandContext Context, string query)
        {
            try
            {
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = ytApiKey,
                    ApplicationName = "RankoKanzaki"
                });

                var searchListRequest = youtubeService.Search.List("snippet");
                var search = System.Net.WebUtility.UrlEncode(query);
                searchListRequest.Q = search;
                searchListRequest.MaxResults = 10;

                var searchListResponse = await searchListRequest.ExecuteAsync();

                List<string> videos = new List<string>();

                foreach (var searchResult in searchListResponse.Items)
                {
                    switch (searchResult.Id.Kind)
                    {
                        case "youtube#video":
                            videos.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.VideoId));
                            break;
                    }
                }
                await Context.Channel.SendMessageAsync(String.Format("Videos: \n{0}\n", String.Join("\n", videos)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private static async Task<dynamic> GetVideosInPlayListAsync(string playListId)
        {
            var parameters = new Dictionary<string, string>
            {
                ["key"] = Configuration.Load().YTToken,
                ["playlistId"] = playListId,
                ["part"] = "snippet",
                ["maxResults"] = "20"
            };

            var baseUrl = "https://www.googleapis.com/youtube/v3/playlistItems?";
            var fullUrl = MakeUrlWithQuery(baseUrl, parameters);

            var result = await new HttpClient().GetStringAsync(fullUrl);

            if (result != null)
            {
                return JsonConvert.DeserializeObject(result);
            }

            return default(dynamic);
        }

        private static string MakeUrlWithQuery(string baseUrl,
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            if (parameters == null || parameters.Count() == 0)
                return baseUrl;

            return parameters.Aggregate(baseUrl,
                (accumulated, kvp) => string.Format($"{accumulated}{kvp.Key}={kvp.Value}&"));
        }
        public async Task<string> GetYtPlaylist(string id, MusicService serv, SocketCommandContext Context)
        {
            dynamic result = await GetVideosInPlayListAsync(id);

            var count = result.items.Count;

            var _channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (_channel == null)
            {
                await Context.Channel.SendMessageAsync(
                    ":no_entry_sign: You must be connected to Voice Channel!");
                return null;
            }
            var channel = (Context.Guild as SocketGuild).CurrentUser.VoiceChannel as IVoiceChannel;
            if (channel.Id != _channel.Id)
            {
                await Context.Channel.SendMessageAsync(":no_entry_sign: You must be in the same Voice Channel as the me!");
                return null;
            }

            if (count > 0)
                foreach (var item in result.items)
                {
                    await serv.AddQueue(string.Format("https://www.youtube.com/watch?v={0}", item.snippet.resourceId.videoId), Context);
                }

            return null;
        }
        public async Task<string> GetYtURL(SocketCommandContext Context, string name, InteractiveService interactive, Discord.Rest.RestUserMessage msg)
        {
            try
            {
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = ytApiKey,
                    ApplicationName = "RankoKanzaki"
                    
                });

                List<string> videos = new List<string>();

                int i = 1;
                var items = new VideoSearch();
                
                List<string> urls = new List<string>();
                foreach (var item in items.SearchQuery(name, 1))
                {

                    byte[] bytes = System.Text.Encoding.Default.GetBytes(item.Title);
                    string value = System.Text.Encoding.UTF8.GetString(bytes);

                    videos.Add(string.Format("{0})", value));
                    urls.Add(string.Format("{0})", item.Url));

                    i++;
                }
                int index;
                if (videos.Count > 1)
                {
                    var eb = new EmbedBuilder()
                    {
                        Color = new Color(4, 97, 247),
                        Title = "Enter the Index of the YT video you want to add.",
                    };
                    string vids = "";
                    int count = 1;
                    foreach (var v in videos)
                    {
                        vids += $"**{count}.** {v}\n";
                        count++;
                    }
                    eb.Description = vids;
                    var del = await Context.Channel.SendMessageAsync("", false, eb.Build());
                    var response = await interactive.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(20));
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
                    if (index > (videos.Count) || index < 1)
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
                return urls[index - 1];

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return "f";
        }
    }
}
