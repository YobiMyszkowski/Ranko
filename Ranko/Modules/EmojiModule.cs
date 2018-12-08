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

public static class GraphicsExtensions
{
    public static void DrawStringInside(this Graphics graphics, Rectangle rect, Font font, Brush brush, string text)
    {
        var textSize = graphics.MeasureString(text, font);
        var state = graphics.Save();
        graphics.TranslateTransform(rect.Left, rect.Top);
        graphics.ScaleTransform(rect.Width / textSize.Width, rect.Height / textSize.Height);
        graphics.DrawString(text, font, brush, PointF.Empty);
        graphics.Restore(state);
    }
}
namespace Ranko.Modules
{
    [Name("Emoji")]
    public class EmojiModule : ModuleBase<SocketCommandContext>
    {
        [Command("emo")]
        [Remarks("Post emoticon")]
        [MinPermissions(AccessLevel.User)]
        public async Task emo([Remainder]string text)
        {
            if (text != null)
            {
                text = text.ToLower();
                Bitmap background = null;

                /* check if emote folder exist and create it doesnt */
                if (!Directory.Exists("emotes"))
                    Directory.CreateDirectory("emotes");

                /* check if folder exist */
                if (Directory.Exists("emotes/"+text))
                {
                    var rand = new Random();
                    var files = Directory.GetFiles("emotes/" + text+ "/", "*.png");

                    background = new Bitmap(string.Format("{0}", files[rand.Next(files.Length)]));
                    Bitmap bitmap = new Bitmap(background.Width, background.Height);
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.DrawImage(background, 0, 0);
                    }
                    System.IO.Stream memoryStream = new System.IO.MemoryStream();
                    bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                    await Context.Channel.SendFileAsync(memoryStream, "emo.png");
                }
                else
                {
                    /* check if emote exist *.png only*/
                    if (!File.Exists(string.Format("{0}{1}.png", "emotes/", text)))
                    {
                        var builder = new EmbedBuilder()
                        {
                            Color = new Discord.Color(214, 10, 28),
                            Description = "My master doesnt own such emote. Check if u wrote it properly."
                        };
                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                        await Context.Message.DeleteAsync();
                    }
                    else
                    {
                        background = new Bitmap(string.Format("{0}{1}.png", "emotes/", text));
                        Bitmap bitmap = new Bitmap(background.Width, background.Height);
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.DrawImage(background, 0, 0);
                        }
                        System.IO.Stream memoryStream = new System.IO.MemoryStream();
                        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                        memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                        await Context.Channel.SendFileAsync(memoryStream, "emo.png");
                    }
                }
            }
        }

        [Command("addemo")]
        [Remarks("Add emoticon")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task addemo(string url, string name)
        {
            if (url != null)
            {
                if(name != null)
                {
                    /* check if folder exist */
                    if (Directory.Exists("emotes/" + name))
                    {
                        var files = Directory.GetFiles("emotes/" + name + "/", "*.png");
                        Bitmap file = LoadPicture(url);
                        if (file != null)
                        {
                            file.Save(string.Format("{0}{1}.png", "emotes/" + name + "/", files.Count()+1), System.Drawing.Imaging.ImageFormat.Png);
                            var builder = new EmbedBuilder()
                            {
                                Color = new Discord.Color(10, 214, 28),
                                Description = "Emote added succesfylly."
                            };
                            await Context.Channel.SendMessageAsync("", false, builder.Build());
                        }
                        else
                        {
                            var builder = new EmbedBuilder()
                            {
                                Color = new Discord.Color(214, 10, 28),
                                Description = "Thats not valid image file. Mby it use unsupported format. My master preffer *.png so stick with it.."
                            };
                            await Context.Channel.SendMessageAsync("", false, builder.Build());
                        }
                    }
                    else
                    {
                        if (!File.Exists(string.Format("{0}{1}.png", "emotes/", name)))
                        {
                            Bitmap file = LoadPicture(url);
                            if (file != null)
                            {
                                file.Save(string.Format("{0}{1}.png", "emotes/", name), System.Drawing.Imaging.ImageFormat.Png);
                                var builder = new EmbedBuilder()
                                {
                                    Color = new Discord.Color(10, 214, 28),
                                    Description = "Emote added succesfylly."
                                };
                                await Context.Channel.SendMessageAsync("", false, builder.Build());
                            }
                            else
                            {
                                var builder = new EmbedBuilder()
                                {
                                    Color = new Discord.Color(214, 10, 28),
                                    Description = "Thats not valid image file. Mby it use unsupported format. My master preffer *.png so stick with it.."
                                };
                                await Context.Channel.SendMessageAsync("", false, builder.Build());
                            }
                        }
                        else
                        {
                            var builder = new EmbedBuilder()
                            {
                                Color = new Discord.Color(214, 10, 28),
                                Description = "Emote with such name already exist. Add it using another string."
                            };
                            await Context.Channel.SendMessageAsync("", false, builder.Build());
                        }
                    }
                }
            }
            else
            {
                var builder = new EmbedBuilder()
                {
                    Color = new Discord.Color(214, 10, 28),
                    Description = "Thats not valid image file. Mby it use unsupported format. My master preffer *.png so stick with it."
                };
                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            await Context.Message.DeleteAsync();
        }

        [Command("slam")]
        [Remarks("Slam mentioned user")]
        [MinPermissions(AccessLevel.User)]
        public async Task slam([Remainder]SocketGuildUser user)
        {
            if (user.Status != Discord.UserStatus.Offline)
            {
                Bitmap background = LoadPicture("http://i.imgur.com/FSJM0kY.png");
                Bitmap foreground = new Bitmap(15, 15);
                foreground = LoadPicture(user.GetAvatarUrl());
                Bitmap bitmap = new Bitmap(background.Width, background.Height);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.DrawImage(background, 0, 0);
                    g.DrawImage(RotateImage(foreground, -45), 82, 195, 100, 100);
                }

                System.IO.Stream memoryStream = new System.IO.MemoryStream();
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(memoryStream, "slam.png");
            }
        }

        [Command("slap")]
        [Remarks("Slap mentioned user")]
        [MinPermissions(AccessLevel.User)]
        public async Task slap([Remainder]SocketGuildUser user)
        {
            if (user.Status != Discord.UserStatus.Offline)
            {
                Bitmap background = LoadPicture("https://i.imgur.com/iOAcKDa.png");
                Bitmap bitmap = new Bitmap(background.Width, background.Height);

                Font drawFont = new Font("Arial", 14);
                SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Black);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.DrawImage(background, 0, 0);
                    Rectangle rect1 = new Rectangle((background.Width / 2) -20, 225, background.Width/2, background.Height/10);
                    StringFormat stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Center;
                    stringFormat.LineAlignment = StringAlignment.Center;

                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    Font goodFont = FindFont(g, user.Username.ToUpper(), rect1.Size, drawFont);
                    g.DrawString(user.Username.ToUpper(), goodFont, drawBrush, rect1, stringFormat);
                }
                System.IO.Stream memoryStream = new System.IO.MemoryStream();
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(memoryStream, "slap.png");
            }
        }

        private static Bitmap LoadPicture(string url)
        {
            HttpWebRequest wreq;
            HttpWebResponse wresp;
            System.IO.Stream mystream;
            Bitmap bmp;

            bmp = null;
            mystream = null;
            wresp = null;
            try
            {
                wreq = (HttpWebRequest)WebRequest.Create(url);
                wreq.AllowWriteStreamBuffering = true;

                wresp = (HttpWebResponse)wreq.GetResponse();

                if ((mystream = wresp.GetResponseStream()) != null)
                    bmp = new Bitmap(mystream);
            }
            finally
            {
                if (mystream != null)
                    mystream.Close();

                if (wresp != null)
                    wresp.Close();
            }
            return (bmp);
        }
        private static Bitmap RotateImage(Bitmap rotateMe, float angle)
        {
            var bmp = new Bitmap(rotateMe.Width + (rotateMe.Width / 2), rotateMe.Height + (rotateMe.Height / 2));

            using (Graphics g = Graphics.FromImage(bmp))
                g.DrawImageUnscaled(rotateMe, (rotateMe.Width / 4), (rotateMe.Height / 4), bmp.Width, bmp.Height);

            rotateMe = bmp;

            Bitmap rotatedImage = new Bitmap(rotateMe.Width, rotateMe.Height);

            using (Graphics g = Graphics.FromImage(rotatedImage))
            {
                g.TranslateTransform(rotateMe.Width / 2, rotateMe.Height / 2);   //set the rotation point as the center into the matrix
                g.RotateTransform(angle);                                        //rotate
                g.TranslateTransform(-rotateMe.Width / 2, -rotateMe.Height / 2); //restore rotation point into the matrix
                g.DrawImage(rotateMe, new Point(0, 0));                          //draw the image on the new bitmap
            }
            return rotatedImage;
        }
        private Font FindFont(System.Drawing.Graphics g, string longString, Size Room, Font PreferedFont)
        {
            SizeF RealSize = g.MeasureString(longString, PreferedFont);
            float HeightScaleRatio = Room.Height / RealSize.Height;
            float WidthScaleRatio = Room.Width / RealSize.Width;
            float ScaleRatio = (HeightScaleRatio < WidthScaleRatio) ? ScaleRatio = HeightScaleRatio : ScaleRatio = WidthScaleRatio;
            float ScaleFontSize = PreferedFont.Size * ScaleRatio;
            return new Font(PreferedFont.FontFamily, ScaleFontSize, PreferedFont.Style, GraphicsUnit.Pixel);
        }
    }
}
