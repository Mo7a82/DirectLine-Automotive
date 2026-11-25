using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web;
using Newtonsoft.Json.Linq;

namespace DirectLineAutomotiveChatBot
{
    /// <summary>
    /// Serves the chat page. The Direct Line secret never leaves the server:
    /// on each page load it is exchanged for a conversation token, and only
    /// the token is written into the page.
    /// </summary>
    public partial class EJAutomotiveChatBot : System.Web.UI.Page
    {
        private const string TokenEndpoint = "https://directline.botframework.com/v3/directline/tokens/generate";

        /// <summary>Short-lived Direct Line token for the browser, or empty when it couldn't be created.</summary>
        protected string DirectLineToken { get; private set; }

        /// <summary>Bot handle, used to tell bot messages from the user's own.</summary>
        protected string BotId { get; private set; }

        /// <summary>Name shown in the chat header and the welcome message.</summary>
        protected string BotDisplayName { get; private set; }

        /// <summary>Why the token couldn't be created, shown to the user instead of the chat.</summary>
        protected string SetupError { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            string secret = ConfigurationManager.AppSettings["DirectLineSecret"];
            BotId = ConfigurationManager.AppSettings["BotId"] ?? string.Empty;
            BotDisplayName = ConfigurationManager.AppSettings["BotDisplayName"];
            if (string.IsNullOrWhiteSpace(BotDisplayName)) BotDisplayName = BotId;
            DirectLineToken = string.Empty;

            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(BotId))
            {
                SetupError = "The chat isn't configured yet. Set DirectLineSecret and BotId in DirectLine.secrets.config.";
                return;
            }

            try
            {
                DirectLineToken = GenerateToken(secret);
            }
            catch (WebException ex)
            {
                SetupError = "Couldn't connect to the bot service (" + ex.Message + ").";
            }
        }

        /// <summary>Encodes a value for use inside a JavaScript string literal in the page.</summary>
        protected static string Js(string value)
        {
            return HttpUtility.JavaScriptStringEncode(value ?? string.Empty);
        }

        private static string GenerateToken(string secret)
        {
            var request = (HttpWebRequest)WebRequest.Create(TokenEndpoint);
            request.Method = "POST";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + secret;
            request.ContentLength = 0;
            request.Timeout = 10000;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                var json = JObject.Parse(reader.ReadToEnd());
                return (string)json["token"];
            }
        }
    }
}
