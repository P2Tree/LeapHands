// 引入库  
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

using UnityEngine;

namespace Network
{
    public class udpClientSocket : MonoBehaviour
    {

        Socket socket;
        EndPoint serverEnd;
        IPEndPoint ipEnd;

        string recvStr;
        byte[] recvData = new byte[1024];
        byte[] sendData = new byte[1024];
        int recvLen;
        Thread connectThread;

        public udpClientSocket(string ip, int port)
        {
            InitSocket(ip, port);
        }

        private void InitSocket(string ip = "127.0.0.1", int port = 8001)
        {
            // 定义连接的服务器 ip 和端口，可以是本机 ip，局域网，互联网  
            ipEnd = new IPEndPoint(IPAddress.Parse(ip), port);
            // 定义套接字类型, 在主线程中定义  
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // 定义服务端  
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            serverEnd = (EndPoint)sender;
            // 建立初始连接，这句非常重要，第一次连接初始化了 serverEnd 后面才能收到消息  
            try
            {
                SocketSend("");
                Debug.Log("udp通信正常");
                Debug.Log("服务器IP: " + ip + ", " + "服务器端口: " + port);
            }
            catch (Exception ex)
            {
                Debug.Log("udp通信失败！(" + ex.Message + ")");
                Debug.Log("尝试服务器IP: " + ip + ", " + "尝试服务器端口: " + port);
                socket.Close();
                return;
            }

            // 开启一个线程连接，必须的，否则主线程卡死  
            connectThread = new Thread(new ThreadStart(SocketReceive));
            connectThread.Start();
        }
        public void SocketSend(string sendStr)
        {
            // 清空发送缓存  
            sendData = new byte[1024];
            // 数据类型转换  
            sendData = Encoding.ASCII.GetBytes(sendStr);
            // 发送给指定服务端  
            socket.SendTo(sendData, sendData.Length, SocketFlags.None, ipEnd);
        }

        public void SocketSend(byte[] sendStr)
        {
            // sendData = new byte[1024];
            // sendData = sendStr;
            // socket.SendTo(sendData, sendData.Length, SocketFlags.None, ipEnd);
            socket.SendTo(sendStr, sendStr.Length, SocketFlags.None, ipEnd);
        }

        // 服务器接收，单独启动一个线程运行接收函数，所以内部是死循环 
        public void SocketReceive()
        {
            // 进入接收循环  
            while (true)
            {
                // 对 data 清零  
                recvData = new byte[1024];
                // 获取客户端，获取服务端端数据，用引用给服务端赋值，实际上服务端已经定义好并不需要赋值  
                recvLen = socket.ReceiveFrom(recvData, ref serverEnd);
                print("udp接收消息来自：" + serverEnd.ToString()); // 打印服务端信息  
                                                               // 输出接收到的数据  
                recvStr = Encoding.ASCII.GetString(recvData, 0, recvLen);
                print("udp接收内容：" + recvStr);
                Debug.Log("udp接收长度：" + recvLen);
            }
        }

        // 连接关闭  
        public void SocketQuit()
        {
            // 关闭线程  
            if (connectThread != null)
            {
                connectThread.Interrupt();
                connectThread.Abort();
            }
            // 最后关闭 socket  
            if (socket != null)
                socket.Close();
        }


    }

    public class tcpClientSocket {
        Socket socket;
        IPAddress mIp;
        IPEndPoint ipEnd;
        string recvStr;
        string sendStr;
        byte[] recvData = new byte[1024];
        byte[] sendData = new byte[1024];
        int recvLen;
        public bool isConnected = false;
        Thread connectThread;

        public tcpClientSocket(string ip, int port)
        {
            InitSocket(ip, port);
        }

        private void InitSocket(string ip = "127.0.0.1", int port = 8088)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            mIp = IPAddress.Parse(ip);
            ipEnd = new IPEndPoint(mIp, port);

            try
            {
                socket.Connect(ipEnd);
                isConnected = true;
                Debug.Log("tcp连接服务器成功");
                Debug.Log("服务器IP: " + ip + ", " + "服务器端口: " + port);
            }
            catch (Exception ex)
            {
                isConnected = false;
                Debug.LogError("tcp连接服务器失败! (" + ex.Message + ")");
                Debug.LogError("尝试服务器IP: " + ip + ", " + "尝试服务器端口: " + port);
                socket.Close();
                return;
            }

            connectThread = new Thread(new ThreadStart(SocketReceive));
            connectThread.Start();
        }

        public void SocketSend(string data)
        {
            if (isConnected == false)
            {
                Debug.LogWarning("TCP通信未连接");
                return;
            }
            try
            {
                sendData = new byte[1024];
                sendData = Encoding.ASCII.GetBytes(data);
                socket.Send(sendData);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                isConnected = false;
                socket.Close();
            }
        }

        public void SocketSend(byte[] data)
        {
            if (isConnected == false)
            {
                Debug.LogWarning("TCP通信未连接");
                return;
            }
            try
            {
                socket.Send(data);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                isConnected = false;
                socket.Close();
            }
        }

        public void SocketReceive()
        {
            if (isConnected == false)
            {
                Debug.LogWarning("TCP通信未连接");
                return;
            }
            while(true)
            {
                try
                {
                    recvLen = socket.Receive(recvData);
                    recvStr = Encoding.ASCII.GetString(recvData, 0, recvLen);
                    Debug.Log("tcp接收内容：" + recvStr);
                    Debug.Log("tcp接收长度：" + recvLen);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                    isConnected = false;
                    // socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    return;
                }
            }
        }

        public void SocketQuit()
        {
            if (connectThread != null)
            {
                connectThread.Interrupt();
                connectThread.Abort();
            }
            if (socket != null)
            {
                socket.Close();
            }
        }
    }

    public class tcpServerSocket {
        Socket socket;
        IPAddress mIp;
        IPEndPoint ipEnd;
        tcpServerSocket(string ip, int port)
        {
            InitSocket(ip, port);
        }

        private void InitSocket(string ip = "127.0.0.1", int port = 8089)
        {
            // IPAddress ip = IPAddress.Parse(ip);
            // ipEnd = new IPEndPoint(ip, port);
            // create server socket object
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // bind ip and port
            socket.Bind(ipEnd);
        }
    }
}