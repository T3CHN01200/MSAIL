using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;

/*
 * Authored by: T3CHN01200
 * Discord: T3CHN01200#7178
 * Last updated: 2019/7/7
 */

namespace MSAIL
{

    public class Bot
    {

        readonly string token = null;
        DiscordSocketClient _client;
        IMessageComparer c = new IMessageComparer();

        public Bot()
        {

            List<string> environmentVariables = null;

            if (!File.Exists(".env"))
                File.Create(".env");

            while (environmentVariables == null)
            {

                using (StreamReader sr = new StreamReader(File.OpenRead(".env")))
                {

                    environmentVariables = sr.ReadToEnd().Split("\n").ToList();

                }

            }

            token = (from e in environmentVariables
                    where e.StartsWith("API_KEY=")
                    select e).ToList()[0].Split("=")[1];

            Constructor().GetAwaiter().GetResult();

        }

        private async Task Constructor()
        {

            _client = new DiscordSocketClient();

            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.Ready += async () =>
            {
                foreach (SocketGuild g in _client.Guilds)
                    await DownloadMessageHistory(g);
            };
            _client.JoinedGuild += DownloadMessageHistory;
            _client.MessageReceived += MessageRecieved;
            _client.MessageUpdated += LogMessageUpdate;

            await Task.Delay(-1);

        }

        private async Task MessageRecieved(SocketMessage m)
        {

            await LogNewMessage(m);
            await Pull(m);

        }

        private async Task Pull(SocketMessage m)
        {

            SocketGuildChannel c = (SocketGuildChannel)m.Channel;
            SocketGuild g = c.Guild;
            string path = @"" + g.Id;

            if (((SocketGuildUser)m.Author).Roles.ToList().
                FindAll((obj) => obj.Permissions.Administrator).Count > 0)
                if (m.Content.ToLower().StartsWith("!pull"))
                {

                    if (m.MentionedChannels.Count > 0)
                    {
                        Console.WriteLine(1);
                        Parallel.ForEach(m.MentionedChannels, (mc) =>
                        {

                            m.Author.SendFileAsync($"{path}/{mc.Id}.csv");

                        });
                    }
                    else if (m.Content.ToLower().Contains("!pull all"))
                    {
                        Console.WriteLine(2);
                        if (File.Exists($"{g.Id}.zip"))
                            File.Delete($"{g.Id}.zip");

                        ZipFile.CreateFromDirectory(path, $"{g.Id}.zip");
                        await m.Author.SendFileAsync($"{g.Id}.zip");
                        File.Delete($"{g.Id}.zip");

                    }
                    else
                    {
                        Console.WriteLine(1);
                        await m.Author.SendFileAsync($"{path}/{c.Id}.csv");
                    }

                }

        }

