using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.InteractiveCommands;
using Ranko.Services;
using Ranko.Preconditions;
using Ranko;
using System;

[Name("Music")]
public class AudioModule : ModuleBase<SocketCommandContext>
{
    public MusicService musicService;
    private InteractiveService _interactive;
    public AudioModule(MusicService _musicService, InteractiveService inter)
    {
        musicService = _musicService;
        _interactive = inter;
    }

    [Command("join", RunMode = RunMode.Async), Summary("Joines the channel of the User")]
    [Remarks("Joines the channel of the User")]
    [MinPermissions(AccessLevel.User)]
    public async Task JoinChannel()
    {
        var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
        if (channel == null)
        {
            await Context.Channel.SendMessageAsync(
                "User must be in a voice channel");
            return;
        }
        try
        {
            await musicService.JoinChannel(channel, Context.Guild.Id);
        }
        catch (Exception e)
        {
            Console.WriteLine("{0} Exception caught.", e);
        }
    }
    [Command("add", RunMode = RunMode.Async), Summary("Adds selected song to Queue")]
    [Remarks("Adds selected song to Queue")]
    [MinPermissions(AccessLevel.User)]
    public async Task AddToQueue([Summary("URL to add"), Remainder] string url)
    {
        if (musicService.GetQueeList().Count >= 9)
        {
            await Context.Channel.SendMessageAsync("Max 10 songs in queue. Dont make my master harddrive full of shit.");
        }
        else
        {
            if (url.Contains("http://") || url.Contains("https://"))
            {
                await musicService.AddQueue(url, Context);
            }
            else
            {
                await musicService.AddQueueYT(Context, url, _interactive);
            }
        }
    }

    [Command("addPlaylist", RunMode = RunMode.Async), Summary("Adds songs from playlist to Queue")]
    [Remarks("Adds songs from playlist to Queue")]
    [MinPermissions(AccessLevel.BotOwner)]
    public async Task AddToQueueYT([Summary("Playlist id"), Remainder] string name)
    {
        var x = await musicService._ytService.GetYtPlaylist(name, musicService, Context);
    }

    [Command("skip", RunMode = RunMode.Async), Summary("Skip current song in queue")]
    [Remarks("Skip current song in queue")]
    [MinPermissions(AccessLevel.User)]
    public async Task SkipQueue()
    {
        await musicService.SkipQueueEntry(Context);
    }

    [Command("clear", RunMode = RunMode.Async),
     Summary("Clears the entire music queue, requires Manage Channels permission though")]
    [Remarks("Clears the entire music queue, requires Manage Channels permission though")]
    [MinPermissions(AccessLevel.BotOwner)]
    public async Task ClearQ()
    {
        if (((SocketGuildUser)Context.User).GuildPermissions.Has(GuildPermission.ManageChannels))
        {
            await musicService.ClearQueue(Context);
        }
        else
        {
            await ReplyAsync(":no_entry_sign: You don't have the Manage Channels permission to clear the Queue!");
        }

    }

    [Command("list"), Summary("Shows a list of all songs in the Queue")]
    [Alias("queue")]
    [Remarks("Shows a list of all songs in the Queue")]
    [MinPermissions(AccessLevel.User)]
    public async Task List()
    {
        await musicService.QueueList(Context);
    }

    [Command("np"), Summary("Tells you which song is currently playing")]
    [Remarks("Tells you which song is currently playing")]
    [MinPermissions(AccessLevel.User)]
    public async Task NowPlaying()
    {
        await musicService.NowPlaying(Context);
    }

    [Command("play", RunMode = RunMode.Async), Summary("Plays the qurrent queue")]
    [Remarks("Plays the qurrent queue")]
    [MinPermissions(AccessLevel.User)]
    public async Task PlayQueue()
    {
        await musicService.PlayQueue(Context);
    }

    [Command("leave", RunMode = RunMode.Async), Summary("Leaves the voice channel in which the User is in.")]
    [Remarks("Leaves the voice channel in which the User is in.")]
    [MinPermissions(AccessLevel.User)]
    public async Task LeaveChannel()
    {
        var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
        if (channel == null)
        {
            await Context.Channel.SendMessageAsync(
                "User must be in a voice channel, or a voice channel must be passed as an argument");
            return;
        }

        await musicService.LeaveChannel(Context, channel);
    }

    [Command("stop", RunMode = RunMode.Async), Summary("Stops the current Audioplayer")]
    [Remarks("Stops the current Audioplayer")]
    [MinPermissions(AccessLevel.User)]
    public async Task StopMusic()
    {
        await musicService.StopMusic(Context);
    }

}
