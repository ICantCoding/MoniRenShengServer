

namespace TDFramework.Network
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    public class ActorMessage
    {
        public string msg;
        public Packet packet;
        public object obj;
        public object obj1;
    }

    public class Actor
    {
        #region 字段
        //邮箱 消息队列
        protected readonly ActorSynchronizationContext m_messageQueue = new ActorSynchronizationContext();
        protected int m_Id; //地址
        protected bool m_isStop = false; //停止
        public BufferBlock<ActorMessage> m_mailbox = new BufferBlock<ActorMessage>();
        #endregion

        #region 属性
        public int Id
        {
            get { return m_Id; }
            set { m_Id = value; }
        }
        #endregion

        #region 构造函数
        public Actor()
        {

        }
        #endregion

        //分发消息，Agent通过Agent的Actor发送消息出来，该Actor就接收到这个消息, 由Actor的ReceiveMsg来处理.
        private async Task Dispatch()
        {
            while (!m_isStop)
            {
                Debug.Log("Actor Dispatch Thread Id: " + Thread.CurrentThread.ManagedThreadId);
                ActorMessage msg = await m_mailbox.ReceiveAsync<ActorMessage>();
                Debug.Log("ThreadId receive " + this.GetType() + " id " + Thread.CurrentThread.ManagedThreadId);
                Debug.Log("Receive message: " + msg);
                await ReceiveMsg(msg); //虚方法，Actor子类方法，有具体的重写实现，再做相应的具体功能
            }
        }
        //虚方法，用于子类重写
        protected virtual async Task ReceiveMsg(ActorMessage msg)
        {
            await Task.FromResult(default(object));  
        }
        protected void RunTask(System.Func<Task> cb)
        {
            Debug.Log("Actor RunTask Thread Id: " + Thread.CurrentThread.ManagedThreadId);
            //这个Context应该是主线程的SyncContext吧, 那么应该为null, 确实也为null. 
            //此时TaskScheduler.FromCurrentSynchronizationContext()是获取不到的, 如果去获取会报异常
            var surroundContext = SynchronizationContext.Current;
            //给主线程设置SynchronizationContext, 就可以获取到TaskScheduler.FromCurrentSynchronizationContext()
            //m_messageQueue本身就是一个SynchronizationContext
            SynchronizationContext.SetSynchronizationContext(m_messageQueue);
            //Task在线程池中获取一个线程，开启该线程来处理Dispatch的死循环
            //cb函数，会经过FromCurrentSynchronizationContext()得到的m_messageQueue调用Post方法来处理cb
            Task.Factory.StartNew(cb, CancellationToken.None, TaskCreationOptions.DenyChildAttach,
                TaskScheduler.FromCurrentSynchronizationContext());
            SynchronizationContext.SetSynchronizationContext(surroundContext);
        }
        public virtual void Init()
        {
            Debug.Log("Actor Init Thread Id: " + Thread.CurrentThread.ManagedThreadId);
            RunTask(Dispatch);
        }
        public void Stop()
        {
            m_isStop = true;
        }

        #region Actor之间通信
        //Actor给Actor发送string类型的信息
        public void SendMsg(string msg)
        {
            var m = new ActorMessage();
            m.msg = msg;
            m_mailbox.SendAsync(m); 
        }
        //Actor给Actor发送Packet类型的信息
        public void SendMsg(Packet packet)
        {
            var m = new ActorMessage() { packet = packet };
            m_mailbox.SendAsync(m); //BufferBlock异步发送，会在Dispatch函数中异步接收
        }
        //Actor给Actor发送ActorMessage类型的信心
        public void SendMsg(ActorMessage msg)
        {
            m_mailbox.SendAsync(msg);
        }
        #endregion  
    }

    public class ActorManager
    {
        #region 字段
        public static ActorManager Instance;
        Dictionary<int, Actor> m_actorDict; //根据Actor的Id来缓存Actor
        Dictionary<Type, Actor> m_actorType; //根据Actor的Type来缓存Actor

        private int m_actorId = 0; //进行原子操作递增， 用于Actor的唯一标识
        private bool m_isStop = false;
        #endregion

        #region 构造函数
        public ActorManager()
        {
            Instance = this;
            m_actorDict = new Dictionary<int, Actor>();
            m_actorType = new Dictionary<Type, Actor>();
        }
        #endregion

        #region 方法
        public int AddActor(Actor actor, bool addType = false)
        {
            if (m_isStop)
            {
                return -1;
            }
            int actorId = Interlocked.Increment(ref m_actorId); //原子操作
            lock (m_actorDict)
            {
                m_actorDict.Add(actorId, actor);
                if (addType)
                {
                    m_actorType.Add(actor.GetType(), actor);
                }
            }
            actor.Id = actorId;
            Debug.Log("AddActor Thread Id: " + Thread.CurrentThread.ManagedThreadId);
            actor.Init();       //向ActorManager添加Actor的时候，就初始化Actor，并启动Actor的任务
            return actorId;
        }
        public void RemoveActor(int actorId)
        {
            lock (m_actorDict)
            {
                if (m_actorDict.ContainsKey(actorId))
                {
                    Actor actor = m_actorDict[actorId];
                    if (m_actorType.ContainsKey(actor.GetType()))
                    {
                        Actor actor2 = m_actorType[actor.GetType()];
                        if(actor2 == actor)
                        {
                            m_actorType.Remove(actor.GetType());
                        }
                    }
                    m_actorDict.Remove(actorId);
                }
            }
        }
        public Actor GetActor(int actorId)
        {
            Actor actor = null;
            lock (m_actorDict)
            {
                m_actorDict.TryGetValue(actorId, out actor);
            }
            return actor;
        }
        public T GetActor<T>() where T : Actor
        {
            T actor = null;
            lock (m_actorDict)
            {
                Actor temp = null;
                m_actorType.TryGetValue(typeof(T), out temp);
                actor = (T)temp;
            }
            return actor;
        }
        public void Stop()
        {
            m_isStop = true;
            lock (m_actorDict)
            {
                foreach(var actor in m_actorDict)
                {
                    actor.Value.Stop();
                }
            }
        }
        #endregion
    }
}