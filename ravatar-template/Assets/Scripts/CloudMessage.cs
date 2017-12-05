using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class CloudMessage {

    public string message;
    public byte[] receivedBytes;
    public int headerSize;
    public CloudMessage()
    {
        message = "";
    }

	public void set(string message, byte[] receivedBytes,int headerSize)
	{
        //moved implementation to
        this.message = message;
        this.receivedBytes = receivedBytes;
        this.headerSize = headerSize; 
    }

	public static string createRequestMessage(int mode,string addr,int port)
	{
		return "CloudMessage" + MessageSeparators.L0 + addr + MessageSeparators.L1 + (mode) + MessageSeparators.L1 + port;
	}
}
