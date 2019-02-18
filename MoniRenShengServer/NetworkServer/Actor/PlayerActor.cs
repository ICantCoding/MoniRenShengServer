
namespace TDFramework.Network
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Text;

    //PlayerActor跟Agent相互绑定在一起
    public class PlayerActor : Actor
    {
        #region 字段
        private uint m_agentId; //Agent的Id
        private Agent m_agent; //Agent
        private WorldActor m_worldActor; //世界Actor
        #endregion

        #region 构造函数
        public PlayerActor(uint id)
        {
            m_agentId = id;
            NetworkServer server = ActorManager.Instance.GetActor<NetworkServer>();
            m_agent = server.GetAgent(m_agentId);
            m_agent.Actor = this;
            m_worldActor = ActorManager.Instance.GetActor<WorldActor>();
        }
        #endregion

        #region 重载方法
        protected override async Task ReceiveMsg(ActorMessage actorMsg)
        {
            Debug.Log("PlayerActor Receive Message....");
            if (actorMsg == null) return;
            if (!string.IsNullOrEmpty(actorMsg.msg))
            {
                //处理ActorMessage中携带string内容的情况

            }
            if(actorMsg.packet != null)
            {
                //处理ActorMessage中携带Packet内容的情况

            }

        }
        #endregion
    }
}



