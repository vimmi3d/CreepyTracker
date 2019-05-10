using UnityEngine;
using System.Collections.Generic;
using System.Net;

public class AvatarMessage {

   public List<string> calibrations;

    public AvatarMessage(string message)
    {
        calibrations = new List<string>();
        string[] chunks = message.Split(MessageSeparators.L1);
        
        foreach (string s in chunks)
        {
            calibrations.Add(s);
        }
    }

   public static string createRequestMessage(int mode,int port)
    {
        return "AvatarMessage" + MessageSeparators.L0 + IPManager.GetIP(ADDRESSFAM.IPv4) + MessageSeparators.L1 + (mode) + MessageSeparators.L1 + port;
    }
}
