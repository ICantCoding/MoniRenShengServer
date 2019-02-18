
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

    public class SendBuffer
    {
        public int position = 0;
        public byte[] buffer;
        public int Size
        {
            get { return buffer.Length - position; }
        }
    }

    public class Agent
    {
        private static readonly object lockObj = new object();
        private static uint maxId = 0;
        private const int bufferSize = 2048;

        #region 字段
        private uint m_id;               //客户端代理Id号
        private Socket m_socket;
        private bool m_isClose = false; //客户端代理是否关闭连接
        private EndPoint m_endPoint;

        private byte[] m_buffer = new byte[bufferSize]; //接收数据流缓存，Tcp数据流无界限
        private MessageReader m_messageReader = null; //接收数据流转完整包数据
        private List<SendBuffer> m_sendBufferList = new List<SendBuffer>(); //Agent发送数据缓存列表，列表让发送的数据包有序
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
                        //看门狗发送一个消息, 用于创建PlayerActor
                        m_dogActor.SendActorMessageToCreatePlayerActor(m_id);
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
                    //继续等待下个数据
                    m_socket.BeginReceive(m_buffer, 0, bufferSize, SocketFlags.None, OnReceiveCallback, m_socket);
                }
                catch (Exception exception)
                {
                    //接收数据失败,就直接关闭掉该客户端的连接
                    Debug.Log(exception.Message);
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
                m_actor.SendMsg(packet); //Agent对应的PlayerActor会接受这个Packet并做处理
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
                    m_socket.Shutdown(SocketShutdown.Both); //优雅关闭Socket连接
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
                //关闭Agent的时候，肯定要使用看门狗通知销毁PlayerActor
                m_dogActor.SendActorMessageToDestroyPlayerActor(m_id);
            }
            if (m_server != null)
            {
                //关闭Agent的时候, 肯定要从Server的Agent集合中移除当前的Agent
                m_server.RemoveAgent(this);
            }
        }
        
        //Agent发送数据包
        public void SendPacket(byte[] datas)
        {
            if(Valid() == false)
            {
                return;
            }
            lock (m_sendBufferList)
            {
                SendBuffer sb = new SendBuffer()
                {
                    position = 0,
                    buffer = datas,
                };
                m_sendBufferList.Add(sb); //需要发送的数据包先放入到发送缓存列表中
                if(m_sendBufferList.Count == 1)
                {
                    try
                    {
                        m_socket.BeginSend(sb.buffer, sb.position, sb.Size, SocketFlags.None, OnSendCallback, m_socket);
                    }
                    catch(Exception exception)
                    {
                        Debug.LogError(exception.Message);
                        Close();
                    }
                }
            }
        }
        private void OnSendCallback(IAsyncResult ar)
        {
            int num = 0;
            try
            {
                num = m_socket.EndSend(ar);
            }
            catch(Exception exception)
            {
                num = 0;
                Close();
                Debug.LogError(exception.Message);
                return;
            }
            lock (m_sendBufferList)
            {
                if (Valid())
                {
                    var sb = m_sendBufferList[0];
                    SendBuffer nextSb = null;
                    if(sb.Size == num)
                    {
                        //表明SendBuffer中的数据全部发送完成
                        m_sendBufferList.RemoveAt(0);
                        if(m_sendBufferList.Count > 0)
                        {
                            nextSb = m_sendBufferList[0];
                        }
                    }
                    else if(sb.Size > num)
                    {
                        //表明SendBuffer中的数据没有全部发送出去
                        sb.position += num;
                        nextSb = m_sendBufferList[0];
                    }
                    else
                    {
                        //表明上一个SendBuffer数据包发送错误，我们就不管上一个数据包了
                        m_sendBufferList.RemoveAt(0);
                        if(m_sendBufferList.Count > 0)
                        {
                            nextSb = m_sendBufferList[0];
                        }
                    }
                    if(nextSb != null)
                    {
                        try
                        {
                            m_socket.BeginSendTo(nextSb.buffer, nextSb.position, nextSb.Size, SocketFlags.None, m_endPoint, OnSendCallback, null);
                        }
                        catch(Exception exception)
                        {
                            //异步发送数据失败，就关闭Agent的Socket连接
                            Debug.LogError(exception.Message);
                            Close();
                        }
                    }
                }
            }
        }
        #endregion
    }
}
