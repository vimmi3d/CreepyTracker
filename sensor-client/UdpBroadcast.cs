using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class UdpBroadcast
    {
        private string _address;
        private int _port;

        private IPEndPoint _remoteEndPoint;
        private UdpClient _udp;
        private int _sendRate;

        private DateTime _lastSent;

        private bool _streaming = false;

        public UdpBroadcast(int port, int sendRate = 100)
        {
            _lastSent = DateTime.Now;
            reset(port, sendRate);
        }

        public void reset(int port, int sendRate = 100)
        {
            _sendRate = sendRate;
            try
            {
                _port = port;

                _remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, _port);
                _udp = new UdpClient();
                _streaming = true;
            }
            catch (Exception e) { }
        }

        public void send(string line)
        {
            if (_streaming)
            {
                try
                {
                    //if (DateTime.Now > _lastSent)
                    //{
                        byte[] data = Encoding.UTF8.GetBytes(line);
                        _udp.Send(data, data.Length, _remoteEndPoint);
                        _lastSent = DateTime.Now;
                    //}
                }
                catch (Exception e)
                {
                }
            }
        }
    }
}