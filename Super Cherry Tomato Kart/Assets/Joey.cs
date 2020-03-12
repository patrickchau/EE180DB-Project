using System;
using UnityEngine;
using System.Net; 
using System.Net.Sockets; 
using System.Text;
using System.Threading;

public class Joey: MonoBehaviour {

    // Use this for initialization
    void Start () {
        // Usually the server doesn't need to draw anything on the screen
        Debug.Log("Joey Miller");
        Thread thr2 = new Thread(CreateServer);
        thr2.Start();
    }    

    void CreateServer() {
        IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());  
        //GetHostName not working on mac
    	IPAddress ipAddr = ipHost.AddressList[0]; 
    	IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111); 
  
    	Socket listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp); 

        try { 
          
        // Using Bind() method we associate a 
        // network address to the Server Socket 
        // All client that will connect to this  
        // Server Socket must know this network 
        // Address 
        	listener.Bind(localEndPoint); 
  
        // Using Listen() method we create  
        // the Client list that will want 
        // to connect to Server 
        	listener.Listen(10); 
        	while (true) { 
              
	            Debug.Log("Waiting connection ... "); 
	  
	            // Suspend while waiting for 
	            // incoming connection Using  
	            // Accept() method the server  
	            // will accept connection of client 
	            Socket clientSocket = listener.Accept(); 
	  
	            // Data buffer 
	            byte[] bytes = new Byte[1024]; 
	            string data = null; 
  
            	while (true) { 
                    Debug.Log("forever");
	                int numByte = clientSocket.Receive(bytes); 
	                  
	                data += Encoding.ASCII.GetString(bytes, 
	                                           0, numByte); 
	                                             
	                if (data.IndexOf("#") > -1) 
	                    break; 
            	} 

            Debug.Log(data); 
            byte[] message = Encoding.ASCII.GetBytes("Test Server"); 
  
            // Send a message to Client  
            // using Send() method 
            clientSocket.Send(message); 
  
            // Close client Socket using the 
            // Close() method. After closing, 
            // we can use the closed Socket  
            // for a new Client Connection 
            clientSocket.Shutdown(SocketShutdown.Both); 
            clientSocket.Close(); 
        	} 
    	} 
    	catch (Exception e) { 
        	Console.WriteLine(e.ToString()); 
    	} 


    }
  
}