        private async Task LogNewMessage(SocketMessage m)
        {

            string path = @"" + ((SocketGuildChannel)m.Channel).Guild.Id +
                "/" + m.Channel.Id + ".csv";

            if (!Directory.Exists(@"" + ((SocketGuildChannel)m.Channel).Guild.Id +
                "/"))
                File.Create(@"" + ((SocketGuildChannel)m.Channel).Guild.Id +
                "/");

            if (!File.Exists(path))
                File.Create(path);

            string attachmentList = "";

            foreach (IAttachment a in m.Attachments)
                attachmentList += $"{a.Url},";

            attachmentList.TrimEnd(',');

            string embedList = "";

            foreach (IEmbed e in m.Embeds)
                embedList += $"{e.Url},";

            embedList.TrimEnd(',');

            using (StreamWriter sw = new StreamWriter(File.Open(path, FileMode.Append)))
            {

                await sw.WriteLineAsync($"{m.Id},{m.Author.Id}," +
                    $"\"{m.Author.Username.Replace("\"", "\'")}\",\"{m.Content.Replace("\"", "\'")}\",\"{attachmentList}\",\"{embedList}\"," +
                        $"{m.Timestamp}");

            }

        }

        private async Task LogMessageUpdate(Cacheable<IMessage, ulong> cacheable,
            SocketMessage m, ISocketMessageChannel c)
        {

            string path = @"" + ((SocketGuildChannel) m.Channel).Guild.Id + "/" +
                m.Channel.Id + ".csv";

            if (!File.Exists(path))
                File.Create(path);

            string attachmentList = "";

            foreach (IAttachment a in m.Attachments)
                attachmentList += $"{a.Url},";

            attachmentList.TrimEnd(',');

            string embedList = "";

            foreach (IEmbed e in m.Embeds)
                embedList += $"{e.Url},";

            embedList.TrimEnd(',');

            using (StreamWriter sw = new StreamWriter(File.Open(path, FileMode.Append)))
            {

                await sw.WriteLineAsync($"{m.Id},{m.Author.Id}," +
                    $"\"{m.Author.Username.Replace("\"", "\'")}\",\"{m.Content.Replace("\"", "\'")}\",\"{attachmentList}\",\"{embedList}\"," +
                        $"{m.EditedTimestamp}");

            }

            List<string> messages = new List<string>();

            using (StreamReader sr = new StreamReader(File.OpenRead(path)))
            {

                string text = await sr.ReadToEndAsync();
                messages = text.Split("\n").ToList();

            }

            messages.Sort((s1, s2) => {
                if (s1 == null)
                {

                    if (s2 == null)
                        return 0;
                    else
                        return -1;

                }
                else
                {

                    if (s2 == null)
                        return 1;
                    else
                    {

                        if (ulong.Parse(s1.Split(",")[0]).
                            CompareTo(ulong.Parse(s2.Split(",")[0])) == 0)
                            return DateTimeOffset.Parse(
                                s1.Split(",")[s1.Split(",").Length - 1]).
                                CompareTo(DateTimeOffset.Parse(
                                s2.Split(",")[s2.Split(",").Length - 1]));
                        else
                            return ulong.Parse(s1.Split(",")[0]).
                                CompareTo(ulong.Parse(s2.Split(",")[0]));

                    }

                }

            });

        }

        private async Task DownloadMessageHistory(SocketGuild g)
        {

            string path = @"" + g.Id + "/";

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            foreach (SocketTextChannel c in g.Channels)
            {

                if (!File.Exists($"{path}{c.Id}.csv"))
                    File.Create($"{path}{c.Id}.csv");

                ulong latestMessageId;

                using (StreamReader sr = new StreamReader(
                    File.OpenRead($"{path}{c.Id}.csv")))
                {

                    string text = await sr.ReadToEndAsync();
                    if (text.Length > 0)
                        latestMessageId = ulong.Parse(
                            text.Split("\n")[text.Split("\n").Length - 1].Split(",")[0]);
                    else
                        latestMessageId = 0;

                }

                IAsyncEnumerable<IReadOnlyCollection<IMessage>> messages =
                    c.GetMessagesAsync(int.MaxValue);

                using (StreamWriter sw = new StreamWriter(
                    File.Open($"{path}{c.Id}.csv", FileMode.Append)))
                {

                    foreach (IReadOnlyCollection<IMessage> i in messages.ToEnumerable())
                    {
                        List<IMessage> messageList = i.ToList();
                        messageList.Reverse();
                        foreach (IMessage m in messageList)
                        {

                            string attachmentList = "";

                            foreach (IAttachment a in m.Attachments)
                                attachmentList += $"{a.Url},";

                            attachmentList.TrimEnd(',');

                            string embedList = "";

                            foreach (IEmbed e in m.Embeds)
                                embedList += $"{e.Url},";

                            embedList.TrimEnd(',');

                            if (m.Id > latestMessageId)
                                await sw.WriteLineAsync($"{m.Id},{m.Author.Id}," +
                                    $"\"{m.Author.Username.Replace("\"", "\'")}\",\"{m.Content.Replace("\"", "\'")}\",\"{attachmentList}\",\"{embedList}\"," +
                                    $"{m.Timestamp}");

                        }

                    }

                }

            }

        }

        private Task Log(LogMessage msg)
        {

            Console.WriteLine($"{msg}");
            return Task.CompletedTask;

        }

    }

    class IMessageComparer : IComparer<IMessage>
    {

        public int Compare(IMessage a, IMessage b)
        {

            if (a == null)
            {

                if (b == null)
                    return 0;
                else
                    return -1;

            }
            else
            {

                if (b == null)
                    return 1;
                else
                {

                    int output = a.Id.CompareTo(b.Id);

                    if (output != 0)
                        return output;
                    else
                    {

                        if (!a.EditedTimestamp.HasValue)
                            return -1;
                        else
                            return 1;

                    }

                }

            }

        }

    }

}
