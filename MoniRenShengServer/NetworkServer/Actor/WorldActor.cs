

namespace TDFramework.Network
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Text;

    //用来存放所有的玩家记录
    public class WorldActor : Actor
    {
        #region 字段
        //世界WorldActor中保存管理了PlayerActor
        private List<PlayerActor> m_playerActorList = new List<PlayerActor>();
        #endregion

        #region 构造函数
        public WorldActor()
        {

        }
        #endregion

        #region 方法
        private async Task UpdateWorld()
        {
            while (!m_isStop)
            {
                await Task.Delay(1000); //每秒定时更新一次信息

            }
        }
        public void AddPlayerActor(PlayerActor playerActor)
        {
            if (playerActor == null) return;
            if(m_playerActorList.Contains(playerActor) == false)
            {
                m_playerActorList.Add(playerActor);
            }
        }
        public void RemovePlayerActor(PlayerActor playerActor)
        {
            if (playerActor == null) return;
            if (m_playerActorList.Contains(playerActor))
            {
                m_playerActorList.Remove(playerActor);
            }
        }
        public void RemovePlayerActor(uint agentId)
        {
            PlayerActor actor = GetPlayerActor(agentId);
            if(actor != null)
            {
                RemovePlayerActor(actor);
            }
        }
        public PlayerActor GetPlayerActor(uint agentId)
        {
            foreach(var temp in m_playerActorList)
            {
                if(temp.Id == agentId)
                {
                    return temp;
                }
            }
            return null;
        }
        #endregion

        #region 重载方法
        protected override async Task ReceiveMsg(ActorMessage msg)
        {
        }
        public override void Init()
        {
            base.Init();
            RunTask(UpdateWorld); //用于定时更新需要的信息
        }
        #endregion

    }
}