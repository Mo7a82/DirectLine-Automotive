namespace DirectLineSampleClient
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.DirectLine;
    using Newtonsoft.Json;

    public class Program
    {
        private static string directLineSecret = ConfigurationManager.AppSettings["DirectLineSecret"];
        private static string botId = ConfigurationManager.AppSettings["BotId"];
        private static string fromUser = "DirectLineSampleClientUser";

        public static int Main(string[] args)
        {
            // The Bot Framework endpoints accept TLS 1.2 only.
            System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;

            if (string.IsNullOrWhiteSpace(directLineSecret) || string.IsNullOrWhiteSpace(botId))
            {
                Console.Error.WriteLine("DirectLineSecret and BotId are not set.");
                Console.Error.WriteLine("Copy DirectLine.secrets.config.example to DirectLine.secrets.config in the DirectLineClient folder, fill it in and build again.");
                return 1;
            }

            StartBotConversation().Wait();
            return 0;
        }

        private static async Task StartBotConversation()
        {
            DirectLineClient client = new DirectLineClient(directLineSecret);

            var conversation = await client.Conversations.StartConversationAsync();

            new System.Threading.Thread(async () => await ReadBotMessagesAsync(client, conversation.ConversationId)).Start();

            Console.WriteLine("Connected. Type a message, or 'exit' to quit.");
            Console.Write("Command> ");
            try
            {
                while (true)
                {
                    string input = (Console.ReadLine() ?? "exit").Trim();

                    if (input.ToLower() == "exit")
                    {
                        break;
                    }

                    if (input.Length > 0)
                    {
                        Activity userMessage = new Activity
                        {
                            From = new ChannelAccount(fromUser),
                            Text = input,
                            Type = ActivityTypes.Message
                        };

                        await client.Conversations.PostActivityAsync(conversation.ConversationId, userMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async Task ReadBotMessagesAsync(DirectLineClient client, string conversationId)
        {
            try
            {
                string watermark = null;

                while (true)
                {
                    var activitySet = await client.Conversations.GetActivitiesAsync(conversationId, watermark);
                    watermark = activitySet?.Watermark;

                    var activities = from x in activitySet.Activities
                                     where x.From.Id == botId
                                     select x;

                    foreach (Activity activity in activities)
                    {
                        if (!string.IsNullOrEmpty(activity.Text))
                        {
                            Console.WriteLine(activity.Text);
                        }

                        if (activity.Attachments != null)
                        {
                            foreach (Attachment attachment in activity.Attachments)
                            {
                                RenderAttachment(attachment);
                            }
                        }

                        Console.Write("Command> ");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void RenderAttachment(Attachment attachment)
        {
            switch (attachment.ContentType)
            {
                case "application/vnd.microsoft.card.hero":
                    RenderCard(JsonConvert.DeserializeObject<HeroCard>(attachment.Content.ToString()));
                    break;

                case "application/vnd.microsoft.card.thumbnail":
                    var thumbnail = JsonConvert.DeserializeObject<ThumbnailCard>(attachment.Content.ToString());
                    RenderCard(new HeroCard(thumbnail.Title, thumbnail.Subtitle, thumbnail.Text, thumbnail.Images, thumbnail.Buttons));
                    break;

                default:
                    if (attachment.ContentType != null && attachment.ContentType.StartsWith("image/"))
                    {
                        Console.WriteLine($"[image] {attachment.ContentUrl}");
                    }
                    else
                    {
                        Console.WriteLine($"[{attachment.ContentType}] {attachment.ContentUrl}");
                    }
                    break;
            }
        }

        private static void RenderCard(HeroCard card)
        {
            if (card == null)
            {
                return;
            }

            const int Width = 70;
            Func<string, string> centred = content =>
                string.Format($"{{0, -{Width}}}", string.Format("{0," + ((Width + content.Length) / 2) + "}", content));

            Console.WriteLine("/{0}", new string('*', Width + 1));
            if (card.Title != null) Console.WriteLine("*{0}*", centred(card.Title));
            if (card.Subtitle != null) Console.WriteLine("*{0}*", centred(card.Subtitle));
            Console.WriteLine("*{0}*", new string(' ', Width));
            if (card.Text != null) Console.WriteLine("*{0}*", centred(card.Text));
            if (card.Images != null && card.Images.Count > 0) Console.WriteLine("*{0}*", centred("[image] " + card.Images[0].Url));
            if (card.Buttons != null)
            {
                foreach (CardAction button in card.Buttons)
                {
                    string hint = button.Type == "openUrl" ? "open " + button.Value : "type: " + button.Value;
                    Console.WriteLine("*{0}*", centred("[" + button.Title + "] " + hint));
                }
            }
            Console.WriteLine("{0}/", new string('*', Width + 1));
        }
    }
}
