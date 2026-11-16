using System.Collections.Generic;
using SharedChat;


namespace ChatServer
{
    public class Server
    {
        public List<UserInfo> Users { get; private set; }

        public List<ChannelInfo> Channels { get; private set; }

        public List<FileData> Files { get; private set; }

        public List<SharedFile> FileInformation { get; private set; }
        
        public List<PendingUserUpdate> UserUpdates { get; private set; }

        public List<CallbackInfo> CallBacks { get; private set; }

        public object StateLock { get; private set; }

        public Server()
        {
            Users = new List<UserInfo>();
            Channels = new List<ChannelInfo>();
            Files = new List<FileData>();
            FileInformation = new List<SharedFile>();
            UserUpdates = new List<PendingUserUpdate>();
            CallBacks = new List<CallbackInfo>();
            StateLock = new object();

        }   
    }

    public class PendingUserUpdate
    {
        public string UserID { get; set; }
        public ServerUpdate Update { get; set; }
        public PendingUserUpdate()
        {
            Update = new ServerUpdate();
        }
    }

    public class CallbackInfo
    {
        public string UserID { get; set; }
        public ChatCallback Callback { get; set; }
    }
}
