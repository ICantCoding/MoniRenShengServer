using System;
using System.Collections.Generic;
using System.Text;

namespace TDFramework.Network
{
    class MessageReader
    {

        enum READ_STATE
        {
            READ_STATE_FLAG = 0,        //数据报文有效性标志 int8 0xcc 1个字节
            READ_STATE_MSGLEN = 1,      //数据报文长度 UInt32 4个字节
            READ_STATE_FLOWID = 2,      //数据报文编号 UInt32 4个字节
            READ_STATE_MODULEID = 3,    //protobuff协议所在的模块编号 int8 1个字节
            READ_STATE_MSGID = 4,       //protobuff协议所在模块中Message编号 int16 2个字节 
            READ_STATE_BODY = 7,        //数据报文实际真实传输内容， 二进制数据
        }
        public delegate void MessageHandler(Packet msg);

        #region 字段
        private System.Byte m_flag;
        private System.UInt32 m_msglen;
        private System.UInt32 m_flowId;
        private System.Byte m_moduleId;
        private System.UInt16 m_msgId;
        private READ_STATE m_state = READ_STATE.READ_STATE_FLAG;
        private System.UInt32 m_expectSize = 1;

        private MemoryStream m_stream = new MemoryStream();
        public MessageHandler m_messageHandler = null;
        #endregion

        #region 构造函数
        public MessageReader()
        {
            m_expectSize = 1;
            m_state = READ_STATE.READ_STATE_FLAG;
        }
        public MessageReader(MessageHandler handler) : this()
        {
            m_messageHandler = handler;
        }
        #endregion

        #region 方法
        public void Process(byte[] datas, System.UInt32 length)
        {
            System.UInt32 totallength = 0;
            while(length > 0 && m_expectSize > 0)
            {
                if(m_state == READ_STATE.READ_STATE_FLAG)
                {
                    if(length >= m_expectSize)
                    {
                        Array.Copy(datas, totallength, m_stream.Data, m_stream.wpos, m_expectSize);
                        totallength += m_expectSize;
                        m_stream.wpos += m_expectSize;
                        length -= m_expectSize;

                        m_flag = m_stream.ReadByte();
                        m_stream.Clear();

                        m_state = READ_STATE.READ_STATE_MSGLEN;
                        m_expectSize = 4;
                    }
                    else
                    {
                        Array.Copy(datas, totallength, m_stream.Data, m_stream.wpos, length);
                        m_stream.wpos += length;
                        m_expectSize -= length;
                        break;
                    }
                }
                else if(m_state == READ_STATE.READ_STATE_MSGLEN)
                {
                    if(length >= m_expectSize)
                    {
                        Array.Copy(datas, totallength, m_stream.Data, m_stream.wpos, m_expectSize);
                        totallength += m_expectSize;
                        m_stream.wpos += m_expectSize;
                        length -= m_expectSize;

                        m_msglen = m_stream.ReadUInt32();
                        m_stream.Clear();

                        m_state = READ_STATE.READ_STATE_FLOWID;
                        m_expectSize = 4;
                    }
                    else
                    {
                        Array.Copy(datas, totallength, m_stream.Data, m_stream.wpos, length);
                        m_stream.wpos += length;
                        m_expectSize -= length;
                        break;
                    }
                }
                else if(m_state == READ_STATE.READ_STATE_FLOWID)
                {
                    if (length >= m_expectSize)
                    {
                        Array.Copy(datas, totallength, m_stream.Data, m_stream.wpos, m_expectSize);
                        totallength += m_expectSize;
                        m_stream.wpos += m_expectSize;
                        length -= m_expectSize;

                        m_msglen = m_stream.ReadUInt32();
                        m_stream.Clear();

                        m_state = READ_STATE.READ_STATE_MODULEID;
                        m_expectSize = 1;
                    }
                    else
                    {
                        Array.Copy(datas, totallength, m_stream.Data, m_stream.wpos, length);
                        m_stream.wpos += length;
                        m_expectSize -= length;
                        break;
                    }
                }
                else if(m_state == READ_STATE.READ_STATE_MSGID)
                {
                    if (length >= m_expectSize)
                    {
                        Array.Copy(datas, totallength, m_stream.Data, m_stream.wpos, m_expectSize);
                        totallength += m_expectSize;
                        m_stream.wpos += m_expectSize;
                        length -= m_expectSize;

                        m_msglen = m_stream.ReadUInt16();
                        m_stream.Clear();

                        m_state = READ_STATE.READ_STATE_BODY;
                        m_expectSize = m_msglen - 4 - 1 - 2; //msglen大小只计算了msglen后边字节的大小
                    }
                    else
                    {
                        Array.Copy(datas, totallength, m_stream.Data, m_stream.wpos, length);
                        m_stream.wpos += length;
                        m_expectSize -= length;
                        break;
                    }
                }
                if(m_state == READ_STATE.READ_STATE_BODY)
                {
                    if(length >= m_expectSize)
                    {
                        Array.Copy(datas, totallength, m_stream.Data, m_stream.wpos, m_expectSize);
                        totallength += m_expectSize;
                        m_stream.wpos += m_expectSize;
                        length -= m_expectSize;

                        string content = Encoding.UTF8.GetString(m_stream.Data, 0, (int)m_stream.wpos);
                        Debug.Log("服务器接收的数据并解析: " + content);
                        Packet p = new Packet(m_flag, m_msglen, m_flowId, m_moduleId, m_msgId, 0, 0, m_stream.Data);
                        if(m_messageHandler != null)
                        {
                            m_messageHandler(p);
                        }
                        m_stream.Clear();
                        m_state = READ_STATE.READ_STATE_FLAG;
                        m_expectSize = 1;
                    }
                    else
                    {
                        Array.Copy(datas, totallength, m_stream.Data, m_stream.wpos, length);
                        m_stream.wpos += length;
                        m_expectSize -= length;
                        break;
                    }
                }
            }
        }
        #endregion


    }
}
