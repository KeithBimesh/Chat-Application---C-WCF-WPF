using System.ServiceModel;

namespace SharedChat
{
    [ServiceContract]
    public interface ChatServiceInterface
    {
        [OperationContract]
        ServiceResult SignIn(string userID);

        [OperationContract]
        ServiceResult SignOut(string userID);

        [OperationContract]
        ChannelInfo[] GetChannels();

        [OperationContract]
        ServiceResult CreateChannel(string userID, string channelName);

        [OperationContract]
        ServiceResult JoinChannel(string userID, string channelName);

        [OperationContract]
        ServiceResult LeaveChannel(string userID);

        [OperationContract]
        ServiceResult SendMessage(string userID, string message);

        [OperationContract]
        ServiceResult SendMessagePrivate(string senderID, string receiverID, string message);

        [OperationContract]
        ServiceResult UploadFile(string userID, string fileName, byte[] content);

        [OperationContract]
        FileData DownloadFile(string userID, string fileID);

        [OperationContract]
        ServerUpdate GetUpdates(string userID);

        [OperationContract]
        ServiceResult RegisterCallBack(string userID);

        [OperationContract]
        ServiceResult UnregisterCallBack(string userID);

    }

    [ServiceContract(
        CallbackContract = typeof(ChatCallback))]
    public interface DuplexChatService : ChatServiceInterface
    {

    }
}
