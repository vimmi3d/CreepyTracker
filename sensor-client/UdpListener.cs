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

        public void processRequests(List<byte> byte_list)
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
                        if(c._address == cm.replyIPAddress.ToString())
                        {
                            if (todeleteSenders == null) todeleteSenders = new List<TcpSender>();
                            todeleteSenders.Add(c);
                        }
                    }
                    if(todeleteSenders != null)
                    {
                        foreach (TcpSender c in todeleteSenders) Clients.Remove(c);
                    }

                    continue;
                }
                //Failsafe
                if (todelete.Contains(cm))
                {
                    continue;
                }
                
               if(cm.mode == 1)
                {
                    TcpSender newclient = new TcpSender();
                    newclient.connect(cm.replyIPAddress.ToString(), cm.port);
                    Clients.Add(newclient);
                    todelete.Add(cm);
                }

                if(cm.mode == 0) {
                    // TMA: Get the bytes from the ArrayList
                    byte[] points_bytes = byte_list.ToArray();
                    // This is the heading for every package.
                    string msg = "CloudMessage" + MessageSeparators.L0 + Environment.MachineName + MessageSeparators.L1 + messageCount + MessageSeparators.L1; // String to tag the sensor
                                                                                                                                                               // Get the heading bytes.
                    int remainder = 4 - (msg.Length % 4);
                    while (remainder-- > 0) msg = 'C' + msg;

                    byte[] msg_bytes = Encoding.ASCII.GetBytes(msg); // Convert to bytes
                    IPEndPoint ep = new IPEndPoint(cm.replyIPAddress, cm.port);
                    for (limit = 0; limit < points_bytes.Length; limit += 8000) // Each packet has 500 points (16 * 500 = 8000 bytes)
                    {
                        if (limit + 8000 > points_bytes.Length) // If there are less points than 500
                        {
                            final_bytes = new byte[msg_bytes.Length + points_bytes.Length - limit];
                            Array.Copy(msg_bytes, 0, final_bytes, 0, msg_bytes.Length);
                            Array.Copy(points_bytes, limit, final_bytes, msg_bytes.Length, points_bytes.Length - limit);
                        }
                        else // If there are more or 500 points to send
                        {
                            final_bytes = new byte[msg_bytes.Length + 8000];
                            Array.Copy(msg_bytes, 0, final_bytes, 0, msg_bytes.Length);
                            Array.Copy(points_bytes, limit, final_bytes, msg_bytes.Length, 8000);
                        }
                        
                        try
                        {
                            System.Threading.Thread.Sleep(10);
                            _udpClient.Send(final_bytes, final_bytes.Length, ep); // Send the bytes

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error sending data to " + cm.replyIPAddress.ToString() + " " + e.Message);
                        }
                    }
                    msg_bytes = Encoding.ASCII.GetBytes(msg); // Convert to bytes
                    System.Threading.Thread.Sleep(10);
                    _udpClient.Send(msg_bytes, msg_bytes.Length, ep); // Send the bytes
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