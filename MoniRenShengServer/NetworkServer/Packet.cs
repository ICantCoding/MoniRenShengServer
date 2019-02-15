

namespace TDFramework.Network
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class Packet
    {
        public System.Byte flag; //数据报文开头一字节标识0xcc                              1byte
        public System.UInt32 msgLen = 0;//数据报文的总长度字节数                           4byte
        public System.UInt32 flowId;//数据报文的唯一标识，递增                             4byte
        public System.Byte moduleId;//数据报文对应ProtoBuff中Message的模块Id               1byte
        public System.UInt16 msgId;//数据报文对应ProtoBuff中Module中的具体MessageId        2byte
        public System.UInt32 responseTime;//服务器响应报文在服务器的时间                    4byte
        public System.Int16 responseFlag;//服务器响应报文的状态，正确还是错误               2byte
        public byte[] data;//服务器响应报文的具体数据内容

        public string protobufMessageClassName = string.Empty;   //用来记录当前Packet对应的Protobuf的Message类名字

        public Packet(byte f, uint len, uint fid, byte moduleid, ushort msgid, uint restime, short resflag, byte[] bytes)
        {
            this.flag = f;
            this.msgLen = len;
            this.flowId = fid;
            this.moduleId = moduleid;
            this.msgId = msgid;
            this.responseTime = restime;
            this.responseFlag = resflag;
            this.data = bytes;

            //this.protobufMessageClassName = ProtoBufModuleMgr.Instance.GetMessageName(moduleId, msgId);
        }
        public Packet()
        {

        }
    }
}