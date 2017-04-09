using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace HomeAutomation.Dialogs
{

    [LuisModel("LUIS_APP_ID", "SUBSCRIPTION_KEY")]
    [Serializable]
    public class GiphyDialog : LuisDialog<object>
    {
        private const string GIPHY_URL = "http://api.giphy.com/v1/gifs/search?q=";
        private const string GUPHY_API_KEY = "&api_key=dc6zaTOxFJmzC";
        private const string NO_RECO_MESSAGE = "Sorry, I don't understand what you mean.";
        private const string START = "start";
        private const string STOP = "stop";
        private const int MAX_RANDOM = 15;

        private string intent;
        private string room;
        private string appliance;

        [LuisIntent("")]
        public async Task None (IDialogContext context, LuisResult result)
        {
            await context.PostAsync(NO_RECO_MESSAGE);
            context.Wait(MessageReceived);
        }

        [LuisIntent("on")]
        public async Task roomDeviceOn(IDialogContext context, LuisResult result)
        {
            intent = START;
            setEntitiesValues(new List<EntityRecommendation>(result.Entities));
            await getGiphyForResponseAsync(context);
        }

        [LuisIntent("off")]
        public async Task roomDeviceOff(IDialogContext context, LuisResult result)
        {
            intent = STOP;
            setEntitiesValues(new List<EntityRecommendation>(result.Entities));
            await getGiphyForResponseAsync(context);
        }

        private void setEntitiesValues(List<EntityRecommendation> entityList)
        {
            if (entityList != null)
            {
                foreach (var entity in entityList)
                {
                    if (entity.Type == "room")
                    {
                        room = entity.Entity.ToString();
                    }else if (entity.Type == "appliance")
                    {
                        appliance = entity.Entity.ToString();

                    }
                }
            }
        }

        private async Task getGiphyForResponseAsync(IDialogContext context)
        {
            var gifMessage = context.MakeMessage();
            var intentResponse = ((dynamic)await httpRequestAsync(GIPHY_URL + intent + GUPHY_API_KEY));
            var roomResponse = ((dynamic)await httpRequestAsync(GIPHY_URL + room + GUPHY_API_KEY));
            var applianceResponse = ((dynamic)await httpRequestAsync(GIPHY_URL + appliance + GUPHY_API_KEY));

            int intentCount = intentResponse.pagination.count;
            int roomCount = roomResponse.pagination.count;
            int applianceCount = applianceResponse.pagination.count;

            string intentUrl = intentResponse.data[getRandomNumber(intentCount)].images.fixed_height_small.url;
            string roomUrl = roomResponse.data[getRandomNumber(roomCount)].images.fixed_height_small.url;
            string applianceUrl = applianceResponse.data[getRandomNumber(applianceCount)].images.fixed_height_small.url;

            gifMessage.Attachments = constructMessage(intentUrl, roomUrl, applianceUrl);

            await context.PostAsync(gifMessage);
            context.Wait(MessageReceived);
        }

        private int getRandomNumber(int count)
        {
            Random random = new Random();
            return Math.Min(MAX_RANDOM, random.Next(count));
        }

        private List<Attachment> constructMessage(string intentUrl, string roomUrl, string applianceUrl)
        {
            List<Attachment> message = new List<Attachment>();
            message.Add(constructAttachment(intentUrl));
            message.Add(constructAttachment(roomUrl));
            message.Add(constructAttachment(applianceUrl));

            return message;
        }

        private async Task<JObject> httpRequestAsync(string url)
        {
            HttpClient client = new HttpClient();
            Task<string> httpRequestTask = client.GetStringAsync(url);

            string response = await httpRequestTask;
            return JObject.Parse(response);
        }

        private Attachment constructAttachment(string url)
        {
            return new Attachment()
            {
                ContentUrl = url,
                ContentType = "image/gif"
            };
        }
    }
}