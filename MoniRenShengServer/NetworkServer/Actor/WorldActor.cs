

namespace TDFramework.Network
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Text;

    //用来存放所有的玩家记录
    class WorldActor : Actor
    {
        #region 字段
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
                await Task.Delay(1000);
            }
        }
        private PlayerActor GetPlayerActor(int id)
        {
            foreach(var temp in m_playerActorList)
            {
                if(temp.Id == id)
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
            //RunTask(UpdateWorld);
        }
        #endregion

    }
}