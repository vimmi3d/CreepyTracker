using UnityEngine;
using System.Collections.Generic;
using System.Net;

public class AvatarMessage {

   public List<string> calibrations;

    public AvatarMessage(string message, byte[] receivedBytes)
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
        return "AvatarMessage" + MessageSeparators.L0 + Network.player.ipAddress + MessageSeparators.L1 + (mode) + MessageSeparators.L1 + port;
    }
}
