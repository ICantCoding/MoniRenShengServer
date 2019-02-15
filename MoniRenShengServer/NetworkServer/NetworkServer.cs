

namespace TDFramework.Network
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Net;
    using System.Threading;

    public class NetworkServer : Actor
    {
        #region 字段
        private TcpListener m_tcpListener = null;
        private int m_tcpPort = 0;  //tcp连接服务器端口号

        private Thread m_thread = null;
        //对Agent使用字典管理, Key为Agent的Id, Value为Agent对象
        private Dictionary<uint, Agent> m_agentDict = new Dictionary<uint, Agent>();

        private WatchDogActor m_dogActor; //看门狗Actor
        #endregion

        #region 属性
        public Thread ListenerThread
        {
            get { return m_thread; }
        }
        #endregion  

        #region 构造函数
        public NetworkServer()
        {
            m_dogActor = ActorManager.Instance.GetActor<WatchDogActor>();
        }
        #endregion

        #region 方法
        public void Close()
        {
            if (m_tcpListener != null)
            {
                m_tcpListener.Stop();
                m_tcpListener = null;
            }
        }
        public bool Start(int tcpPort)
        {
            try
            {
                m_tcpPort = tcpPort;
                m_tcpListener = new TcpListener(IPAddress.Any, m_tcpPort);
                m_tcpListener.Start(50); //设置最大挂载连接数为50
            }
            catch (System.Exception e)
            {
                Debug.Log("TcpListener Init Fail. " + e.Message);
                return false;
            }
            m_thread = new Thread(new ThreadStart(ThreadFunction));
            m_thread.Start(); //开启一个子线程专门用于监听客户端的连接
            return true;
        }
        private void ThreadFunction()
        {
            while (true)
            {
                //确定是否有挂起的连接请求, Pending()返回true, 表示有从客户端来的连接请求
                if (m_tcpListener != null && m_tcpListener.Pending())
                {
                    Socket socket = m_tcpListener.AcceptSocket();
                    AddAgent(socket);
                }
                Thread.Sleep(1);
            }
        }
        #endregion

        #region 数据管理方法
        public void AddAgent(Socket socket)
        {
            Agent agent = new Agent(socket);
            agent.DogActor = m_dogActor; //设置Agent的看门狗Actor
            agent.Server = this;
            lock (m_agentDict)
            {
                m_agentDict.Add(agent.Id, agent);
            }
            agent.StartReceive();
        }
        public void RemoveAgent(Agent agent)
        {
            if (m_agentDict.ContainsKey(agent.Id))
            {
                lock (m_agentDict)
                {
                    m_agentDict.Remove(agent.Id);
                }
            }
        }
        public Agent GetAgent(uint agentId)
        {
            Agent agent = null;
            lock (m_agentDict)
            {
                m_agentDict.TryGetValue(agentId, out agent);
            }
            return agent;
        }
        #endregion
    }
}
