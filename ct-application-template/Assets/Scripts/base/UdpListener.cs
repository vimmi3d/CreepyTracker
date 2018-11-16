using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;

public class UdpListener : MonoBehaviour {

	public static string NoneMessage = "0";

	public int Port;

    private UdpClient _udpClient = null;
    private IPEndPoint _anyIP;
    private List<string> _stringsToParse;

    void Start()
    { 
		udpRestart();
	}

    public void udpRestart()
    {
        if (_udpClient != null)
        {
            _udpClient.Close();
        }

        _stringsToParse = new List<string>();
        
		_anyIP = new IPEndPoint(IPAddress.Any, Port);
        
        _udpClient = new UdpClient(_anyIP);

        _udpClient.BeginReceive(new AsyncCallback(this.ReceiveCallback), null);

		Debug.Log("[UDPListener] Receiving in port: " + Port);
    }

    public void ReceiveCallback(IAsyncResult ar)
    {
        Byte[] receiveBytes = _udpClient.EndReceive(ar, ref _anyIP);
        _stringsToParse.Add(Encoding.ASCII.GetString(receiveBytes));

        _udpClient.BeginReceive(new AsyncCallback(this.ReceiveCallback), null);
    }

    void Update()
    {
        while (_stringsToParse.Count > 0)
        {
            string stringToParse = _stringsToParse.First();
            _stringsToParse.RemoveAt(0);

			List<Body> bodies = new List<Body>();

			if (stringToParse.Length != 1)
			{
				List<string> bstrings = new List<string>(stringToParse.Split(MessageSeparators.L1)); 

				bstrings.RemoveAt(0); // first statement is not a body

				foreach (string b in bstrings)
				{
					if (b != NoneMessage) bodies.Add(new Body(b));
				}
			}
			gameObject.GetComponent<TrackerClient>().setNewFrame(bodies.ToArray());
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
