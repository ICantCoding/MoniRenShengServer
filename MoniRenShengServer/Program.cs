using System;
using System.Threading;
using TDFramework.Network;

namespace MoniRenShengServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Log("Server Start...");
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelEventHandler);

            //初始化ActorManager, 全局管理所有的Actor
            ActorManager actorManager = new ActorManager();
            //初始化看门狗Actor
            Debug.Log("Main Thread Id: " + Thread.CurrentThread.ManagedThreadId);
            WatchDogActor dogActor = new WatchDogActor();
            ActorManager.Instance.AddActor(dogActor);
            ////初始化WorldActor
            //WorldActor worldActor = new WorldActor();
            //ActorManager.Instance.AddActor(worldActor);
            

            ////启动NetworkServerActor
            //NetworkServer server = new NetworkServer();
            //ActorManager.Instance.AddActor(server, true); //NetworkServer也是一个Actor, 需要被ActorManager管理
            //server.Start(10086); //服务器Tcp监听10086端口
            //server.ListenerThread.Join(); //阻塞，等待监听的线程（监听线程是一个死循环线程）

            Debug.Log("Server End...");
            Console.ReadLine();
        }

        private static void ConsoleCancelEventHandler(object sender, ConsoleCancelEventArgs e)
        {
            Debug.Log("Ctrl + C To Server Stop...");
        }
    }
}








