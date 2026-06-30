using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public interface ISteamWebAPI
    {
        Task<List<SteamWebAPI.Model.WorkshopItemDetails>> GetWorkshopItemDetails(string[] idList);
    }
    public class SteamWebAPI : ISteamWebAPI
    {
        public static class Model
        {
            public class Tag
            {
                [JsonProperty("Tag")]
                public string Name;
            }

            public class WorkshopItemDetails
            {
                public string PublishedFileId;
                public int Result;
                public string Creator;
                [JsonProperty("creator_app_id")]
                public int CreatorAppId;
                [JsonProperty("consumer_app_id")]
                public int ConsumerAppId;
                public string FileName;
                [JsonProperty("file_size")]
                public long FileSize;
                [JsonProperty("file_url")]
                public string FileURL;
                [JsonProperty("hcontent_file")]
                public string HContentFile;
                [JsonProperty("preview_url")]
                public string PreviewURL;
                [JsonProperty("hcontent_preview")]
                public string HContentPreview;
                public string Title;
                public string Description;
                [JsonProperty("time_created")]
                public int TimeCreated;
                [JsonProperty("time_updated")]
                public int TimeUpdated;
                public int Visibility;
                public int Banned;
                [JsonProperty("ban_reason")]
                public string BanReason;
                public int Subscriptions;
                public int Favorited;
                [JsonProperty("lifetime_subscriptions")]
                public int LifetimeSubscriptions;
                [JsonProperty("lifetime_favorited")]
                public int LifetimeFavorited;
                public int Views;
                public List<Tag> Tags;
            }
        }

        private readonly HttpClient _client;
        private readonly string _baseAPIURL = "https://api.steampowered.com/";

        public SteamWebAPI(HttpClient client)
        {
            _client = client;
        }

        public async Task<List<Model.WorkshopItemDetails>> GetWorkshopItemDetails(string[] idList)
        {
            string apiEndPoint = "ISteamRemoteStorage/GetPublishedFileDetails/v1/";

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("itemCount", idList.Length.ToString())
            };

            for (int i = 0; i < idList.Length; i++)
            {
                parameters.Add(new KeyValuePair<string, string>($"publishedfileids[{i}]", idList[i]));
            }

            var content = new FormUrlEncodedContent(parameters);
            try
            {
                var response = await _client.PostAsync(_baseAPIURL + apiEndPoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[SteamWebAPI] Error: {response.StatusCode}");
                    return new List<Model.WorkshopItemDetails>();
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject parsedJson = JObject.Parse(jsonResponse);
                JToken detailsToken = parsedJson["response"]?["publishedfiledetails"];

                if (detailsToken != null)
                {
                    return detailsToken.ToObject<List<Model.WorkshopItemDetails>>();
                }

                return new List<Model.WorkshopItemDetails>();
            }
            catch (Exception e)
            {
                LogFile.WriteLine(Messages.Get("steam_api_error").KeyFormat(("error", e.Message)));
                return new List<Model.WorkshopItemDetails>();
            }
        }
    }
}
