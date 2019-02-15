
namespace TDFramework.Network
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Text;

    //PlayerActor跟Agent相互绑定在一起
    class PlayerActor : Actor
    {
        #region 字段
        private uint m_agentId; //Agent的Id
        private Agent m_agent; //Agent
        #endregion

        #region 构造函数
        public PlayerActor(uint id)
        {
            m_agentId = id;
            NetworkServer server = ActorManager.Instance.GetActor<NetworkServer>();
            m_agent = server.GetAgent(m_agentId);
            m_agent.Actor = this;
        }
        #endregion

        #region 重载方法
        protected override async Task ReceiveMsg(ActorMessage msg)
        {
            Debug.Log("PlayerActor Receive Message....");
        }
        #endregion
    }
}



