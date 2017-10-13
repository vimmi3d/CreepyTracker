using System.Collections.Generic;
using System.Net;

public class SurfaceMessage
{
    public IPAddress replyIPAddress;
    public int port;

    public SurfaceMessage(string message, byte[] receivedBytes)
    {
        string[] msg = message.Split(MessageSeparators.L1);
        replyIPAddress = IPAddress.Parse(msg[0]);
        port = int.Parse(msg[1]);
    }

    public string createSurfaceMessage(List<Surface> surfaces)
    {
        string res = "SurfaceMessage" + MessageSeparators.L0;
        foreach (Surface s in surfaces)
        {
            res += s.name + MessageSeparators.L1
                + CommonUtils.convertVectorToStringRPC(s.cBottomLeft) + MessageSeparators.L1
                + CommonUtils.convertVectorToStringRPC(s.cBottomRight) + MessageSeparators.L1
                + CommonUtils.convertVectorToStringRPC(s.cTopLeft) + MessageSeparators.L1
                + CommonUtils.convertVectorToStringRPC(s.cTopRight)
                + MessageSeparators.L1;
        }



        return res;
    }
}
