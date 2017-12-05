using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;


public static class MessageSeparators
{
    public const char L0 = '$'; // header separator
    public const char L1 = '#'; // top level separator -> bodies
    public const char L2 = '/'; // -> body attributes
    public const char L3 = ':'; // -> 3D values
    public const char SET = '=';
}


public class UdpListener : MonoBehaviour {

    private UdpClient _udpClient = null;
    private IPEndPoint _anyIP;
    private List<byte[]> _stringsToParse; // TMA: Store the bytes from the socket instead of converting to strings. Saves time.
    private byte[] _receivedBytes;
    private int number = 0;
    CloudMessage message;



    void Start()
    {
        message = new CloudMessage();
    }

    public void udpRestart()
    {
        if (_udpClient != null)
        {
            _udpClient.Close();
        }

        _stringsToParse = new List<byte[]>();
        
		_anyIP = new IPEndPoint(IPAddress.Any, TrackerProperties.Instance.listenPort);
        
        _udpClient = new UdpClient(_anyIP);

        _udpClient.BeginReceive(new AsyncCallback(this.ReceiveCallback), null);

		Debug.Log("[UDPListener] Receiving in port: " + TrackerProperties.Instance.listenPort);
    }
    
    public void ReceiveCallback(IAsyncResult ar)
    {
        Byte[] receiveBytes = _udpClient.EndReceive(ar, ref _anyIP);
        _udpClient.BeginReceive(new AsyncCallback(this.ReceiveCallback), null);
        _stringsToParse.Add(receiveBytes);
    }

    void Update()
    {
  
        while (_stringsToParse.Count > 0)
        {
            try
            {
                byte[] toProcess = _stringsToParse.First();
                if(toProcess != null)
                {
                  if (Convert.ToChar(toProcess[0]) == 'A')
                    {
                        Debug.Log("Got Calibration Message! ");
                        string stringToParse = Encoding.ASCII.GetString(toProcess);
                        string[] splitmsg = stringToParse.Split(MessageSeparators.L0);
                        AvatarMessage av = new AvatarMessage(splitmsg[1], toProcess);
                        gameObject.GetComponent<Tracker>().processAvatarMessage(av);
                    }
                }
                _stringsToParse.RemoveAt(0);
            }
            catch (Exception exc) { _stringsToParse.RemoveAt(0); }
        }
    }

    void OnApplicationQuit()
    {
        if (_udpClient != null) _udpClient.Close();
    }

    void OnQuit()
    {
        OnApplicationQuit();
    }
}
