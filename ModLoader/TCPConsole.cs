using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class TCPConsole : MonoBehaviour
{
    const int READ_BUFFER_SIZE = 3000;
    const string IP = "127.0.0.1";
    const int PORT = 9999;
    private byte[] readBuffer = new byte[READ_BUFFER_SIZE];
    public string strMessage = string.Empty;

    private TcpClient client;

    private void Awake()
    {
        Application.logMessageReceived += MLCallback;
        ListenForData();
    }
    private void ListenForData()
    {
        client = new TcpClient(IP, PORT);
        client.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(ReceiveData), null);
    }

    private void ReceiveData(IAsyncResult ar)
    {
        int BytesRead;
        lock (client.GetStream())
        {
            BytesRead = client.GetStream().EndRead(ar);
        }
        string message = Encoding.ASCII.GetString(readBuffer, 0, BytesRead);
        Debug.Log("Recived :" + message);
        VTOLAPI.instance.CheckConsoleCommand(message);
        lock (client.GetStream())
        {
            client.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(ReceiveData), null);
        }

    }
    private void MLCallback(string message, string stackTrace, LogType type)
    {
        if (client == null || !client.Connected)
            return;
        if (message.Contains("Recived"))
        {
            Debug.Log("GOOOOTTTT IT!");
        }
        //lock ensure that no other threads try to use the stream at the same time.
        lock (client.GetStream())
        {
            StreamWriter writer = new StreamWriter(client.GetStream());
            writer.Write(message);
            // Make sure all data is sent now.
            writer.Flush();
        }
    }
    private void OnApplicationQuit()
    {
        client.GetStream().Close();
    }
}