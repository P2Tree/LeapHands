using UnityEngine;
using System.Collections;
// 引入库  
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;

namespace Network
{
    public class udpSocket : MonoBehaviour
    {

        Socket socket;
        EndPoint serverEnd;
        IPEndPoint ipEnd;

        string recvStr;
        string sendStr;
        byte[] recvData = new byte[1024];
        byte[] sendData = new byte[1024];
        int recvLen;
        Thread connectThread;

        public udpSocket(string ip, int port)
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
            print("waiting for sending UDP dgram");
            // 建立初始连接，这句非常重要，第一次连接初始化了 serverEnd 后面才能收到消息  
            SocketSend("");

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

        // 服务器接收  
        public void SocketReceive()
        {
            // 进入接收循环  
            while (true)
            {
                // 对 data 清零  
                recvData = new byte[1024];
                // 获取客户端，获取服务端端数据，用引用给服务端赋值，实际上服务端已经定义好并不需要赋值  
                recvLen = socket.ReceiveFrom(recvData, ref serverEnd);
                print("message from:" + serverEnd.ToString()); // 打印服务端信息  
                                                               // 输出接收到的数据  
                recvStr = Encoding.ASCII.GetString(recvData, 0, recvLen);
                print(recvStr);
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
}