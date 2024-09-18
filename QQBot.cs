using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lagrange.Core.Common.Interface.Api;
using System.Drawing;
using System.Windows.Forms;
using Lagrange.Core;
using System.ComponentModel.Design.Serialization;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

namespace QQBot
{
    internal class QQBot
    {
        public uint GroupId { get; set; }

        private readonly BotDeviceInfo _deviceInfo;
        private BotContext bot;
        private bool _isLogin = false;
        private SparkSession _currentSession;
        public bool AutoLogin = false;

        private string _appid;
        private string _apiSecret;
        private string _apiKey;

        public QQBot(uint groupId, string appId, string apiSecret, string apiKey, bool autoLogin = false)
        {
            GroupId = groupId;
            _appid = appId;
            _apiSecret = apiSecret;
            _apiKey = apiKey;
            _deviceInfo = new BotDeviceInfo()
            {
                Guid = Guid.NewGuid(),
                MacAddress = [0x52, 0x40, 0x32, 0x44, 0x46, 0x30],
                DeviceName = $"Lagrange-52D02F",
                SystemKernel = "Windows 10.0.19042",
                KernelVersion = "10.0.19042.0"
            };
            AutoLogin = autoLogin;
            var _keyStore = LoadKeyStore();
            bot = BotFactory.Create(new BotConfig(), _deviceInfo, _keyStore);
            Init();
        }

        private void Init()
        {
            bot.Invoker.OnBotOnlineEvent += (context, @event) =>
            {
                SaveKeyStore(bot.UpdateKeystore());
                Console.WriteLine("Bot online! keystore saved.");
            };
            bot.Invoker.OnGroupMessageReceived += (context, @event) =>
            {
                //如果收到的不是当前机器人的群聊消息，则直接返回
                if (@event.Chain.GroupUin != GroupId)
                    return;

                bool isAtMe = false;
                foreach (var entity in @event.Chain)
                {
                    Console.WriteLine(entity.ToPreviewString());
                    if (entity is TextEntity textEntity) {

                        if (isAtMe == true)
                        {
                            Task.Run(() => { 
                                ProcessCommand(textEntity.Text);
                            });
                            isAtMe = false;
                        }
                    }
                    if (entity is MentionEntity mentionEntity)
                    {
                        if (mentionEntity.Uin == bot.BotUin)
                            isAtMe = true;
                    }

                }
            };
        }
        private void CreatSession(string prompt)
        {
            _currentSession = new SparkSession(_appid, _apiSecret, _apiKey, SparkVersions.general, prompt);
        }
        private void ProcessCommand(string command)
        {
            string[] args = command.Trim().Split(' ');
            string answer = "请输入正确的指令，可输入 /help 查看可用指令。";
            CommandProcessor processor = null;
            switch (args[0])
            {
                case "/help":
                    processor = new Help();
                    break;
                case "/ask":
                    if (_currentSession == null)
                    {
                        CreatSession("");
                    }
                    processor = new Ask();
                    break;
                case "/new":
                    if (args.Length >= 2)
                    {
                        string prompt = args[1];
                        CreatSession(prompt);
                        answer = "会话创建成功";
                    }
                    break;
                case "/history":
                    if (_currentSession!= null)
                        processor = new History();
                    else
                        answer = "当前没有会话";
                    break;
            }
            if (processor != null)
                answer = processor.Process(_currentSession, args);
            SendMessage(answer);
        }

        private byte[] GenRandomBytes(int v)
        {
            byte[] b = new byte[v];
            for (int i = 0; i < v; i++)
            {
                b[i] = (byte)new Random().Next(0, 256);
            }
            return b;
        }

        public async Task Login(Action<byte[]> ShowQrCode)
        {
            if (!AutoLogin)
            {
                await LoginByQrCode(ShowQrCode);
            }
            else
            {
                await LoginByPassword();
            }
        }

        public async Task LoginByQrCode(Action<byte[]> ShowQrCode)
        {
            // 显示二维码
            Console.WriteLine("Fetching QR code...");
            var qrCode = await bot.FetchQrCode();
            Console.WriteLine("Please scan the QR code to login.");
            ShowQrCode(qrCode.Value.QrCode);
            await bot.LoginByQrCode();
            Console.WriteLine("Login success!");
            _isLogin = true;
        }

        public async Task LoginByPassword()
        {
            await bot.LoginByPassword();
            Console.WriteLine("Login success!");
            _isLogin = true;
        }

        public void SendMessage(string message)
        {
            if (_isLogin == false)
            {
                Console.WriteLine("Please login first.");
                return;
            }
            var messageChain = MessageBuilder.Group(GroupId).Text(message).Build();
            Task.Run(async () =>
            {
                var result = await bot.SendMessage(messageChain);
                Console.WriteLine(result.ToString());
            });

        }

        private BotKeystore LoadKeyStore()
        {
            // 从文件反序列化对象
            if (File.Exists("keystore.json"))
            {
                string json = File.ReadAllText("keystore.json");
                return JsonConvert.DeserializeObject<BotKeystore>(json);
            }
            return new BotKeystore();
        }

        private void SaveKeyStore(BotKeystore keystore)
        {
            // 序列化对象到文件
            string json = JsonConvert.SerializeObject(keystore);
            File.WriteAllText("keystore.json", json);
        }
    }
}

