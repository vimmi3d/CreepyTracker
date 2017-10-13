using System;
using System.Net;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class CloudMessage
    {
        public IPAddress replyIPAddress;
        public int port;
        public int mode;

        public CloudMessage(string m)
        {
            string[] msg = m.Split(MessageSeparators.L1);
            replyIPAddress = IPAddress.Parse(msg[0]);
            mode = int.Parse(msg[1]); 
            port = int.Parse(msg[2]);
        }

        public static string createMessage(string cloudInfo, uint id)
        {
            return "CloudMessage" + MessageSeparators.L0 + Environment.MachineName + MessageSeparators.L1 + id + MessageSeparators.L1 + cloudInfo; 
        }
    }
}