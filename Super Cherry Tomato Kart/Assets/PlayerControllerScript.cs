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
    DisplayWebCam web;
    Process pro;

    // 2. Initialize variables
    void Start()
    {
        ProcessStartInfo psi = new ProcessStartInfo();
        print("hello world");
        //need to update to wherever the conda installation is
        //psi.FileName = "\'C:\\Users\\Patrick Chau\\Anaconda3\\envs\\test\\python.exe\'";
        psi.FileName = "\'C:\\Users\\Patrick Chau\\Anaconda3\\_conda.exe\'";
        string script = Path.GetFullPath("FaceDetection\\facedetect.py");
        print(script);
        psi.Arguments = string.Format("python \"{0}\"",script);
        print(psi.Arguments);
        psi.UseShellExecute = false;
        psi.RedirectStandardError = true;
        psi.RedirectStandardInput = true;
        psi.RedirectStandardOutput = true;
        string errors = "None";
        string results = "None";
        pro = Process.Start(psi);
            /*
        using ()
        {
            errors = pro.StandardError.ReadToEnd();
            results = pro.StandardOutput.ReadToEnd();
        }
        print(errors+ "    " + results);
        */
        port = 5065; //1 
        InitUDP(); //4
        client = new UdpClient(port); //1
        go = GameObject.Find("Cube");
        web = go.GetComponent<DisplayWebCam>();
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
