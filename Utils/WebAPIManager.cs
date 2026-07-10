using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static DotNETCoreDiscordBot.WebAPIManager;

namespace DotNETCoreDiscordBot
{
    public interface IWebAPIManager
    {
        Task<List<WebAPIManager.Model.WorkshopItemDetails>> GetWorkshopItemDetails(string[] idList);
        Task<Model.ReleaseDetails> GetLatestBotDetails();
    }
    public class WebAPIManager : IWebAPIManager
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
            public class ReleaseDetails
            {
                public string Name;
                public string HtmlUrl;
                public string TagName;
                public Version Version;
            }
        }

        private readonly HttpClient _client;
        private readonly ILogFile _logFile;
        private string _apiEndPoint;

        public WebAPIManager(HttpClient client, ILogFile logFile)
        {
            _client = client;
            _logFile = logFile;
        }

        public async Task<List<Model.WorkshopItemDetails>> GetWorkshopItemDetails(string[] idList)
        {
            _apiEndPoint = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";

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
                var response = await _client.PostAsync(_apiEndPoint, content);

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject parsedJson = JObject.Parse(jsonResponse);
                JToken detailsToken = parsedJson["response"]?["publishedfiledetails"];

                if (detailsToken != null)
                {
                    return detailsToken.ToObject<List<Model.WorkshopItemDetails>>();
                }
            }
            catch (Exception e)
            {
                _logFile.WriteLine(Messages.Get("web_api_error").KeyFormat(("error", e.Message)));
            }

            return new List<Model.WorkshopItemDetails>();
        }

        public async Task<Model.ReleaseDetails> GetLatestBotDetails()
        {
            _apiEndPoint = "https://api.github.com/repos/kaebalsaebal/PZDotNETCoreDiscordBot/releases/latest";

            Model.ReleaseDetails result = null;
            
            try
            {
                var response = await _client.GetAsync(_apiEndPoint);
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject parsedJson = JObject.Parse(jsonResponse);

                JToken detailsToken = parsedJson["id"];

                if (detailsToken != null)
                {
                    result = new Model.ReleaseDetails();

                    result.Name = parsedJson["name"].ToString();
                    result.TagName = parsedJson["tag_name"].ToString();
                    result.HtmlUrl = parsedJson["html_url"].ToString();

                    // Version tag example: vx.x.x
                    if (Regex.IsMatch(result.TagName, @"^v?\d+(\.\d+)*$", RegexOptions.IgnoreCase) &&
                        Version.TryParse(result.TagName.TrimStart('v'), out Version? version))
                    {
                        result.Version = version;
                    }
                }

            }
            catch (Exception e)
            {
                _logFile.WriteLine(Messages.Get("web_api_error").KeyFormat(("error", e.Message)));
            }

            return result;
        }
    }
}
