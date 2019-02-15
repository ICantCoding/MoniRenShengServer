
//////////////////////////////////////////////////////////////////
//                            _ooOoo_                           //
//                           o8888888o                          //
//                           88" . "88                          //
//                           (| -_- |)                          //
//                           O\  =  /O                          //    
//                        ____/`---'\____                       //
//                      .'  \\|     |//  `.                     //
//                     /  \\|||  :  |||//  \                    //
//                    /  _||||| -:- |||||-  \                   //
//                    |   | \\\  -  /// |   |                   //
//                    | \_|  ''\---/''  |   |                   //
//                    \  .-\__  `-`  ___/-. /                   //
//                  ___`. .'  /--.--\  `. . __                  //
//               ."" '<  `.___\_<|>_/___.'  >'"".               //
//              | | :  `- \`.;`\ _ /`;.`/ - ` : | |             //
//              \  \ `-.   \_ __\ /__ _/   .-` /  /             //
//         ======`-.____`-.___\_____/___.-`____.-'======        //
//                            `=---='                           //
//        ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^         //
//           佛祖保佑 程序员一生平安,健康,快乐,没有Bug!         //
//////////////////////////////////////////////////////////////////


namespace TDFramework.Network
{
    using System;
    using System.Text;
    using System.Net;
    using System.Net.Sockets;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    public class Agent
    {
        private static readonly object lockObj = new object();
        private static uint maxId = 0;
        private const int bufferSize = 2048;

        #region 字段
        private uint m_id;               //客户端代理Id号
        private Socket m_socket;
        private bool m_isClose = false; //客户端代理是否关闭连接
        EndPoint m_endPoint;

        private byte[] m_buffer = new byte[bufferSize]; //接收数据流缓存，Tcp数据流无界限
        private MessageReader m_messageReader = null; //接收数据流转完整包数据
        private Actor m_actor = null;  //Actor模式，一切皆Actor
        private WatchDogActor m_dogActor = null; //看门狗Actor
        private NetworkServer m_server = null; //服务器Server
        #endregion

        #region 属性
        public uint Id
        {
            get { return m_id; }
        }
        public Actor Actor
        {
            get { return m_actor; }
            set { m_actor = value; }
        }
        public WatchDogActor DogActor
        {
            get { return m_dogActor; }
            set { m_dogActor = value; }
        }
        public NetworkServer Server
        {
            get { return m_server; }
            set { m_server = value; }
        }
        #endregion

        #region 构造函数
        public Agent(Socket socket)
        {
            lock (lockObj)
            {
                m_id = ++maxId;
            }
            m_socket = socket;
            m_endPoint = m_socket.RemoteEndPoint;
            m_messageReader = new MessageReader(HandleMessage);
        }
        #endregion

        #region 方法
        public void StartReceive()
        {
            if (Valid())
            {
                try
                {
                    if(m_dogActor != null)
                    {
                        m_dogActor.SendMsg(string.Format("open {0}", m_id)); //看门狗发送一个消息
                    }
                    //会开辟子线程, 该子线程阻塞, 等待数据流的到来
                    m_socket.BeginReceive(m_buffer, 0, bufferSize, SocketFlags.None, OnReceiveCallback, m_socket); 
                }
                catch (Exception exception)
                {
                    //接收数据失败,就直接关闭掉该客户端的连接
                    Debug.Log("Agent BeginReceive Fail. " + exception.Message);
                    Close();
                }
            }
        }
        private void OnReceiveCallback(IAsyncResult ar)
        {
            int bytes = 0;
            try
            {
                bytes = m_socket.EndReceive(ar);
            }
            catch (Exception exception)
            {
                //接收数据失败,就直接关闭掉该客户端的连接
                Debug.Log("Agent BeginReceive Fail. " + exception.Message);
                Close();
            }
            if (bytes <= 0)
            {
                //接收数据的大小<=0, 就直接关闭到该客户端的连接
                Close();
            }
            else
            {
                uint num = (uint)bytes;
                string content = Encoding.UTF8.GetString(m_buffer, 0, bytes);
                Debug.Log("接收到Agent Id: " + m_id.ToString() + "的数据: " + content);
                m_messageReader.Process(m_buffer, (uint)bytes);
                try
                {
                    m_socket.BeginReceive(m_buffer, 0, bufferSize, SocketFlags.None, OnReceiveCallback, m_socket);
                }
                catch (Exception exception)
                {
                    //接收数据失败,就直接关闭掉该客户端的连接
                    Console.WriteLine(exception.Message);
                    Close();
                }
            }
        }
        //MessageReader对象获取到完整的一个数据包后，需执行这个回调
        void HandleMessage(Packet packet)
        {
            if (m_actor != null)
            {
                //Agent接收到客户端发送过来的完整数据包Packet后，将这个数据包通过该Agent的Actor进行顺序排序
                m_actor.SendMsg(packet); 
            }
        }
        //判断该客户端的Socket连接是否有效
        public bool Valid()
        {
            if (m_socket != null && m_socket.Connected)
            {
                return true;
            }
            return false;
        }
        //关闭该客户端的连接
        public void Close()
        {
            if (m_isClose)
            {
                return;
            }
            if (Valid())
            {
                try
                {
                    m_socket.Shutdown(SocketShutdown.Both);
                    m_socket.Close();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }
            m_socket = null;
            m_isClose = true;

            if (m_actor != null)
            {
                m_actor.SendMsg(string.Format("close"));
            }
            if (m_dogActor != null)
            {
                m_dogActor.SendMsg(string.Format("close {0}", m_id));
            }
            if (m_server != null)
            {
                m_server.RemoveAgent(this);
            }
        }
        #endregion
    }
}
