using System.IO;
using System.ServiceModel;

namespace SharedChat
{
    [ServiceContract]
    public interface ChatCallback
    {
        [OperationContract(IsOneWay = true)]
        void ChannelListUpdated(ChannelInfo[] channels);

        [OperationContract(IsOneWay = true)]
        void MemberListUpdated(string ChannelName, string userID, bool joined);

        [OperationContract(IsOneWay = true)]
        void MessageReceived(Chat message);

        [OperationContract(IsOneWay = true)]
        void PrivateMessageReceived(PrivateNessage message);

        [OperationContract(IsOneWay = true)]
        void FileShared(SharedFile file);
    }
}
