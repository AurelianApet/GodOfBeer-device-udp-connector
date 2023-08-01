using System;
using Newtonsoft.Json.Linq;
using GodOfBeer.network;
using Quobject.SocketIoClientDotNet.Client;
using GodOfBeer.util;
using GodOfBeer.restful;
using SimpleJSON;
using System.Runtime.InteropServices;

namespace device
{
    class Program
    {
        public static int tagGWPort = 16000;
        public static int boardPort = 17000;
        public static int regulatorPort = 18000;
        public static UdpSessionManager udpSessionManager = ServerManager.Instance.udpSessionManager;
        public static bool is_socket_open = false;

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static void Main(string[] args)
        {
            ConfigSetting.api_prefix = @"/m-api/device/";
            string title = "UdpConnector";
            Console.Title = title;
            IntPtr hWnd = FindWindow(null, title);
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, 2); // minimize the winodw  
            }

            int serverType = 0;
            Console.WriteLine("-----Udp Connecter Exe-----");
            try
            {
                if (args.Length > 0)
                {
                    serverType = int.Parse(args[0]);
                    ConfigSetting.server_address = args[1];
                    if (serverType == 0)
                    {
                        Console.WriteLine("Connecter Type : TagGW Loading...");
                    }
                    else if (serverType == 1)
                    {
                        Console.WriteLine("Connecter Type : Board Loading...");
                    }
                    else if (serverType == 2)
                    {
                        Console.WriteLine("Connecter Type : Regulator Loading...");
                    }
                    Console.WriteLine("server_address : " + ConfigSetting.server_address);
                    ConfigSetting.api_server_domain = @"http://" + ConfigSetting.server_address + ":3006";
                    ConfigSetting.socketServerUrl = @"http://" + ConfigSetting.server_address + ":3006";
                    Console.WriteLine("api_url : " + ConfigSetting.api_server_domain);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception : " + ex);
            }

            Socket socket = IO.Socket(ConfigSetting.socketServerUrl);

            socket.On(Socket.EVENT_CONNECT, () =>
            {
                try
                {
                    Console.WriteLine("Setting Info Sended!");
                    if (is_socket_open)
                    {
                        return;
                    }
                    is_socket_open = true;
                    var UserInfo = new JObject();
                    socket.Emit("udpConnecterSetInfo", UserInfo);
                } catch(Exception ex)
                {
                    Console.WriteLine("Exception : " + ex);
                }
            });

            socket.On(Socket.EVENT_CONNECT_ERROR, (data) =>
            {
                try
                {
                    Console.WriteLine("Socket Connect failed.");
                    is_socket_open = false;
                    socket.Close();
                    socket = IO.Socket(ConfigSetting.socketServerUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception : " + ex);
                }
            });

            socket.On("udpConnecterSetSuccess", (data) =>
            {
                try
                {
                    Console.WriteLine("Socket Connected!");
                    ApiClient.Instance.UdpConnecterSuccessFunc();

                    if (serverType == 0)//TagGW
                    {
                        udpSessionManager.Start(tagGWPort);
                    }
                    else if (serverType == 1)//Board
                    {
                        udpSessionManager.Start(boardPort);
                    }
                    else if (serverType == 2) //regulator
                    {
                        udpSessionManager.Start(regulatorPort);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception : " + ex);
                }
            });

            socket.On("sendUdpResponse", (data) =>
            {
                try
                {
                    JSONNode jsonNode = SimpleJSON.JSON.Parse(data.ToString());
                    string ip = jsonNode["ip"];
                    string mac = jsonNode["mac"];
                    Console.WriteLine("Setting IP Address : " + ip);
                    Console.WriteLine("Setting Mac Address : " + mac);
                    udpSessionManager.Send_REQ_SET_DEVICE_INFO(ip, mac);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception : " + ex);
                }
            });
            Console.ReadLine();
        }
    }
}
