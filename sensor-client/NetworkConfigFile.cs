using System;
using System.IO;
using Microsoft.Kinect;
using System.Collections.Generic;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    internal class NetworkConfigFile
    {
        private string _port = "33333";
        private string _listenPort = "5007";

        private Dictionary<string, int> _jointConfidenceWeight;
        public Dictionary<string, int> JointConfidenceWeight
        {
            get
            {
                return _jointConfidenceWeight;
            }
        }

        public NetworkConfigFile(string filename)
        {
            _jointConfidenceWeight = new Dictionary<string, int>();
            foreach (JointType j in Enum.GetValues(typeof(JointType)))
            {
                _jointConfidenceWeight[j.ToString()] = 1;
            }


            if (File.Exists(filename))
            {
                foreach (string line in File.ReadAllLines(filename))
                {
                    string [] s = line.Split('=');

                    if (s[0] == "udp.port")
                    {
                        _port = s[1];
                    }
                    if (s[0] == "udp.listen")
                    {
                        _listenPort = s[1];
                    }
                    else if (_jointConfidenceWeight.ContainsKey(s[0]))
                    {
                        _jointConfidenceWeight[s[0]] = int.Parse(s[1]);
                    }
                }
            }
        }

        public string Port { get { return _port; } internal set { _port = value; } }
        public string ListenPort { get { return _listenPort; } internal set { _listenPort = value; } }

    }
}