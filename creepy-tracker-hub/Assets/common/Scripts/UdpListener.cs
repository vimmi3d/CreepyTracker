using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;

public class UdpListener : MonoBehaviour
{
    private UdpClient _udpClient = null;
    private IPEndPoint _anyIP;
    private List<byte[]> _stringsToParse; // TMA: Store the bytes from the socket instead of converting to strings. Saves time.
    private byte[] _receivedBytes;
    //so we don't have to create again
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
                    // TMA: THe first char distinguishes between a BodyMessage and a CloudMessage
                    if (Convert.ToChar(toProcess[0]) == 'B')
                    {
                        try
                        {
                            string stringToParse = Encoding.ASCII.GetString(toProcess);
                            string[] splitmsg = stringToParse.Split(MessageSeparators.L0);
                            BodiesMessage b = new BodiesMessage(splitmsg[1]);
                            gameObject.GetComponent<Tracker>().setNewFrame(b);
                        }
                        catch (BodiesMessageException e)
                        {
                            Debug.Log(e.Message);
                        }
                    }
                    else if (Convert.ToChar(toProcess[0]) == 'C')
                    {
                        string stringToParse = Encoding.ASCII.GetString(toProcess);
                        string[] splitmsg = stringToParse.Split(MessageSeparators.L0);
                        message.set(splitmsg[1], toProcess, splitmsg[0].Length);
                        gameObject.GetComponent<Tracker>().setNewCloud(message);
                    }
                    else if (Convert.ToChar(toProcess[0]) == 'A')
                    {
                        string stringToParse = Encoding.ASCII.GetString(toProcess);
                        string[] splitmsg = stringToParse.Split(MessageSeparators.L0);
                        AvatarMessage av = new AvatarMessage(splitmsg[1], toProcess);
                        gameObject.GetComponent<Tracker>().processAvatarMessage(av);
                    }
                    else if (Convert.ToChar(toProcess[0]) == 'S')
                    {
                        string stringToParse = Encoding.ASCII.GetString(toProcess);
                        string[] splitmsg = stringToParse.Split(MessageSeparators.L0);
                        SurfaceMessage av = new SurfaceMessage(splitmsg[1], toProcess);
                        gameObject.GetComponent<Tracker>().processSurfaceMessage(av);
                    }
                }
                _stringsToParse.RemoveAt(0);
            }
            catch (Exception /*e*/) { _stringsToParse.RemoveAt(0); }
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