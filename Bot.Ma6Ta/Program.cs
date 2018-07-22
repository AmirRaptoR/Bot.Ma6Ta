using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Bot.Ma6Ta
{

    public class UserData
    {
        public int MessageCount { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
    }

    class Program
    {
        private static Dictionary<int, UserData> _messagesCount;
        private static Telegram.Bot.TelegramBotClient botClient;
        static void Main(string[] args)
        {
            Load();
            MainAsync().GetAwaiter().GetResult();
            Console.WriteLine("Bot is up and running");
            Console.ReadLine();

            Thread resetTimer = new Thread(() =>
            {
                while (true)
                {
                    if (DateTime.Now.Hour == 4 && DateTime.Now.Minute == 0)
                    {
                        foreach (var key in _messagesCount.Keys)
                        {
                            _messagesCount[key].MessageCount = 0;
                        }
                    }
                    Thread.Sleep(60000);
                }

            })
            {
                IsBackground = true
            };
            resetTimer.Start();
        }

        private static readonly string DataFile = "data.dat";

        private static void Save()
        {
            if (System.IO.File.Exists(DataFile))
            {
                System.IO.File.Delete(DataFile);
            }

            using (var sw = new StreamWriter(DataFile))
            {
                foreach (var key in _messagesCount.Keys)
                {
                    sw.WriteLine(string.Join("\t", key, _messagesCount[key].MessageCount,
                         _messagesCount[key].UserName, _messagesCount[key].Name));
                }
            }
        }

        private static void Load()
        {
            if (!System.IO.File.Exists(DataFile))
            {
                _messagesCount = new Dictionary<int, UserData>();
                return;
            }

            var lines = System.IO.File.ReadAllLines(DataFile);
            _messagesCount = new Dictionary<int, UserData>();
            foreach (var line in lines)
            {
                _messagesCount[int.Parse(line.Split('\t')[0])] =
                    new UserData()
                    {
                        MessageCount = int.Parse(line.Split('\t')[1]),
                        UserName = line.Split('\t')[2],
                        Name = line.Split('\t')[3],
                    }
                     ;

            }
        }

        private static async Task MainAsync()
        {
            botClient = new Telegram.Bot.TelegramBotClient(Properties.Settings.Default.BotAccessToken);

            var me = await botClient.GetMeAsync();
            botClient.OnMessage += MessageReceived;
            botClient.StartReceiving(new[]{
            UpdateType.Message });

        }

        private static async void MessageReceived(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.Message.Text.Contains("/report"))
                {
                    var result = "";
                    foreach (var key in _messagesCount.Keys)
                    {
                        result += $"[{_messagesCount[key].Name}](tg://user?id={key}) - {_messagesCount[key].MessageCount} پیام\r\n";
                    }
                    if (result.Length > 2)
                        result = result.Substring(0, result.Length - 2);
                    else
                        result = "حافظم خالیه";

                    await botClient.SendTextMessageAsync(e.Message.Chat.Id,
                        result, replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Markdown);

                    return;
                }


                if (!_messagesCount.ContainsKey(e.Message.From.Id))
                {
                    _messagesCount[e.Message.From.Id] = new UserData()
                    {
                        MessageCount = 0,
                    };
                }
                _messagesCount[e.Message.From.Id].MessageCount++;
                _messagesCount[e.Message.From.Id].Name = e.Message.From.FirstName + " " + e.Message.From.LastName;
                _messagesCount[e.Message.From.Id].UserName = e.Message.From.Username;

                if (_messagesCount[e.Message.From.Id].MessageCount >= Properties.Settings.Default.MessagePerDay)
                {
                    if ((_messagesCount[e.Message.From.Id].MessageCount - Properties.Settings.Default.MessagePerDay) % 5 == 0)
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"{e.Message.From.FirstName} چقدر حرف می‌زنی. [مریم](tg://user?id=) ناراحت می‌شه. از صبح {_messagesCount[e.Message.From.Id].MessageCount} تا پیام فرستادی",
                            replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Markdown);
                    Console.WriteLine($"{e.Message.From.Username ?? e.Message.From.FirstName} has spoken beyond his/her limit({_messagesCount[e.Message.From.Id].MessageCount})");
                }

            }
            catch
            {

            }
            Save();
        }
    }
}
