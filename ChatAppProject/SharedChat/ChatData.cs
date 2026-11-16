using System;
using System.Collections.Generic;
using System.Runtime.Serialization;



namespace SharedChat
{
    [DataContract]
    public class UserInfo
    {
        [DataMember]
        public string UserID { get; set; }

        [DataMember]
        public string ChannelName { get; set; }
    }

    [DataContract]
    public class ChannelInfo
    {
        public ChannelInfo()
        {
            Members = new List<string>();
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<string> Members { get; set; }
    }

    [DataContract]
    public class Chat
    {
        [DataMember]
        public string SenderID { get; set; }

        [DataMember]
        public string ChannelName { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public DateTime SentTime { get; set; }

    }

    [DataContract]
    public class PrivateNessage
    {
        [DataMember]
        public string SenderID { get; set; }

        [DataMember]
        public string ReceiverID { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public DateTime SentTime { get; set; }
    }

    [DataContract]

    public class SharedFile
    {
        [DataMember]
        public string FileID { get; set; }

        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public string SharedBy { get; set; }

        [DataMember]
        public string ChannelName { get; set; }

        [DataMember]
        public long FileSize { get; set; }

        [DataMember]
        public DateTime SharedTime { get; set; } 

     }

    [DataContract]
    public class FileData
    {
        [DataMember]
        public string FileID { get; set; }

        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public byte[] Content { get; set; }
    }

    [DataContract]
    public class ServerUpdate
    {
        public ServerUpdate()
        {
            Channels = new List<ChannelInfo>();
            Messages = new List<Chat>();
            PrivateMessages = new List<PrivateNessage>();
            SharedFiles = new List<SharedFile>();
            Members = new List<string>();
        }

        [DataMember]
        public List<ChannelInfo> Channels { get; set; }

        [DataMember]
        public List<Chat> Messages { get; set; }

        [DataMember]
        public List<PrivateNessage> PrivateMessages { get; set; }

        [DataMember]
        public List<SharedFile> SharedFiles { get; set; }

        [DataMember]
        public List<string> Members { get; set; }
    }

}
