using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using IniParser;
using IniParser.Model;

namespace PCsurveillance
{

    class Program
    {
        //Work only in Windows
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;


        private static string token = null;
        private static string authcode = null;
        private static List<String> list = new List<String>();
        private static List<String> list2 = new List<String>();
        public static List<String> Authorized = new List<String>();


        static void Main(string[] args)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile("conf.ini");
            token = data["General"]["token"];
            authcode = data["General"]["authcode"];
            Authorized.Add(data["General"]["admin"]);
            TelegramBotClient Bot = new TelegramBotClient(token);

            if(data["General"]["hide"] == "true")
            {
                var handle = GetConsoleWindow();

                ShowWindow(handle, SW_HIDE);
            }
            Log(Lang("gen.start"));
            String date = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            SendAll("[" + date + "] " + Lang("gen.logon"));
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            Bot.OnMessage += Bot_OnMessageAsync;
            var me = Bot.GetMeAsync().Result;
            Bot.StartReceiving();
            Console.ReadLine();
        }

        static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                SendAll(Lang("gen.lock"));
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                SendAll(Lang("gen.on"));

            }
            else if(e.Reason == SessionSwitchReason.SessionLogoff)
            {
                SendAll(Lang("gen.off"));
            }
        }

        private static void Bot_OnReceiveError(object sender, Telegram.Bot.Args.ReceiveErrorEventArgs e)
        {
            Debugger.Break();
        }

        public static void Bot_OnMessageAsync(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var message = e.Message;

            if (message == null || message.Type != MessageType.TextMessage) return;
            
            if(message.Text.StartsWith(Lang("com.help")))
            {
                Send(message.Chat.Id, Lang("gen.help"));
            }

            if (message.Text.StartsWith("m"))
            {
                var rkm = new ReplyKeyboardMarkup();

                rkm.Keyboard =
                    new KeyboardButton[][]
                    {
        new KeyboardButton[]
        {
            new KeyboardButton(Lang("com.auth")),
            new KeyboardButton(Lang("com.manage")),
            new KeyboardButton(Lang("com.command"))
        },

        new KeyboardButton[]
        {
            new KeyboardButton(Lang("com.info")), new KeyboardButton(Lang("com.help"))
        }
                    };

                Send(message.Chat.Id, Lang("gen.help"), 1,rkm);
            }

            if (message.Text.StartsWith(Lang("com.auth")))
            {
                        Log(message.Chat.Id.ToString());
                        list.Add(message.Chat.Id.ToString());
                        list2.Add("Auth");

                        Log(Lang("log.auth"));
                        Send(message.Chat.Id, Lang("gen.password"));
            }

            if (message.Text.StartsWith(Lang("com.manage")))
            {
                for (int i = 0; i < Authorized.Count; i++)
                {
                    if (message.Chat.Id.ToString() == Authorized[i])
                    {
                        list.Add(message.Chat.Id.ToString());
                        list2.Add("Manage");

                        Log(Lang("log.manage"));

                        var rkm = new ReplyKeyboardMarkup();

                rkm.Keyboard =
                    new KeyboardButton[][]
                    {
        new KeyboardButton[]
        {
            new KeyboardButton(Lang("com.lock")),
            new KeyboardButton(Lang("com.off")),
            new KeyboardButton(Lang("com.canoff"))
        }
                    };

                Send(message.Chat.Id, Lang("gen.command1"), 1,rkm);
            }
                }
            }

            if (message.Text.StartsWith(Lang("com.command")))
            {
                for (int i = 0; i < Authorized.Count; i++)
                {
                    if (message.Chat.Id.ToString() == Authorized[i])
                    {
                        list.Add(message.Chat.Id.ToString());
                        list2.Add("Excute");

                        Log(Lang("log.command"));
                        Send(message.Chat.Id, Lang("gen.command2"));
                    }
                }
            }

            if (message.Text.StartsWith(Lang("com.info")))
            {
                for (int i = 0; i < Authorized.Count; i++)
                {
                    if (message.Chat.Id.ToString() == Authorized[i])
                    {
                        string externalip = new WebClient().DownloadString("https://ipinfo.io/ip");
                        Send(message.Chat.Id, Lang("gen.status") + "\n" + Lang("gen.externalip") + " " +externalip + Lang("gen.localip") + " " + GetLocalIPAddress() + "\n" + Lang("gen.os") + " " + Environment.OSVersion + "\nCPU: " + GetComponent("Win32_Processor", "Name"));
                    }
                }
            }

            if (message.Text.StartsWith(Lang("com.check")))
            {
                for (int i = 0; i < Authorized.Count; i++)
                {
                    if (message.Chat.Id.ToString() == Authorized[i])
                    {
                        Send(message.Chat.Id, "인증받은 사용자임");
                    }
                }
            }

            if(message.Text.StartsWith(Lang("com.cancel")))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list2[i] == "Auth" || list2[i] == "Manage" || list2[i] == "Excute")
                    {
                        list.RemoveAt(i);
                        list2.RemoveAt(i);
                        Send(message.Chat.Id, Lang("gen.cancel"));
                    }
                }
            }

            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list2[i] == "Auth")
                    {
                        if (message.Text == authcode)
                        {
                            Log(Lang("gen.authsuc"));
                            Authorized.Add(message.Chat.Id.ToString());
                            Send(message.Chat.Id, Lang("gen.authsuc"));
                            list.RemoveAt(i);
                            list2.RemoveAt(i);
                        }
                        else if (message.Text == Lang("com.auth"))
                        {
                            break;
                        }
                        else
                        {
                            Log(Lang("gen.authfail"));
                            Send(message.Chat.Id, Lang("gen.authfail"));
                            list.RemoveAt(i);
                            list2.RemoveAt(i);
                            break;
                        }
                    }

                    if (list2[i] == "Manage")
                    {
                        if (message.Text == Lang("com.lock"))
                        {
                            LockWorkStation();
                            Send(message.Chat.Id, Lang("gen.excute"));
                        }

                        if (message.Text == Lang("com.off"))
                        {
                            Process.Start("shutdown", "-s -f -t 10");
                            Send(message.Chat.Id, Lang("gen.excute"));
                        }

                        if (message.Text == Lang("com.canoff"))
                        {
                            Process.Start("shutdown", "-a");
                            Send(message.Chat.Id, Lang("gen.excute"));
                        }

                    }


                    if (list2[i] == "Excute")
                    {
                        string[] splited = message.Text.Split(';');

                        if(splited.Length == 2)
                        {
                            ProcessStartInfo proInfo = new ProcessStartInfo();
                            Process pro = new Process();

                            proInfo.FileName = @"cmd";
                            proInfo.CreateNoWindow = true;
                            proInfo.UseShellExecute = false;
                            proInfo.RedirectStandardOutput = true;
                            proInfo.RedirectStandardInput = true;
                            proInfo.RedirectStandardError = true;

                            pro.StartInfo = proInfo;
                            pro.Start();

                            pro.StandardInput.Write(splited[0] + Environment.NewLine);
                            pro.StandardInput.Close();
                            Log(splited[0]);
                            string resultValue = pro.StandardOutput.ReadToEnd();
                            pro.WaitForExit();
                            pro.Close();

                            list.RemoveAt(i);
                            list2.RemoveAt(i);

                            Send(message.Chat.Id, resultValue);
                        }

                        if (message.Text == Lang("com.command"))
                        {
                            break;
                        }
                    }
                }
            }
        }

        public static async void Send(long id, String Message, int log = 1, ReplyKeyboardMarkup rkm = null)
        {
            TelegramBotClient Bot = new TelegramBotClient(token);

            if (log == 1)
            {              
                Log(Message + " | " + Lang("gen.send"));
                await Bot.SendTextMessageAsync(id, Message, ParseMode.Default, false, false, 0, rkm);
            }
            else
            {
                await Bot.SendTextMessageAsync(id, Message, ParseMode.Default, false, false, 0, rkm);
            }
        }

        public static void Log(String Log)
        {
            String date = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            Console.WriteLine("["+date + "] " + Log);
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        public static string GetComponent(String HWclass, String Syntax)
        {
            string info = null;

            ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM " + HWclass);
            foreach (ManagementObject mj in mos.Get())
            {
                info = mj[Syntax].ToString();
            }
            return info;
        }

        public static void SendAll(string Message)
        {
            for (int i = 0; i < Authorized.Count; i++)
            {
                Send(Convert.ToInt64(Authorized[i]), Message);
            }
        }

        public static string Lang(string id)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile("conf.ini");
            string lang = data["General"]["lang"];

            return data["Lang"][id + "." + lang];
        }


        [DllImport("user32")]
        public static extern void LockWorkStation();

    }
}
