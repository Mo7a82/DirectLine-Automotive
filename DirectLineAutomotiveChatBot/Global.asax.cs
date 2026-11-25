using System;
using System.Net;

namespace DirectLineAutomotiveChatBot
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // The Bot Framework endpoints accept TLS 1.2 only. .NET Framework 4.6.1
            // doesn't always offer it by default, so turn it on explicitly.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }
    }
}
