using UnityEngine;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class PlayerControllerScript : MonoBehaviour
{

    // 1. Declare Variables
    Thread receiveThread; //1
    UdpClient client; //2
    int port; //3
    GameObject go;
    DisplayWebCam web;

    // 2. Initialize variables
    void Start()
    {
        port = 5065; //1 
        InitUDP(); //4
        client = new UdpClient(port); //1
        go = GameObject.Find("Cube");
        web = go.GetComponent<DisplayWebCam>();
    }

    // 3. InitUDP
    private void InitUDP()
    {
        print("UDP Initialized");

        receiveThread = new Thread(new ThreadStart(ReceiveData)); //1 
        receiveThread.IsBackground = true; //2
        receiveThread.Start(); //3

    }

    // 4. Receive Data
    private void ReceiveData()
    {


        while (true) //2
        {
            string text = "";
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port); //3
                byte[] data = client.Receive(ref anyIP); //4

                text = Encoding.UTF8.GetString(data); //5
                print(">> " + text);

            }
            catch (Exception e)
            {
                // print out error type
                print(e.ToString()); //7
            }

            if (!(string.IsNullOrEmpty(text)))
            {
                // if the string has some content
                if (text == "updated")
                {
                    // then we want to pull the new texture
                    web.update_image(true);
                }
            }
        }
    }

    // 6. Check for variable value, and make the Player Jump!
    void Update()
    {

    }
}
