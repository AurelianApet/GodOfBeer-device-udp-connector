using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using GodOfBeer.util;
using GodOfBeer.restful;
using System.Linq;

namespace GodOfBeer.network
{
    public class UdpSessionManager
    {
        bool flag = false;
        UdpSession udpSession = null;
        AsyncCallback receiveCallback;
        int responseCnt = 0;
        byte[] dataArray = new byte[40];
        bool isDataExist = false;

        public bool Start(int port)
        {
            responseCnt = 0;
            flag = true;
            isDataExist = false;
            receiveCallback = new AsyncCallback(OnUdpReceive);
            udpSession = new UdpSession(port, receiveCallback);
            udpSession.Broadcast(port, (int)Opcode.REQ_GET_DEVICE_INFO, 0, 0, null);

            var a = NetUtils.GetDirectedBroadcastAddresses();
            Console.WriteLine("--------Broadcast Addresses----------");
            foreach(var aa in a)
            {
                Console.WriteLine(aa.ToString());
            }

            return true;
        }

        public void Stop()
        {
            flag = false;
            if (udpSession != null)
            {
                udpSession.Close();
                udpSession = null;
            }
            if (receiveCallback != null)
            {
                receiveCallback = null;
            }
        }

        public void OnUdpReceive(IAsyncResult result)
        {
            try
            {
                Console.WriteLine("[UDP][RECV]");
                if (!flag) return;
                UdpClient socket = result.AsyncState as UdpClient;

                IPEndPoint source = new IPEndPoint(0, 0);

                byte[] packet = socket.EndReceive(result, ref source);

                Console.WriteLine("[UDP][RECV] ip : " + source.Address.ToString());

                int length = NetUtils.ToInt32(packet, 0);
                int opcode = NetUtils.ToInt32(packet, 4);
                long reqid = NetUtils.ToInt64(packet, 8);
                long token = NetUtils.ToInt64(packet, 16);

                if (packet.Length == length)
                {
                    Console.WriteLine("[UDP][RECV] length : " + length);

                    byte[] body = null;
                    if (length > PacketInfo.HeaderSize)
                    {
                        body = new byte[length - PacketInfo.HeaderSize];
                        Array.Copy(packet, PacketInfo.HeaderSize, body, 0, length - PacketInfo.HeaderSize);
                    }

                    switch ((Opcode)opcode)
                    {
                        case Opcode.RES_GET_DEVICE_INFO:
                            Console.WriteLine("Receive(udp) Opcode : 0x" + opcode.ToString("X8") + " ResponseCnt : " + responseCnt);
                            //if (responseCnt > 0)
                            //{//세팅할 장비만 전원을 켜고 검색을 하세요.
                            //    ApiClient.Instance.SendAlertFunc(ConfigSetting.ShopId);
                            //    break;
                            //}
                            if(body != null && body.Length == 40)
                            {
                                dataArray = body;
                                isDataExist = true;
                                SendSettingResult();
                                responseCnt++;
                            }
                            else
                            {
                                Console.WriteLine("Body Wrong Length!!!");
                            }
                            break;
                        case Opcode.RES_SET_DEVICE_INFO:
                            Console.WriteLine("Receive(udp) Opcode : 0x" + opcode.ToString("X8"));
                            if (body != null && body.Length == 40)
                            {
                                //if(isDataExist && dataArray == body)
                                //{
                                    udpSession.Broadcast(udpSession.udp_port, (int)Opcode.REQ_SET_DEVICE_REBOOT, reqid, token, null);
                                //}
                                //else
                                //{
                                //    Console.WriteLine("Setting failed!!!");
                                //}
                            }
                            else
                            {
                                Console.WriteLine("Body Wrong Length!!!");
                            }
                            break;
                        case Opcode.RES_SET_DEVICE_REBOOT:
                            Console.WriteLine("Receive(udp) Opcode : 0x" + opcode.ToString("X8"));
                            if (body == null)
                            {//Success
                                ApiClient.Instance.UdpConnecterKillFunc();
                            }
                            else
                            {
                                Console.WriteLine("Body Wrong Length!!!");
                            }
                            break;
                        default:
                            Console.WriteLine("Receive(udp) Wrong Opcode : 0x" + opcode.ToString("X8"));
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Receive(udp) Wrong Length : Header Length("+length+")" +", Real Length("+packet.Length+")");
                }
                socket.BeginReceive(receiveCallback, socket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void Send_REQ_DEVICE_REBOOT()
        {
            udpSession.Broadcast(udpSession.udp_port, (int)Opcode.REQ_SET_DEVICE_REBOOT, 0, 0, null);
        }

        public string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "0.0.0.0";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        public void SendSettingResult()
        {
            if(isDataExist)
            {
                string id, ip, mac;
                uint[] idArray = new uint[3];
                string[] idStr = new string[3];
                for (int i = 0; i < 12; i += 4)
                {
                    idArray[i / 4] = NetUtils.ToUInt32(dataArray, i);
                }
                byte[] macAddress = new byte[6];
                string[] str_mac = new string[6];
                byte[] deviceAddress = new byte[4];
                //Global.tagGW_udpPort = NetUtils.ToInt32(data, 12); //UDP포구 고정
                Array.Copy(dataArray, 16, macAddress, 0, 6);
                Array.Copy(dataArray, 22, deviceAddress, 0, 4);
                //Array.Copy(dataArray, 26, netmask, 0, 4);
                //Array.Copy(dataArray, 30, gateway, 0, 4);
                //Array.Copy(dataArray, 34, serverTcpIpAddress, 0, 4);
                for(int i = 0; i < idArray.Length; i++)
                {
                    idStr[i] = string.Format("{0:X}", idArray[i]);
                }
                id = idStr[0] + "." + idStr[1] + "." + idStr[2];
                ip = deviceAddress[0] + "." + deviceAddress[1] + "." + deviceAddress[2] + "." + deviceAddress[3];
                for(int i = 0; i < macAddress.Length; i ++)
                {
                    str_mac[i] = string.Format("{0:X}", macAddress[i]);
                }
                mac = str_mac[0] + ":" + str_mac[1] + ":" + str_mac[2] + ":" + str_mac[3] + ":" + str_mac[4] + ":" + str_mac[5];
                Console.WriteLine("Device Id : " + id + ", IP : " + ip + ", MacAddress : " + mac);
                var res = ApiClient.Instance.SendSettingResultFunc(id, ip, mac);
            }
        }

        public void Send_REQ_SET_DEVICE_INFO(string ip, string mac)
        {
            try
            {
                string hexValues = "0123456789ABCDEF";
                if (isDataExist)
                {
                    int[] deviceAddress = new int[4];
                    int[] macAddress = new int[6];
                    string[] ipArray = ip.Split('.');
                    if (ipArray.Length != 4)
                    {
                        Console.WriteLine("IPAddress Format is not corret!");
                        return;
                    }
                    for (int i = 0; i < ipArray.Length; i++)
                    {
                        deviceAddress[i] = int.Parse(ipArray[i]);
                    }
                    string[] macArray = mac.Split(':');
                    if (macArray.Length != 6)
                    {
                        Console.WriteLine("MacAddress Format is not corret!");
                        return;
                    }

                    for (int i = 0; i < macArray.Length; i++)
                    {
                        int value = 0;
                        int index = 0;
                        for (int j = macArray[i].Length - 1; j >= 0; j--)
                        {
                            int decValue = hexValues.IndexOf(macArray[i][j]);
                            if (decValue != -1)
                            {
                                value = value + (decValue * (int)Math.Pow(16, index));
                            }
                            else
                            {
                                Console.WriteLine("Mac Address Format is not corret!");
                                return ;
                            }
                            index++;
                        }
                        macAddress[i] = value;
                    }

                    //mac address
                    for (int i = 16; i < 22; i++)
                    {
                        dataArray[i] = (byte)macAddress[i - 16];
                    }
                    //device ip
                    for (int i = 22; i < 26; i++)
                    {
                        dataArray[i] = (byte)deviceAddress[i - 22];
                    }
                    //netmask
                    for (int i = 26; i < 29; i++)
                    {
                        dataArray[i] = 255;
                    }
                    dataArray[29] = 0;
                    //gateway
                    for(int i = 30; i < 33; i ++)
                    {
                        dataArray[i] = (byte)deviceAddress[i - 30];
                    }
                    dataArray[33] = 1;
                    //SEVERIP
                    string ipadress = Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList.FirstOrDefault(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetwork)
                    .ToString();
                    string[] severIp = ipadress.Split('.');

                    if (severIp.Length != 4)
                    {
                        Console.WriteLine("ServerAddress Format is not corret!");
                        return;
                    }
                    int[] severAddress = new int[4];
                    for (int i = 0; i < severIp.Length; i++)
                    {
                        severAddress[i] = int.Parse(severIp[i]);
                    }
                    for (int i = 34; i < 38; i++)
                    {
                        dataArray[i] = (byte)severAddress[i - 34];
                    }
                    udpSession.Broadcast(udpSession.udp_port, (int)Opcode.REQ_SET_DEVICE_INFO, 0, 0, dataArray);
                    Console.WriteLine("Send REQ_SET_DEVICE_INFO Sended!");
                }
                else
                {
                    Console.WriteLine("Data is not exist for REQ_SET_DEVICE_INFO");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception : " + ex);
            }
        }
    }
}
