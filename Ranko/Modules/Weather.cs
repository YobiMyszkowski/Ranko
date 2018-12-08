using Discord.Commands;
using Ranko.Preconditions;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Drawing;
using System.IO;
using System;
using System.Net;
using System.Xml;
using System.Diagnostics;
using Discord;

namespace Ranko.Modules
{
    [Name("Weather")]
    public class WeatherModule : ModuleBase<SocketCommandContext>
    {
        private const string API_KEY = "0aa1248405c62b03a19277d7e007aa5b";
        [Command("weather")]
        [Remarks("Check weather from given place")]
        [MinPermissions(AccessLevel.User)]
        public async Task weather([Remainder]string city)
        {
            if (city != "" || city != null || city.Length > 0)
            {
                //imperial
                String CurrentUrl = string.Format("http://api.openweathermap.org/data/2.5/weather?{0}{1}{2}{3}", "q=", city, "&mode=xml&units=metric&APPID=", API_KEY);

                try
                {
                    using (WebClient client = new WebClient())
                    {
                        string xmlContent = client.DownloadString(CurrentUrl);
                        XmlDocument xmlDocument = new XmlDocument();
                        xmlDocument.LoadXml(xmlContent);

                        var node = xmlDocument.SelectSingleNode("//city");
                        var icon = xmlDocument.SelectSingleNode("//weather");
                        var builder = new EmbedBuilder()
                        {
                            Color = new Discord.Color(114, 137, 218),
                            Description = string.Format("Here is current weather for **{0}**", node.Attributes["name"].InnerText),
                            Footer = new EmbedFooterBuilder()
                            {
                                Text = $"Requested by {Context.User.Username}#{Context.User.Discriminator}",
                                IconUrl = (Context.User.GetAvatarUrl())
                            }
                            ,
                            ThumbnailUrl = string.Format("http://openweathermap.org/img/w/{0}.png", icon.Attributes["icon"].InnerText)
                        };
                        node = xmlDocument.SelectSingleNode("//temperature");
                        builder.AddField(x =>
                        {
                            x.Name = "temp.";
                            x.Value = node.Attributes["value"].InnerText + "°C";
                            x.IsInline = true;
                        });
                        node = xmlDocument.SelectSingleNode("//humidity");
                        builder.AddField(x =>
                        {
                            x.Name = "humidity";
                            x.Value = node.Attributes["value"].InnerText + "%";
                            x.IsInline = true;
                        });
                        node = xmlDocument.SelectSingleNode("//pressure");
                        builder.AddField(x =>
                        {
                            x.Name = "pressure";
                            x.Value = node.Attributes["value"].InnerText + "hPa";
                            x.IsInline = true;
                        });
                        node = xmlDocument.SelectSingleNode("//clouds");
                        builder.AddField(x =>
                        {
                            x.Name = "clouds";
                            x.Value = node.Attributes["name"].InnerText;
                            x.IsInline = true;
                        });
                        node = xmlDocument.SelectSingleNode("//speed");
                        builder.AddField(x =>
                        {
                            x.Name = "wind";
                            x.Value = node.Attributes["name"].InnerText;
                            x.IsInline = true;
                        });
                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                    }
                }
                catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                {
                    // handle 404 exceptions
                    var error = new EmbedBuilder()
                    {
                        Color = new Discord.Color(214, 10, 28),
                        Description = "Error 404. City not found."
                    };
                    await Context.Channel.SendMessageAsync("", false, error.Build());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                var error = new EmbedBuilder()
                {
                    Color = new Discord.Color(214, 10, 28),
                    Description = "Command is empty. Write city name which u wanna look into."
                };
                await Context.Channel.SendMessageAsync("", false, error.Build());
            }
        }
    }
}
