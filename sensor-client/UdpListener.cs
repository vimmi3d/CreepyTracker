using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class UdpListener
    {

        private UdpClient _udpClient = null;
        private IPEndPoint _anyIP;
        private int _port;
        public uint messageCount;
        int limit; // TMA: To keep track of the number of bytes sent.
        byte[] final_bytes; // TMA: To point to the bytes that will be send.
        public List<CloudMessage> PendingRequests;
        public List<TcpSender> Clients;

        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        public UdpListener(int port)
        {
            _port = port;
            PendingRequests = new List<CloudMessage>();
            Clients = new List<TcpSender>();
        }
        public void udpRestart()
        {
            if (_udpClient != null)
            {
                _udpClient.Close();
            }

            PendingRequests = new List<CloudMessage>();
            _anyIP = new IPEndPoint(IPAddress.Any, _port);
            _udpClient = new UdpClient(_anyIP);
            _udpClient.BeginReceive(new AsyncCallback(this.ReceiveCallback), null);

            Console.WriteLine("[UDPListener] Receiving in port: " + _port);
        }

        public void ReceiveCallback(IAsyncResult ar)
        {
            Console.WriteLine("[UDPListener] Received request: " + _port);
            try
            { 
                Byte[] receiveBytes = _udpClient.EndReceive(ar, ref _anyIP);
                string request = Encoding.ASCII.GetString(receiveBytes);
               
                string[] msg = request.Split(MessageSeparators.L0);
                if (msg[0] == "CloudMessage")
                {
                    PendingRequests.Add(new CloudMessage(msg[1]));
                }
                _udpClient.BeginReceive(new AsyncCallback(this.ReceiveCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on callback" + e.Message);
            }
            
        }

        public void processRequests(byte[] depth,byte[] color,int compressed,bool compression,int iscale)
        {
            List<CloudMessage> todelete = new List<CloudMessage>();
            List<TcpSender> todeleteSenders = null;
            for (int i = 0; i < PendingRequests.Count; i++)
            {
                CloudMessage cm = PendingRequests[i];
                //Stop
                if (cm.mode == 2)
                {
                    foreach (CloudMessage cm2 in PendingRequests)
                    {
                        if (cm.replyIPAddress.ToString() == cm2.replyIPAddress.ToString() &&
                            cm.port == cm2.port)
                            todelete.Add(cm2);
                    }
                    foreach (TcpSender c in Clients)
                    {
                        if (c._address == cm.replyIPAddress.ToString())
                        {
                            if (todeleteSenders == null) todeleteSenders = new List<TcpSender>();
                            todeleteSenders.Add(c);

                        }
                    }
                    if (todeleteSenders != null)
                    {
                        foreach (TcpSender c in todeleteSenders)
                        {
                            Clients.Remove(c);
                            c.close();
                        }
                    }

                    continue;
                }
                //Failsafe
                if (todelete.Contains(cm))
                {
                    continue;
                }

                if (cm.mode == 1)
                {
                    TcpSender newclient = new TcpSender();
                    newclient.connect(cm.replyIPAddress.ToString(), cm.port);
                    Clients.Add(newclient);
                    todelete.Add(cm);
                }

                if (cm.mode == 0)
                {
                    TcpSender newclient = new TcpSender();
                    newclient.connect(cm.replyIPAddress.ToString(), cm.port);
                    System.Threading.Thread.Sleep(500);
                    Console.WriteLine("Sent data");
                    newclient.sendData(depth, messageCount,compressed, compression,iscale);
                    newclient.sendData(color, messageCount,color.Length, compression,iscale);
                    //newclient.close();
                    todelete.Add(cm);
                }
            }
            foreach (CloudMessage cm in todelete)
            {
                PendingRequests.Remove(cm);
            };
        }

        public void OnApplicationQuit()
        {
            if (_udpClient != null) _udpClient.Close();
        }
    }
}