using System;
using System.Text;
using System.IO;
using System.Net.Sockets;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class TcpSender
    {
        private bool _connected;
        public bool Connected { get { return _connected; } }

        private TcpClient _client;
        private Stream _stream;

        public string _address;
        private int _port;
        private byte[] sendHeaderBuffer;
        private ASCIIEncoding _encoder;
        private byte[] ackbuf;

        public TcpSender()
        {
            _connected = false;
            sendHeaderBuffer = new byte[13];
            ackbuf = new byte[3];
        }

        public void connect(string address, int port)
        {
            _address = address;
            _port = port;

            _encoder = new ASCIIEncoding();

            _client = new TcpClient();
            try
            {
                _client.Connect(address, port);

                _stream = _client.GetStream();

                _connected = true;

                this.write("k/" + Environment.MachineName + "/");
            }
            catch (Exception e)
            {
                _connected = false;
                Console.WriteLine("Unable to connect");
            }
        }

        public void write(string line)
        {
            byte[] ba = _encoder.GetBytes(line);
            this.write(ba,ba.Length);
        }

        public void sendData(byte[] frame, uint messageCount, int size,bool compressed,int scale)
        {
            byte[] id = BitConverter.GetBytes(messageCount);
            byte[] count = BitConverter.GetBytes(size);
            byte[] b = BitConverter.GetBytes(compressed);
            byte[] s = BitConverter.GetBytes(scale);
            Array.Copy(id, 0, sendHeaderBuffer, 0, 4);
            Array.Copy(count, 0, sendHeaderBuffer, 4, 4);
            Array.Copy(b, 0, sendHeaderBuffer, 8, 1);
            Array.Copy(s, 0, sendHeaderBuffer, 9, 4);
            write(sendHeaderBuffer,13);
            write(frame,size);
           // confirm();
        }

        public void confirm()
        {
            if (_connected)
            {
                byte[] ok;
                try
                {
                    _stream.Read(ackbuf,0,3);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    close();
                    _connected = false;
                }
            }
        }

        public void write(byte[] frame,int size)
        {
            if (_connected)
            {
                try
                {
                    _stream.Write(frame, 0,size);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    close();
                    _connected = false;
                }
            }
        }

        public void close()
        {
            _client.Close();
        }
    }
}
