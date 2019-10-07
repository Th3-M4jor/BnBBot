using Newtonsoft.Json;
using System.IO;

namespace csharp
{
    public sealed class config
    {

        public static readonly config instance;

        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string Prefix { get; private set; }

        [JsonProperty("majorID")]
        public string MajorID
        {

            set
            {
                MajorIDConverted = ulong.Parse(value);
            }

        }

        [JsonProperty("jinID")]
        public string JinID
        {
            set
            {
                JinIDConverted = ulong.Parse(value);
            }
        }


        [JsonProperty("dbPasswd")]
        public string DBPass
        {
            get; set;
        }

        public ulong MajorIDConverted { get; private set; }

        public ulong JinIDConverted { get; private set; }

        static config()
        {
            using (var jsonTextFile = File.OpenText("./config.json"))
            {
                string jsonText = jsonTextFile.ReadToEnd();
                instance = JsonConvert.DeserializeObject<config>(jsonText);
            }
        }

        private config()
        {
            
        }

    }
}