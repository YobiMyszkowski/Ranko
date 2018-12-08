using Newtonsoft.Json;
using System;
using System.IO;

namespace Ranko
{
    public class Configuration
    {
        [JsonIgnore]
        /// <summary> The location and name of your bot's configuration file. </summary>
        public static string FileName { get; private set; } = "config/configuration.json";
        /// <summary> Ids of users who will have owner access to the bot. </summary>
        public ulong[] Owners { get; set; }
        /// <summary> Your bot's command prefix. </summary>
        public string Prefix { get; set; } = "!";
        /// <summary> Your bot's login token. </summary>
        public string Token { get; set; } = "";
        public string YTToken { get; set; } = "";

        public static void EnsureExists()
        {
            
            string file = Path.Combine(Environment.CurrentDirectory, FileName);
            if (!File.Exists(file))                                 // Check if the configuration file exists.
            {
                string path = Path.GetDirectoryName(file);          // Create config directory if doesn't exist.
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var config = new Configuration();                   // Create a new configuration object.

                Console.WriteLine("Please enter your discor bot token: ");
                string token = Console.ReadLine();                  // Read the bot token from console.
                config.Token = token;
                ulong[] master = {211467860940161025};
                config.Prefix = "!";
                config.Owners = master;
                Console.WriteLine("Please enter your youtube api token: ");
                token = Console.ReadLine();
                config.YTToken = token;

                config.SaveJson();                                  // Save the new configuration object to file.
            }
            Console.WriteLine("Configuration Loaded");
        }

        /// <summary> Save the configuration to the path specified in FileName. </summary>
        public void SaveJson()
        {
            string file = Path.Combine(Environment.CurrentDirectory, FileName);
            File.WriteAllText(file, ToJson());
        }

        /// <summary> Load the configuration from the path specified in FileName. </summary>
        public static Configuration Load()
        {
            string file = Path.Combine(Environment.CurrentDirectory, FileName);
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(file));
        }

        /// <summary> Convert the configuration to a json string. </summary>
        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
