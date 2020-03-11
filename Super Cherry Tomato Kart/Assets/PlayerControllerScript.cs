using UnityEngine;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;


public class PlayerControllerScript : MonoBehaviour
{

    // 1. Declare Variables
    Thread receiveThread; //1
    UdpClient client; //2
    int port; //3
    GameObject go;
    GameObject fp;
    DisplayWebCam web;
    Process pro;
    KartGame.Track.TrackManager tp;

    // 2. Initialize variables
    void Start()
    {
        ProcessStartInfo psi = new ProcessStartInfo();
        //need to update to wherever the conda installation is
        //psi.FileName = "\'C:\\Users\\Patrick Chau\\Anaconda3\\_conda.exe\'";
        psi.FileName = "\'C:\\Users\\firey\\AppData\\Local\\Programs\\Python\\Python37\\python.exe\'";
        string script = Path.GetFullPath("FaceDetection\\facedetect.py");
        //print(script);
        psi.Arguments = string.Format("python \"{0}\"", script);
        //print(psi.Arguments);
        psi.UseShellExecute = false;
        //psi.RedirectStandardError = true;
        //psi.RedirectStandardInput = true;
        //psi.RedirectStandardOutput = true;
        //psi.CreateNoWindow = true;

        pro = Process.Start(psi);


        port = 5065; //1 
        InitUDP(); //4
        client = new UdpClient(port); //1
        go = GameObject.Find("Cube");
        web = go.GetComponent<DisplayWebCam>();
        fp = GameObject.Find("TrackManager");
        tp = fp.GetComponent<KartGame.Track.TrackManager>();

    }
    private void OnApplicationQuit()
    {
        pro.Kill();
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
        string received = "";
        while (true) //2
        {
            string text = "";
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port); //3

                // receive data
                byte[] data = client.Receive(ref anyIP); //4
                text = Encoding.UTF8.GetString(data); //5

                // try to send data, only send if there is an update in first place
                string fi = tp.GetFirstPlace();
                //print("fi: " + fi + "   text:" + received + "   Equality?: " + fi.Equals(received));
                if (!fi.Equals(received))
                {
                    //print("First place updated! First place player is now " + fi);
                    received = fi;
                    byte[] sendData = Encoding.UTF8.GetBytes(received);
                    client.Send(sendData, sendData.Length, anyIP);
                }
            }
            catch (Exception e)
            {
                // print out error type
                //print(e.ToString()); //7
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
