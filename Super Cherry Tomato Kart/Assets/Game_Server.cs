using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;

public class Game_Server : MonoBehaviour
{
    public int port = 8080;

    private List<ServerClient> clients;
    private List<ServerClient> disconnected_clients;

    private TcpListener server;
    private bool serverStarted;
    // Use this for initialization

    // Public Static gives us access to these variables from other classes
    public static string Player1_Buttons = "";
    public static string Player2_Buttons = "";
    public static string Player3_Buttons = "";
    public static string Player4_Buttons = "";

    void Start()
    {
        Init();
    }

    public void Init() {
        clients = new List<ServerClient>();
        disconnected_clients = new List<ServerClient>();

        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            serverStarted = true;
            
            StartListening();
        }
        catch (Exception e)
        {
            Debug.Log("Socket error: " + e.Message);
        }
    }

    private void Update()
    {
        if (!serverStarted)
            return;

        foreach(ServerClient c in clients) {

            if (!IsConnected(c.tcp))
            {
                c.tcp.Close();
                disconnected_clients.Add(c);
                continue;
            }
            else
            {
                NetworkStream s = c.tcp.GetStream();
                if (s.DataAvailable)
                {
                    StreamReader reader = new StreamReader(s, true);
                    
                    while (s.DataAvailable) {
                        string data = reader.ReadLine();

                        if (data != null)
                        {
                            OnIncomingData(c, data);
                        }
                    }
                }
            }
        }

        for (int i = 0; i < disconnected_clients.Count - 1; i++)
        {
            clients.Remove(disconnected_clients[i]);
            disconnected_clients.RemoveAt(i);
        }
    }

    private void StartListening()
    {
        Debug.Log("begin Listening for Clients");
        server.BeginAcceptTcpClient(AcceptTcpClient, server);
    }

    private void AcceptTcpClient(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        
        ServerClient sc = new ServerClient(listener.EndAcceptTcpClient(ar));
        clients.Add(sc);

        Debug.Log("New connection!");

        StartListening();
    }

    private bool IsConnected(TcpClient c)
    {
        try {
            if (c != null && c.Client != null && c.Client.Connected)
            {
                if (c.Client.Poll(0, SelectMode.SelectRead))
                    return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);

                return true;
            }
            else {
                return false;
            }
        }
        catch {
            return false;
        }
    }

    private void MessageClient(string data, List<ServerClient> cl)
    {
        foreach (ServerClient sc in cl) {
            try
            {
                StreamWriter writer = new StreamWriter(sc.tcp.GetStream());
                writer.WriteLine(data);
                writer.Flush();
            }
            catch (Exception e)
            {
                Debug.Log("Write error: " + e.Message);
            }
        }
    }

    void Awake() { DontDestroyOnLoad(transform.gameObject); }

    private void OnIncomingData(ServerClient c, string data)
    {
        

        if (data.Length >= 14 && data.Substring(0,3) == "P1:") {
            Player1_Buttons = data.Substring(3, 11);
            Debug.Log("Player 1: " + Player1_Buttons);
        }

        if (data.Length >= 28 && data.Substring(14, 3) == "P2:")
        {
            Player2_Buttons = data.Substring(17, 11);
            Debug.Log("Player 2: " + Player2_Buttons);
        }

        if (data.Length >= 42 && data.Substring(28, 3) == "P3:")
        {
            Player3_Buttons = data.Substring(31, 11);
            Debug.Log("Player 3: " + Player3_Buttons);
        }

        if (data.Length >= 56 && data.Substring(42, 3) == "P4:")
        {
            Player4_Buttons = data.Substring(45, 11);
            Debug.Log("Player 4: " + Player4_Buttons);
        }
    }

}

public class ServerClient
{
    public string clientName;
    public TcpClient tcp;

    public ServerClient(TcpClient tcp)
    {
        this.tcp = tcp;
    }

}