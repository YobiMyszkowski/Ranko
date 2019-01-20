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

namespace Ranko.Modules
{
    [Name("Role")]
    public class RoleModule : ModuleBase<SocketCommandContext>
    {
        string[] selfNotRoles = { "Index" };//, "Botbotwot", "Reinbaw", "Retired", "Guest", "@everyone", "Temporary Overlord", "Memegician Odin", "Case", "Emoji manager", "Reinbaw?", "Zombie", "ExDanchou"};

        [Command("rolelist")]
        [Remarks("Message list of all roles")]
        [MinPermissions(AccessLevel.User)]
        public async Task rolelist()
        {
            string list = null;
            foreach (SocketRole role in Context.Guild.Roles)
            {
                if(!selfNotRoles.Contains(role.Name))
                {
                    list += role.Name + " ,";
                }
            }
            await Context.Channel.SendMessageAsync(list);
        }
        [Command("iam")]
        [Remarks("Add role to user")]
        [MinPermissions(AccessLevel.User)]
        public async Task iam([Remainder]string text)
        {
            foreach (var roleName in Context.Guild.Roles)
            {
                if(text == roleName.Name && !selfNotRoles.Contains(roleName.Name))
                {
                    if (((SocketGuildUser)Context.User).Roles.Contains(roleName))
                    {
                        var builder = new EmbedBuilder()
                        {
                            Color = new Discord.Color(214, 10, 28),
                            Description = "U have that role already. Are u bakaaaaaaaa!?"
                        };
                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                        return;
                    }
                    else
                    {
                        await ((SocketGuildUser)Context.User).AddRoleAsync(roleName);
                        var builder = new EmbedBuilder()
                        {
                            Color = new Discord.Color(10, 214, 28),
                            Description = "U are now: " + roleName.Name
                        };
                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                        return;
                    }
                }
            }
            var builder1 = new EmbedBuilder()
            {
                Color = new Discord.Color(214, 10, 28),
                Description = "Such role doesnt exist or can't be assigned to you by me."
            };
            await Context.Channel.SendMessageAsync("", false, builder1.Build());

        }
        [Command("iamnot")]
        [Remarks("Remove role from user")]
        [MinPermissions(AccessLevel.User)]
        public async Task iamnot([Remainder]string text)
        {
            foreach (var roleName in Context.Guild.Roles)
            {
                if (text == roleName.Name && !selfNotRoles.Contains(roleName.Name))
                {
                    if (!((SocketGuildUser)Context.User).Roles.Contains(roleName))
                    {
                        var builder = new EmbedBuilder()
                        {
                            Color = new Discord.Color(214, 10, 28),
                            Description = "U don't have that role. Are u bakaaaaaaaa!?"
                        };
                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                        return;
                    }
                    else
                    {
                        await ((SocketGuildUser)Context.User).RemoveRoleAsync(roleName);
                        var builder = new EmbedBuilder()
                        {
                            Color = new Discord.Color(10, 214, 28),
                            Description = "I removed '" + roleName.Name + "' role from you"
                        };
                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                        return;
                    }
                }
            }
            var builder1 = new EmbedBuilder()
            {
                Color = new Discord.Color(214, 10, 28),
                Description = "Such role doesnt exist or can't be removed from you by me."
            };
            await Context.Channel.SendMessageAsync("", false, builder1.Build());
        }

    }
}
