

namespace TDFramework.Network
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class WatchDogActor : Actor
    {
        #region 重写
        protected override async System.Threading.Tasks.Task ReceiveMsg(ActorMessage msg)
        {
            Debug.Log("Watch Dog Actor Receive ActorMessage...");

            if (!string.IsNullOrEmpty(msg.msg))
            {
                var cmds = msg.msg.Split(' ');
                if (cmds[0] == "open")
                {
                    var agentId = System.Convert.ToUInt32(cmds[1]);
                    var act = new PlayerActor(agentId); //创建PlayerActor
                    Debug.Log("CreateActor " + agentId);
                    ActorManager.Instance.AddActor(act);
                }
                else if (cmds[0] == "close")
                {
                    var agentId = System.Convert.ToInt32(cmds[1]);
                }
            }
        }
        #endregion
    }
}
