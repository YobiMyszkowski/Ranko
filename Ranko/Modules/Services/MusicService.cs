﻿using System;
using System.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Audio;
using Discord.WebSocket;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Discord.Addons.InteractiveCommands;


namespace Ranko.Services
{
    public class MusicService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> audioDict =
            new ConcurrentDictionary<ulong, IAudioClient>();
        private readonly ConcurrentDictionary<IAudioClient, audioStream_Token> audioStreamDict =
            new ConcurrentDictionary<IAudioClient, audioStream_Token>();
        private ConcurrentDictionary<ulong, List<SongStruct>> queueDict = new ConcurrentDictionary<ulong, List<SongStruct>>();
        public YTService _ytService;
        private JsonSerializer _jSerializer = new JsonSerializer();
        public ConcurrentDictionary<ulong, List<SongStruct>> GetQueeList()
        {
            return queueDict;
        }

        public struct SongStruct
        {
            public string name;
            public string user;
        }
        public MusicService(YTService ser)
        {
            InitializeLoader();
            LoadDatabase();
            _ytService = ser;
        }

        public async Task JoinChannel(IVoiceChannel channel, ulong guildID)
        {
            var audioClient = await channel.ConnectAsync();
            audioDict.TryAdd(guildID, audioClient);
        }

        public async Task LeaveChannel(SocketCommandContext Context, IVoiceChannel _channel)
        {
            try
            {
                IAudioClient aClient;
                audioDict.TryGetValue(Context.Guild.Id, out aClient);
                if (aClient == null)
                {
                    await Context.Channel.SendMessageAsync(":no_entry_sign: Bot is not connected to any Voice Channels");
                    return;
                }
                var channel = (Context.Guild as SocketGuild).CurrentUser.VoiceChannel as IVoiceChannel;
                if (channel.Id == _channel.Id)
                {
                    await aClient.StopAsync();
                    aClient.Dispose();
                    audioDict.TryRemove(Context.Guild.Id, out aClient);
                }
                else
                {
                    await Context.Channel.SendMessageAsync(":no_entry_sign: You must be in the same channel as the me!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task CheckIfAlone(SocketUser user, SocketVoiceState stateOld, SocketVoiceState stateNew)
        {
            try
            {
                if (user.IsBot)
                    return;
                if (stateOld.VoiceChannel == null)
                    return;
                if (stateOld.VoiceChannel == (stateNew.VoiceChannel ?? null))
                    return;
                int users = 0;
                foreach (var u in stateOld.VoiceChannel.Users)
                {
                    if (!u.IsBot)
                    {
                        users++;
                    }
                }
                if (users < 1)
                {
                    IAudioClient aClient;
                    var userG = (SocketGuildUser)user;
                    audioDict.TryGetValue(userG.Guild.Id, out aClient);
                    if (aClient == null)
                    {
                        return;
                    }
                    await aClient.StopAsync();
                    aClient.Dispose();
                    audioDict.TryRemove(userG.Guild.Id, out aClient);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
        public async Task AddQueueYT(SocketCommandContext Context, string name, InteractiveService interactive)
        {
            try
            {
                IAudioClient aClient;
                audioDict.TryGetValue(Context.Guild.Id, out aClient);
                if (aClient == null)
                {
                    await Context.Channel.SendMessageAsync(":no_entry_sign: Bot is not connected to any Voice Channels");
                    return;
                }
                var _channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
                if (_channel == null)
                {
                    await Context.Channel.SendMessageAsync(
                        ":no_entry_sign: You must be in the same Voice Channel as me!");
                    return;
                }
                var channel = (Context.Guild as SocketGuild).CurrentUser.VoiceChannel as IVoiceChannel;
                if (channel.Id != _channel.Id)
                {
                    await Context.Channel.SendMessageAsync(":no_entry_sign: You must be in the same Voice Channel as the me!");
                    return;
                }

                var msg =
                    await Context.Channel.SendMessageAsync(
                        ":arrows_counterclockwise: Searching, Downloading and Adding to Queue...");


                string tempName = await _ytService.GetYtURL(Context, name, interactive, msg);

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
                string nameT = await Download(tempName, msg, Context);
                if (!nameT.Equals("f"))
                {
                    List<SongStruct> tempList = new List<SongStruct>();
                    SongStruct tempStruct = new SongStruct
                    {
                        name = nameT,
                        user = $"{Context.User.Username}#{Context.User.Discriminator}"
                    };
                    if (queueDict.ContainsKey(Context.Guild.Id))
                    {
                        queueDict.TryGetValue(Context.Guild.Id, out tempList);
                        tempList.Add(tempStruct);
                        queueDict.TryUpdate(Context.Guild.Id, tempList);
                    }
                    else
                    {
                        tempList.Add(tempStruct);
                        queueDict.TryAdd(Context.Guild.Id, tempList);
                    }
                    SaveDatabase();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task AddQueue(string url, SocketCommandContext Context)
        {
            try
            {
                IAudioClient aClient;
                audioDict.TryGetValue(Context.Guild.Id, out aClient);
                if (aClient == null)
                {
                    await Context.Channel.SendMessageAsync(":no_entry_sign: Bot is not connected to any Voice Channels");
                    return;
                }
                var _channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
                if (_channel == null)
                {
                    await Context.Channel.SendMessageAsync(
                        ":no_entry_sign: You must be in the same Voice Channel as me!");
                    return;
                }
                var channel = (Context.Guild as SocketGuild).CurrentUser.VoiceChannel as IVoiceChannel;
                if (channel.Id != _channel.Id)
                {
                    await Context.Channel.SendMessageAsync(":no_entry_sign: You must be in the same Voice Channel as the me!");
                    return;
                }

                var msg =
                    await Context.Channel.SendMessageAsync(
                        ":arrows_counterclockwise: Downloading and Adding to Queue...");

                string nameT = await Download(url, msg, Context);
                if (!nameT.Equals("f"))
                {
                    List<SongStruct> tempList = new List<SongStruct>();
                    SongStruct tempStruct = new SongStruct
                    {
                        name = nameT,
                        user = $"{Context.User.Username}#{Context.User.Discriminator}"
                    };
                    if (queueDict.ContainsKey(Context.Guild.Id))
                    {
                        queueDict.TryGetValue(Context.Guild.Id, out tempList);
                        tempList.Add(tempStruct);
                        queueDict.TryUpdate(Context.Guild.Id, tempList);
                    }
                    else
                    {
                        tempList.Add(tempStruct);
                        queueDict.TryAdd(Context.Guild.Id, tempList);
                    }
                    SaveDatabase();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task PlayQueue(SocketCommandContext Context)
        {
            try
            {
                IAudioClient client;
                if (!audioDict.TryGetValue(Context.Guild.Id, out client))
                {
                    await Context.Channel.SendMessageAsync(":no_entry_sign: Bot must first join a Voice Channel!");
                    return;
                }
                await Context.Channel.SendMessageAsync(":musical_note: Started playing");
                await PlayQueueAsync(client, Context);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task ClearQueue(SocketCommandContext Context)
        {
            try
            {
                List<SongStruct> queue = new List<SongStruct>();
                if (!queueDict.TryGetValue(Context.Guild.Id, out queue))
                {
                    await Context.Channel.SendMessageAsync(
                        ":no_entry_sign: You first have to create a Queue by adding atleast one song!");
                }
                else
                {
                    /*await StopMusic(Context);
                    foreach(var item in queue)
                    {
                        if(File.Exists(item.name + ".info.json"))
                            File.Delete(item.name + ".info.json");
                        if (File.Exists(item.name + ".mp3"))
                            File.Delete(item.name + ".mp3");
                    }*/
                    queue.Clear();
                    queueDict.TryUpdate(Context.Guild.Id, queue);
                    await Context.Channel.SendMessageAsync(":put_litter_in_its_place:  Cleared the entire list.");
                    SaveDatabase();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task SkipQueueEntry(SocketCommandContext Context)
        {
            try
            {
                IAudioClient client;
                if (!audioDict.TryGetValue(Context.Guild.Id, out client))
                {
                    List<SongStruct> queue = new List<SongStruct>();
                    if (!queueDict.TryGetValue(Context.Guild.Id, out queue))
                    {
                        await Context.Channel.SendMessageAsync(
                            ":no_entry_sign: You first have to create a Queue by adding atleast one song!");
                    }
                    else
                    {
                        if (queue.Count < 1)
                        {
                            await Context.Channel.SendMessageAsync(
                            ":no_entry_sign: The queue is empty! Nothing to skip here..");
                            return;
                        }
                       /* await StopMusic(Context);

                        if (File.Exists(queue[0].name + ".info.json"))
                            File.Delete(queue[0].name + ".info.json");
                        if (File.Exists(queue[0].name + ".mp3"))
                            File.Delete(queue[0].name + ".mp3");*/

                        queue.RemoveAt(0);
                        queueDict.TryUpdate(Context.Guild.Id, queue);
                        SaveDatabase();
                        await Context.Channel.SendMessageAsync(":track_next: Skipped first entry in Queue");
                    }
                }
                else
                {
                    List<SongStruct> queue = new List<SongStruct>();
                    if (!queueDict.TryGetValue(Context.Guild.Id, out queue))
                    {
                        await Context.Channel.SendMessageAsync(
                            ":no_entry_sign: You first have to create a Queue by adding atleast one song!");
                    }
                    else
                    {
                        if (queue.Count < 1)
                        {
                            await Context.Channel.SendMessageAsync(
                            ":no_entry_sign: The queue is empty! Nothing to skip here..");
                            return;
                        }
                        /*await StopMusic(Context);

                        if (File.Exists(queue[0].name + ".info.json"))
                            File.Delete(queue[0].name + ".info.json");
                        if (File.Exists(queue[0].name + ".mp3"))
                            File.Delete(queue[0].name + ".mp3");*/

                        queue.RemoveAt(0);
                        queueDict.TryUpdate(Context.Guild.Id, queue);
                        SaveDatabase();
                        if (queue.Count == 0)
                        {
                            await Context.Channel.SendMessageAsync(":track_next: Queue is now empty!");
                            await StopMusic(Context);
                        }
                        else
                        {
                            await Context.Channel.SendMessageAsync(
                                   ":track_next: Skipped first entry in Queue and started playing next song!");
                            await PlayQueueAsync(client, Context);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private Process CreateStream(string path)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i {path}.mp3 -ac 2 -loglevel quiet -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true

            };
            return Process.Start(ffmpeg);
        }
        private async Task PlayQueueAsync(IAudioClient client, SocketCommandContext Context)
        {
            try
            {
                if (queueDict.ContainsKey(Context.Guild.Id))
                {
                    List<SongStruct> queue = new List<SongStruct>();
                    queueDict.TryGetValue(Context.Guild.Id, out queue);
                    for (int i = 1; i <= queue.Count;)
                    {
                        string name = queue[0].name;
                        var ffmpeg = CreateStream(name);
                        
                        audioStream_Token strToken;
                        if (audioStreamDict.ContainsKey(client))
                        {
                            audioStreamDict.TryGetValue(client, out strToken);
                            strToken.tokenSource.Cancel();
                            strToken.tokenSource.Dispose();
                            strToken.tokenSource = new CancellationTokenSource();
                            strToken.token = strToken.tokenSource.Token;
                            audioStreamDict.TryUpdate(client, strToken);
                        }
                        else
                        {
                            strToken.audioStream = client.CreatePCMStream(AudioApplication.Music, 98304, 1920);
                            strToken.tokenSource = new CancellationTokenSource();
                            strToken.token = strToken.tokenSource.Token;
                            audioStreamDict.TryAdd(client, strToken);
                        }

                        var output = ffmpeg.StandardOutput.BaseStream; //1920, 2880, 960
                        await output.CopyToAsync(strToken.audioStream, 1920, strToken.token).ContinueWith(task =>
                        {
                            if (!task.IsCanceled && task.IsFaulted) //supress cancel exception
                               Console.WriteLine(task.Exception);
                        });

                        ffmpeg.WaitForExit();
                        
                        await strToken.audioStream.FlushAsync();
                        queueDict.TryGetValue(Context.Guild.Id, out queue);

                        /*if (File.Exists(queue[0].name + ".info.json"))
                            File.Delete(queue[0].name + ".info.json");
                        if (File.Exists(queue[0].name + ".mp3"))
                            File.Delete(queue[0].name + ".mp3");*/

                        queue.RemoveAt(0);
                        queueDict.TryUpdate(Context.Guild.Id, queue);
                        SaveDatabase();
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(":no_entry_sign: Please add a song to the Queue first!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task StopMusic(SocketCommandContext Context)
        {
            try
            {
                if (audioDict.ContainsKey(Context.Guild.Id))
                {
                    IAudioClient client;
                    audioDict.TryGetValue(Context.Guild.Id, out client);

                    if (audioStreamDict.ContainsKey(client))
                    {
                        audioStream_Token strAT;
                        audioStreamDict.TryGetValue(client, out strAT);

                        strAT.tokenSource.Cancel();

                        strAT.tokenSource.Dispose();
                        strAT.tokenSource = new CancellationTokenSource();
                        strAT.token = strAT.tokenSource.Token;
                        audioStreamDict.TryUpdate(client, strAT);


                        await Context.Channel.SendMessageAsync(":stop_button: Stopped the Music Playback!");
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(":no_entry_sign: Currently not playing anything!");
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(":no_entry_sign: I didn't even join a Channel yet!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task QueueList(SocketCommandContext Context)
        {
            List<SongStruct> queue = new List<SongStruct>();
            if (queueDict.TryGetValue(Context.Guild.Id, out queue))
            {
                if (queue.Count != 0)
                {
                    try
                    {
                        var eb = new EmbedBuilder()
                        {
                            Color = new Color(4, 97, 247),
                            Footer = new EmbedFooterBuilder()
                            {
                                Text = $"Requested by {Context.User.Username}#{Context.User.Discriminator}",
                                IconUrl = (Context.User.GetAvatarUrl())
                            }
                        };

                        eb.Title = "Queue List";
                        var infoJsonT = File.ReadAllText($"{queue[0].name}.info.json");
                        var infoT = JObject.Parse(infoJsonT);

                        var titleT = infoT["fulltitle"].ToString();
                        if (String.IsNullOrWhiteSpace(titleT))
                            titleT = "Couldn't find name";

                        eb.AddField((efb) =>
                        {
                            int duration = 0;
                            var con = Int32.TryParse(infoT["duration"].ToString(), out duration);
                            string dur = "00:00";
                            if (con)
                                dur = Convert(duration);
                            efb.Name = "Now playing";
                            efb.IsInline = true;
                            efb.Value = $"[{dur}] - **[{titleT}]({StringEncoder.Base64Decode(queue[0].name)})** \n      \t*by {queue[0].user}*";
                        });



                        int lenght = 0;
                        if (queue.Count > 11)
                        {
                            lenght = 11;
                        }
                        else
                        {
                            lenght = queue.Count;
                        }
                        for (int i = 1; i < lenght; i++)
                        {
                            eb.AddField((efb) =>
                            {
                                efb.Name = $"#{i} by {queue[i].user}";
                                efb.IsInline = false;
                                var infoJson = File.ReadAllText($"{queue[i].name}.info.json");
                                var info = JObject.Parse(infoJson);

                                int duration = 0;
                                var con = Int32.TryParse(info["duration"].ToString(), out duration);
                                string dur = "00:00";
                                if (con)
                                    dur = Convert(duration);

                                var title = info["fulltitle"].ToString();
                                if (String.IsNullOrWhiteSpace(title))
                                    title = "Couldn't find name";
                                efb.Value += $"[{dur}] - **[{title}]({StringEncoder.Base64Decode(queue[i].name)})**";
                            });
                        }
                        if (queue.Count == 1)
                        {
                            eb.AddField((efb) =>
                            {
                                efb.Name = "Queue";
                                efb.IsInline = false;
                                efb.Value = "No Songs in Queue";
                            });
                        }


                        await Context.Channel.SendMessageAsync("", false, eb.Build());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(":no_entry_sign: Queue is empty!");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(
                    ":no_entry_sign: You first have to create a Queue by adding atleast one song!");
            }
        }

        public async Task NowPlaying(SocketCommandContext Context)
        {
            try
            {
                List<SongStruct> queue = new List<SongStruct>();
                if (queueDict.TryGetValue(Context.Guild.Id, out queue))
                {
                    if (queue.Count != 0)
                    {
                        var infoJson = File.ReadAllText($"{queue[0].name}.info.json");
                        var info = JObject.Parse(infoJson);

                        var title = info["fulltitle"];

                        var eb = new EmbedBuilder()
                        {
                            Color = new Color(4, 97, 247),
                            Footer = new EmbedFooterBuilder()
                            {
                                Text = $"Requested by {Context.User.Username}#{Context.User.Discriminator}",
                                IconUrl = (Context.User.GetAvatarUrl())
                            }
                        };

                        eb.AddField((efb) =>
                        {
                            int duration = 0;
                            var con = Int32.TryParse(info["duration"].ToString(), out duration);
                            string dur = "00:00";
                            if (con)
                                dur = Convert(duration);
                            efb.Name = "Now playing";
                            efb.IsInline = false;
                            efb.Value = $"[{dur}] - [{title}]({StringEncoder.Base64Decode(queue[0].name)})";
                        });

                        eb.AddField((x) =>
                        {
                            x.Name = "Requested by";
                            x.IsInline = false;
                            x.Value = queue[0].user;
                        });

                        await Context.Channel.SendMessageAsync("", false, eb.Build());
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(":no_entry_sign: Queue is empty!");
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(
                        ":no_entry_sign: You first have to create a Queue by adding atleast one song!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }



        private Process YtDl(string path, string name)
        {
            var ytdl = new ProcessStartInfo
            {
                FileName = "youtube-dl",
                Arguments =
                    $"-i -x --no-playlist --max-filesize 20m --audio-format mp3 --audio-quality 0 --output \"{name}.%(ext)s\"  {path} --write-info-json" //--audio-format mp3
                ,CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            return Process.Start(ytdl);
        }

        private Process CheckerYtDl(string path)
        {
            var ytdl = new ProcessStartInfo
            {
                FileName = "youtube-dl",
                Arguments =
                    $"-J --flat-playlist {path}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            return Process.Start(ytdl);
        }

        private async Task<string> Download(string path, IUserMessage msg, SocketCommandContext Context)
        {
            bool stream = false;

            if (path.Contains("https://www.youtube.com/watch?v=") && path.Contains("&"))//path.Contains('&'))
            {
                var split = path.Split('&');
                path = split[0];
            }
            string name = StringEncoder.Base64Encode(path);


            Process ytdl = new Process();
            if (!File.Exists(name + ".mp3"))
            {
                try
                {
                    var ytdlChecker = CheckerYtDl(path);
                    ytdlChecker.ErrorDataReceived += (x, y) =>
                    {
                        stream = false;
                        Console.WriteLine("YTDL CHECKER FAILED");
                    };
                    string output = ytdlChecker.StandardOutput.ReadToEnd();
                    ytdlChecker.WaitForExit();
                    if (ytdlChecker.ExitCode != 0)
                    {
                        await msg.ModifyAsync(x =>
                        {
                            x.Content =
                                ":no_entry_sign: YT-DL Error. Possible reasons: Video is blocked in Bot's country, NO LIVESTREAMS or a plain YT-DL bug. Retry once";
                        });
                        return "f";
                    }

                    IDictionary<string, JToken> json = JObject.Parse(output);

                    if (json.ContainsKey("is_live") && !String.IsNullOrEmpty(json["is_live"].Value<string>()))
                    {
                        stream = false;
                        ytdl = YtDl("", name);
                        Console.WriteLine("YTDL CHECKER LIVE DETECTED");
                    }
                    else
                    {
                        ytdl = YtDl(path, name);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            if (name != null)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                stopwatch.Start();
                while (!File.Exists(name + ".mp3"))
                {
                    if (ytdl != null)
                    {
                        if (ytdl.HasExited)
                            break;
                    }
                    if (stopwatch.ElapsedMilliseconds > 60000)
                    {
                        stopwatch.Stop();
                        try
                        {
                            ytdl.Kill();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        var dir = new DirectoryInfo(".");

                        foreach (var file in dir.EnumerateFiles("*.part"))
                        {
                            file.Delete();
                        }
                        File.Delete($"{name}.info.json");
                        await msg.ModifyAsync(x =>
                        {
                            x.Content =
                                ":no_entry_sign: The Server that hosts the Video you tried to download is way to slow. Try another, faster service!";
                        });
                        return "f";
                    }
                }
                if (File.Exists(name + ".mp3"))
                {
                    try
                    {

                        await msg.ModifyAsync(
                            x => { x.Content = ":musical_note: Successfully Downloaded."; });
                        stream = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    await msg.ModifyAsync(x =>
                    {
                        x.Content =
                            ":no_entry_sign: Failed to Download. Possible reasons: Video is blocked in Bot's country, Video was too long, NO PLAYLISTS AND NO LIVE STREAMS!";
                    });
                }
            }
            else
            {
                await msg.ModifyAsync(x => { x.Content = "It must be a YT link! Failed to Download."; });
            }
            if (stream)
            {
                return name;
            }
            else
            {
                return "f";
            }
        }
        //$"/c \"opusenc {name}.wav {name}.opus\"", ///c \"ffmpeg -i {name}.mp3 -f s16le pipe:0 | opusenc - > --bitrate 268 {name}.opus\"
        private Process OpusEncoding(string name)
        {
            var opus = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"ffmpeg -i {name}.mp3 -f s16le pipe:0 | opusenc - > --bitrate 268 {name}.opus\"",
                    //$"/c \"opusenc {name}.wav {name}.opus\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            return Process.Start(opus);
        }

        public string Convert(int value)
        {
            TimeSpan ts = TimeSpan.FromSeconds(value);
            if (value > 3600)
            {
                return String.Format("{0}:{1}:{2:D2}", ts.Hours, ts.Minutes, ts.Seconds);
            }
            return String.Format("{0}:{1:D2}", ts.Minutes, ts.Seconds);
        }


        public struct audioStream_Token
        {
            public AudioOutStream audioStream;
            public CancellationTokenSource tokenSource;
            public CancellationToken token;

            public audioStream_Token(AudioOutStream stream, CancellationTokenSource source, CancellationToken _token)
            {
                audioStream = stream;
                tokenSource = source;
                token = _token;
            }
        }

        public int PlayingFor()
        {
            return audioDict.Count;
        }

        private void InitializeLoader()
        {
            _jSerializer.Converters.Add(new JavaScriptDateTimeConverter());
            _jSerializer.NullValueHandling = NullValueHandling.Ignore;
        }

        public void SaveDatabase()
        {
            using (StreamWriter sw = File.CreateText("MusicQueue.json"))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    _jSerializer.Serialize(writer, queueDict);
                }
            }
        }

        private void LoadDatabase()
        {
            if (File.Exists("MusicQueue.json"))
            {
                using (StreamReader sr = File.OpenText("MusicQueue.json"))
                {
                    using (JsonReader reader = new JsonTextReader(sr))
                    {
                        var temp = _jSerializer.Deserialize<ConcurrentDictionary<ulong, List<SongStruct>>>(reader);
                        if (temp == null)
                            return;
                        queueDict = temp;
                    }
                }
            }
            else
            {
                File.Create("MusicQueue.json").Dispose();
            }
        }
    }// CLASS

    public static class Extensions
    {
        public static bool TryUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> self, TKey key, TValue value)
        {
            TValue currentValue = default(TValue);
            if (self.ContainsKey(key))
                self.TryGetValue(key, out currentValue);
            return self.TryUpdate(key, value, currentValue);
        }

        public static TValue TryGet<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> self, TKey key)
        {
            TValue value = default(TValue);
            if (self.ContainsKey(key))
                self.TryGetValue(key, out value);
            return value;
        }
    }

    public static class StringEncoder
    {

        static readonly char[] padding = { '=' };

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes).TrimEnd(padding).Replace('+', '-').Replace('/', '_');
        }

        public static string Base64Decode(string base64EncodedData)
        {
            string incoming = base64EncodedData.Replace('_', '/').Replace('-', '+');
            switch (base64EncodedData.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            var base64EncodedBytes = Convert.FromBase64String(incoming);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }


}




