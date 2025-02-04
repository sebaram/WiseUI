using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

public class TCPClient : MonoBehaviour
{
    //protected TcpClient socket;
    protected Socket socket;
    

    public virtual void Connect(string serverIP, int serverPort)
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPAddress serverAddr = IPAddress.Parse(serverIP);
        IPEndPoint clientEP = new IPEndPoint(serverAddr, serverPort);
        socket.Connect(clientEP);

        
        
    }

    public bool isConnected
    {
        get
        {
            if (socket == null)
                return false;

            return socket.Connected;
        }
    }
  
    /// Send message to server using socket connection.     
    public void SendMessage(byte[] buffer)
    {
        socket.Send(buffer);
    }

    public virtual void ReceieveCallBack(IAsyncResult aResult)
    {

    }

    public void Disconnect()
    {
        //Waiting for exiting ohter thread.
        if (socket != null && socket.Connected)
        {
            //socket.Disconnect(false);
            socket.Close();
            socket = null;
        }
    }
    private void OnDestroy()
    {
        Disconnect();
    }

}
