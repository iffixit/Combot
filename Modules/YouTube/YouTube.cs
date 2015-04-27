﻿using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Combot.IRCServices.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Combot.Modules.Plugins
{
    public class YouTube : Module
    {
        public override void Initialize()
        {
            Bot.CommandReceivedEvent += HandleCommandEvent;
        }

        public override void ParseCommand(CommandMessage command)
        {
            Command foundCommand = Commands.Find(c => c.Triggers.Contains(command.Command));

            switch (foundCommand.Name)
            {
                case "YouTube Search":
                    YoutubeSearch(command);
                    break;
            }
        }

        private void YoutubeSearch(CommandMessage command)
        {
            string urlTemplate = "https://www.googleapis.com/youtube/v3/search?part=snippet&q={0}&key={1}";
            Uri searchUrl = new Uri(string.Format(urlTemplate, command.Arguments["Query"], GetOptionValue("API Key")));
            WebClient web = new WebClient();
            web.Encoding = Encoding.UTF8;
            try
            {
                string page = web.DownloadString(searchUrl);

                JObject parsed = (JObject) JsonConvert.DeserializeObject(page);
                if (parsed["items"].Any())
                {
                    string videoID = parsed["items"].First()["id"].Value<string>("videoId");
                    string vidDescription = GetYoutubeDescription(videoID);
                    string youtubeMessage = string.Format("{0} - {1}", vidDescription, string.Format("http://youtu.be/{0}", videoID));
                    SendResponse(command.MessageType, command.Location, command.Nick.Nickname, youtubeMessage);
                }
                else
                {
                    string noResults = string.Format("No results found for \u0002{0}\u000F.", command.Arguments["Query"]);
                    SendResponse(command.MessageType, command.Location, command.Nick.Nickname, noResults);
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    int code = (int)((HttpWebResponse)ex.Response).StatusCode;
                    string errorCode = string.Format("Unable to search for \u0002{0}\u000F.  Youtube returned status code \u0002{1}\u000F.", command.Arguments["Query"], code);
                    SendResponse(command.MessageType, command.Location, command.Nick.Nickname, errorCode);
                }
                else
                {
                    string errorCode = string.Format("Unable to search for \u0002{0}\u000F.", command.Arguments["Query"]);
                    SendResponse(command.MessageType, command.Location, command.Nick.Nickname, errorCode);
                }
            }
        }

        private string GetYoutubeDescription(string ID)
        {
            string description = string.Empty;

            string urlTemplate = "http://gdata.youtube.com/feeds/api/videos/{0}?v=2&alt=jsonc";
            Uri searchUrl = new Uri(string.Format(urlTemplate, ID));
            WebClient web = new WebClient();
            web.Encoding = Encoding.UTF8;
            try
            {
                string page = web.DownloadString(searchUrl);

                JObject parsed = (JObject) JsonConvert.DeserializeObject(page);
                var data = parsed["data"];

                description = string.Format("\u0002{0}\u000F", data["title"]);

                if (data["duration"] == null)
                {
                    return description;
                }

                TimeSpan duration = TimeSpan.FromSeconds(data["duration"].Value<double>());
                description += string.Format(" | Length: \u0002{0}\u000F", duration.ToString("g"));

                if (data["ratingCount"] != null)
                {
                    int likes = data["likeCount"].Value<int>();
                    string pluralLikes = (likes > 1) ? "s" : string.Empty;
                    int dislikes = data["ratingCount"].Value<int>() - likes;
                    string pluralDislikes = (dislikes > 1) ? "s" : string.Empty;
                    double percent = 100.0*((double) likes/data["ratingCount"].Value<int>());
                    description += string.Format(" | Rating: {0} Like{1}, {2} Dislike{3} (\u0002{4}\u000F%)", likes, pluralLikes, dislikes, pluralDislikes, Math.Round(percent, 1));
                }

                if (data["viewCount"] != null)
                {
                    description += string.Format(" | Views: \u0002{0}\u000F", data["viewCount"].Value<int>());
                }

                DateTime uploadDate = Convert.ToDateTime(data["uploaded"].Value<string>());

                description += string.Format(" | Uploaded By: \u0002{0}\u000F on \u0002{1}\u000F", data["uploader"].Value<string>(), uploadDate.ToString("R"));

                if (data["contentRating"] != null)
                {
                    description += " | \u0002NSFW\u000F";
                }
            }
            catch (WebException ex)
            {
                description = string.Empty;
            }

            return description;
        }
    }
}
