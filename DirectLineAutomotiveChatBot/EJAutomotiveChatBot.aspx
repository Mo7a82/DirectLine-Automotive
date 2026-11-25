<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EJAutomotiveChatBot.aspx.cs" Inherits="DirectLineAutomotiveChatBot.EJAutomotiveChatBot" %>

<!DOCTYPE html>

<html lang="en">
<head>
    <meta charset="UTF-8" />
    <title>Automotive ChatBot</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <link href="/styles/botchat.css" rel="stylesheet" />
    <link href="/styles/botchat-fullwindow.css" rel="stylesheet" />
    <style>
        html, body {
            height: 100%;
            margin: 0;
            padding: 0;
            overflow: hidden;
            font-family: "Segoe UI", sans-serif;
        }

        #chat-header {
            position: absolute;
            top: 0;
            right: 0;
            left: 0;
            height: 48px;
            padding: 0 16px;
            line-height: 48px;
            font-size: 18px;
            color: #fff;
            background-color: #3e92d2;
        }

        #bot {
            position: absolute;
            top: 48px;
            right: 0;
            bottom: 0;
            left: 0;
        }

        .setup-error {
            max-width: 520px;
            margin: 80px auto;
            padding: 16px 20px;
            border-left: 4px solid #c50f1f;
            background: #fdf3f4;
            color: #333;
        }
    </style>
</head>
<body>
    <% if (!string.IsNullOrEmpty(SetupError)) { %>
        <div class="setup-error"><%: SetupError %></div>
    <% } else { %>
        <div id="chat-header"><%: BotDisplayName %></div>
        <div id="bot"></div>

        <script src="/scripts/botchat.js"></script>
        <script>
            (function () {
                var settings = {
                    token: "<%= Js(DirectLineToken) %>",
                    botId: "<%= Js(BotId) %>",
                    botName: "<%= Js(BotDisplayName) %>",
                    highlightColor: "#3e92d2"
                };

                // Optional header colour, for example ?bothighlightcolor=%23c00000
                var match = /[?&]bothighlightcolor=([^&]+)/i.exec(window.location.search);
                if (match) {
                    var colour = decodeURIComponent(match[1]);
                    if (/^#[0-9a-f]{3,8}$/i.test(colour)) {
                        settings.highlightColor = colour;
                    }
                }

                var style = document.createElement("style");
                style.textContent =
                    "#chat-header, .wc-message-from-me .wc-message-content { background-color: " + settings.highlightColor + " !important; color: #fff !important; }" +
                    ".wc-message-from-me svg.wc-message-callout path { fill: " + settings.highlightColor + " !important; }" +
                    ".wc-card button { color: " + settings.highlightColor + " !important; }";
                document.head.appendChild(style);

                // A random id per visit, so separate visitors never share a conversation.
                var userId = "web-" + Math.random().toString(36).slice(2, 10);

                // The token comes from the server and is refreshed by Web Chat while the page is open.
                var directLine = new BotChat.DirectLine({ token: settings.token });

                BotChat.App({
                    botConnection: directLine,
                    user: { id: userId, name: "You" },
                    bot: { id: settings.botId, name: settings.botName },
                    formatOptions: { showHeader: false },
                    resize: "detect"
                }, document.getElementById("bot"));

                // Say hello so the bot opens with its welcome cards. Wait until Web Chat
                // has started the conversation, so the greeting goes into the same one.
                var attempts = 0;
                var waitForConversation = setInterval(function () {
                    attempts++;
                    if (directLine.conversationId) {
                        clearInterval(waitForConversation);
                        directLine.postActivity({
                            type: "message",
                            from: { id: userId, name: "You" },
                            text: "hi"
                        }).subscribe(function () { }, function (error) { console.error(error); });
                    } else if (attempts > 50) {
                        clearInterval(waitForConversation);
                    }
                }, 200);
            })();
        </script>
    <% } %>
</body>
</html>
