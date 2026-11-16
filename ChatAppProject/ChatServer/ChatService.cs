using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ServiceModel;
using SharedChat;

namespace ChatServer
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerSession,  
        ConcurrencyMode = ConcurrencyMode.Multiple)]

    public class ChatService : DuplexChatService
    {
        private static Server state = new Server();

        private string CurrentUserID;

        private bool cleanUp;

        private static readonly string[] AllowedExtensions =
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".bmp",
            ".txt"
        };

        private const long FileSize = 2 * 1024 * 1024;

        public ChatService()
        {
            cleanUp = false;
            if (OperationContext.Current != null)
            {
                OperationContext.Current.InstanceContext.Closed += InstanceContext_Closed;

                OperationContext.Current.InstanceContext.Faulted += InstanceContext_Faulted;
            }
        }

        private void InstanceContext_Closed(object sender, EventArgs e)
        {
            CleanupDisconnectedUser();
        }

        private void InstanceContext_Faulted(object sender, EventArgs e)
        {
            CleanupDisconnectedUser();
        }

        private void CleanupDisconnectedUser()
        {
            if(cleanUp)
            {
                return;
            }

            cleanUp = true;

            if(string.IsNullOrEmpty(CurrentUserID))
            {
                return;
            }

            string channelName = null;

            lock (state.StateLock)
            {
                UserInfo user = FindUser(CurrentUserID);

                if(user == null)
                {
                    return;
                }
                channelName = user.ChannelName;

                RemoveUserFromChannel(user);

                RemovePendingUpdate(CurrentUserID);

                RemoveCallback(CurrentUserID);

                state.Users.Remove(user);
            }

            if(!string.IsNullOrEmpty(channelName))
            {
                NotifyMembersChanged(channelName, CurrentUserID, false);
            }

            NotifyChannelListChanged();
        }

        public ServiceResult SignIn(string userID)
        {
            if(string.IsNullOrWhiteSpace(userID))
            {
                return ServiceResult.Fail("User ID can't be empty");
            }

            userID = userID.Trim();

            lock(state.StateLock)
            {
                UserInfo existing = FindUser(userID);

                if(existing != null)
                {
                    return ServiceResult.Fail("The user id is already signed in");
                }

                UserInfo newUser = new UserInfo
                {
                    UserID = userID,
                    ChannelName = null
                };

                state.Users.Add(newUser);

                PendingUserUpdate pending = new PendingUserUpdate
                {
                    UserID = userID
                };

                state.UserUpdates.Add(pending);

                CurrentUserID = userID;
            }

            NotifyChannelListChanged();

            return ServiceResult.Pass("Signed in successfully");
        }

        public ServiceResult SignOut(string userID)
        {
            string channelName = null;

            lock(state.StateLock)
            {
                UserInfo user = FindUser(userID);
                
                if(user == null)
                {
                    return ServiceResult.Fail("User is not signed in");
                }

                channelName = user.ChannelName;

                RemoveUserFromChannel(user);
                RemovePendingUpdate(userID);
                RemoveCallback(userID);
                state.Users.Remove(user);
            }

            cleanUp = true;

            if(!string.IsNullOrEmpty(channelName))
            {
                NotifyMembersChanged(channelName, userID, false);
            }

            NotifyChannelListChanged();

            return ServiceResult.Pass("Signed out successfully");
        }

        public ChannelInfo[] GetChannels()
        {
            lock(state.StateLock)
            {
                return state.Channels.Select(CopyChannel).ToArray();
            }
        }

        public ServiceResult CreateChannel(string userID, string channelName)
        {
            if(string.IsNullOrWhiteSpace(channelName))
            {
                return ServiceResult.Fail("Channel name is empty");
            }

            channelName = channelName.Trim();

            lock(state.StateLock)
            {
                UserInfo user = FindUser(userID);

                if(user == null)
                {
                    return ServiceResult.Fail("You must be signed in");
                }

                ChannelInfo existing = FindChannel(channelName);

                if(existing != null)
                {
                    return ServiceResult.Fail("That channel already exists");
                }

                ChannelInfo channel = new ChannelInfo
                {
                    Name = channelName
                };

                state.Channels.Add(channel);

                AddChannelListUpdateForAllUsers();
            }

            NotifyChannelListChanged();

            return ServiceResult.Pass("Channel created successfully");
        }

        public ServiceResult JoinChannel(string userID, string channelName)
        {
            if(string.IsNullOrWhiteSpace(channelName))
            {
                return ServiceResult.Fail("Channel name is empty");
            }

            channelName = channelName.Trim();

            string oldChannel = null;

            lock(state.StateLock)
            {
                UserInfo user = FindUser(userID);

                if(user == null)
                {
                    return ServiceResult.Fail("You must be signed in");
                }

                ChannelInfo channel = FindChannel(channelName);

                if(channel == null)
                {
                    return ServiceResult.Fail("Channel doesn't exist");
                }

                oldChannel = user.ChannelName;

                if(oldChannel == channelName)
                {
                    return ServiceResult.Fail("You are already in this channel");
                }

                if(!string.IsNullOrEmpty(oldChannel))
                {
                    ChannelInfo old = FindChannel(oldChannel);

                    if(oldChannel != null)
                    {
                        old.Members.Remove(userID);
                    }             
                }

                channel.Members.Add(userID);

                user.ChannelName = channelName;
            }

            if(!string.IsNullOrEmpty(oldChannel))
            {
                NotifyMembersChanged(oldChannel, userID, false);
            }

            NotifyMembersChanged(channelName, userID, true);

            return ServiceResult.Pass("Joined successfully");
        }

        public ServiceResult LeaveChannel(string userID)
        {
            string channelName;

            lock(state.StateLock)
            {
                UserInfo user = FindUser(userID);

                if(user == null)
                {
                    return ServiceResult.Fail("You are not signed in");
                }

                channelName = user.ChannelName;

                if(string.IsNullOrEmpty(channelName))
                {
                    return ServiceResult.Fail("You are not in a channel");
                }

                ChannelInfo channel = FindChannel(channelName);

                if (channel != null)
                {
                    channel.Members.Remove(userID);
                }

                user.ChannelName = null;
            }

            NotifyMembersChanged(channelName, userID, false);

            return ServiceResult.Pass("You left from the channel");
        }

        public ServiceResult SendMessage(string userID, string message)
        {
            if(string.IsNullOrWhiteSpace(message))
            {
                return ServiceResult.Fail("Message can't be empty");
            }

            Chat chat;

            List<string> recipients;

            lock(state.StateLock)
            {
                UserInfo user = FindUser(userID);

                if(user == null)
                {
                    return ServiceResult.Fail("You must be signed in");
                }

                string ChannelName = user.ChannelName;

                if (string.IsNullOrEmpty(ChannelName))
                {
                    return ServiceResult.Fail("You must join a channel");
                }

                ChannelInfo channel = FindChannel(ChannelName);

                if(channel == null)
                {
                    return ServiceResult.Fail("Channel is not exists");
                }

                chat = new Chat
                {
                    SenderID = userID,
                    ChannelName = ChannelName,
                    Message = message,
                    SentTime = DateTime.Now
                };

                recipients = new List<string>(channel.Members);
            }

            foreach(string recipient in recipients)
            {
                AddMessageUpdate(recipient, chat);

                PushMessage(recipient, chat);
            }

            return ServiceResult.Pass();
        }

        public ServiceResult SendMessagePrivate(string senderID, string recipientID, string message)
        {
            if(string.IsNullOrWhiteSpace(message))
            {
                return ServiceResult.Fail("Message is empty");
            }

            PrivateNessage privateMessage;

            lock(state.StateLock)
            {
                UserInfo sender = FindUser(senderID);

                UserInfo recipient = FindUser(recipientID);

                if(sender == null)
                {
                    return ServiceResult.Fail("User is not signed in");
                }

                if(recipient == null)
                {
                    return ServiceResult.Fail("Recipient is not signed in");
                }

                if(string.IsNullOrEmpty(sender.ChannelName))
                {
                    return ServiceResult.Fail("you must be in a channel");
                }

                if(sender.ChannelName != recipient.ChannelName)
                {
                    return ServiceResult.Fail("Private messages only can send to a member of the same channel ");
                }

                privateMessage = new PrivateNessage
                {
                    SenderID = senderID,
                    ReceiverID = recipientID,
                    Message = message,
                    SentTime = DateTime.Now
                };
            }

            AddPrivateMessageUpdate(senderID, privateMessage);
            AddPrivateMessageUpdate(recipientID, privateMessage);

            PushPrivateMessage(recipientID, privateMessage);

            return ServiceResult.Pass();
        }

        public ServiceResult UploadFile(string userID, string fileName, byte[] content)
        {
            if(string.IsNullOrWhiteSpace(fileName))
            {
                return ServiceResult.Fail("File is empty");
            }

            if(content == null || content.Length == 0)
            {
                return ServiceResult.Fail("Selected file is empty");
            }

            if(content.Length > FileSize)
            {
                return ServiceResult.Fail("File is too large");
            }

            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            if(!AllowedExtensions.Contains(extension))
            {
                return ServiceResult.Fail("That file type is not allowed");
            }

            SharedFile fileInfo;

            List<string> recipients;

            lock(state.StateLock)
            {
                UserInfo user = FindUser(userID);

                if(user == null)
                {
                    return ServiceResult.Fail("You must be signed in");
                }

                string channelName = user.ChannelName;

                if(string.IsNullOrEmpty(channelName))
                {
                    return ServiceResult.Fail("You must join a channel");
                }

                ChannelInfo channel = FindChannel(channelName);

                if(channel == null)
                {
                    return ServiceResult.Fail("Channel is not exists");
                }

                string fileID = Guid.NewGuid().ToString();

                fileInfo = new SharedFile
                {
                    FileID = fileID,
                    FileName = Path.GetFileName(fileName),
                    SharedBy = userID,
                    ChannelName = channelName,
                    FileSize = content.Length,
                    SharedTime = DateTime.Now
                };

                FileData file = new FileData
                {
                    FileID = fileID,
                    FileName = fileInfo.FileName,
                    Content = content
                };

                state.Files.Add(file);

                state.FileInformation.Add(fileInfo);

                recipients = new List<string>(channel.Members);
            }

            foreach(string recipient in recipients)
            {
                AddFileUpdate(recipient, fileInfo);

                PushFile(recipient, fileInfo);
            }

            return ServiceResult.Pass("File shared successfully");
        }

        public FileData DownloadFile(string userID, string fileID)
        {
            lock(state.StateLock)
            {
                UserInfo user = FindUser(userID);

                if(user == null)
                {
                    return null;
                }

                SharedFile info = state.FileInformation.FirstOrDefault(f => f.FileID == fileID);

                if (info == null)
                {
                    return null;
                }

                if (user.ChannelName != info.ChannelName)
                {
                    return null;
                }

                FileData file = state.Files.FirstOrDefault(f => f.FileID == fileID);

                if(file == null)
                {
                    return null;
                }

                return new FileData
                {
                    FileID = file.FileID,
                    FileName = file.FileName,
                    Content = file.Content
                };
            }
        }

        public ServerUpdate GetUpdates(string userID)
        {
            lock(state.StateLock)
            {
                UserInfo user = FindUser(userID);

                if(user == null)
                {
                    return new ServerUpdate();
                }
                PendingUserUpdate pending = FindPendingUpdate(userID);

                ServerUpdate result;

                if(pending == null)
                {
                    result = new ServerUpdate();
                }
                else
                {
                    result = CopyUpdate(pending.Update);
                    pending.Update = new ServerUpdate();
                }

                result.Channels = state.Channels.Select(CopyChannel).ToList();

                if(!string.IsNullOrEmpty(user.ChannelName))
                {
                    ChannelInfo channel = FindChannel(user.ChannelName);

                    if(channel != null)
                    {
                        result.Members = new List<string>(channel.Members);

                        result.SharedFiles = state.FileInformation.Where(
                            f => f.ChannelName == user.ChannelName).Select(CopyFileInfo).ToList();

                    }
                }

                return result;
            }
        }

        public ServiceResult RegisterCallBack(string userID)
        {
            ChatCallback callback = OperationContext.Current.GetCallbackChannel<ChatCallback>();

            lock(state.StateLock)
            {
                UserInfo user = FindUser(userID);

                if(user == null)
                {
                    return ServiceResult.Fail("User is not signed in");
                }

                CallbackInfo existing = FindCallback(userID);

                if(existing != null)
                {
                    existing.Callback = callback;
                }
                else
                {
                    state.CallBacks.Add(new CallbackInfo
                    {
                        UserID = userID,
                        Callback = callback
                    });
                }
            }
            return ServiceResult.Pass("Callback registered successfully");
        }

        public ServiceResult UnregisterCallBack(string userID)
        {
            lock(state.StateLock)
            {
                RemoveCallback(userID);
            }
            return ServiceResult.Pass();
        }

        private UserInfo FindUser(string userID)
        {
            return state.Users.FirstOrDefault(u => u.UserID == userID);
        }

        private ChannelInfo FindChannel(string channelName)
        {
            return state.Channels.FirstOrDefault(c => c.Name == channelName);
        }

        private CallbackInfo FindCallback(string userID)
        {
            return state.CallBacks.FirstOrDefault(c => c.UserID == userID);
        }

        private PendingUserUpdate FindPendingUpdate(string userID)
        {
            return state.UserUpdates.FirstOrDefault(p => p.UserID == userID);
        }

        private void RemoveCallback(string userID)
        {
            CallbackInfo callback = FindCallback(userID);

            if(callback != null)
            {
                state.CallBacks.Remove(callback);
            }
        }

        private void RemovePendingUpdate(string userID)
        {
            PendingUserUpdate pendingupdate = FindPendingUpdate(userID);

            if(pendingupdate != null)
            {
                state.UserUpdates.Remove(pendingupdate);
            }
        }

        private void RemoveUserFromChannel(UserInfo user)
        {
            if(string.IsNullOrEmpty(user.ChannelName))
            {
                return;
            }
            ChannelInfo channel = FindChannel(user.ChannelName);

            if (channel != null)
            {
                channel.Members.Remove(user.UserID);
            }

            user.ChannelName = null;
        }

        private void AddMessageUpdate(string userID, Chat message)
        {
            lock(state.StateLock)
            {
                PendingUserUpdate pending = FindPendingUpdate(userID);

                if(pending != null)
                {
                    pending.Update.Messages.Add(message);
                }
            }
        }

        private void AddPrivateMessageUpdate(string userID, PrivateNessage message)
        {
            lock(state.StateLock)
            {
                PendingUserUpdate pending = FindPendingUpdate(userID);

                if(pending != null)
                {
                    pending.Update.PrivateMessages.Add(message);
                }
            }
        }

        private void AddFileUpdate(string userID, SharedFile file)
        {
            lock(state.StateLock)
            {
                PendingUserUpdate pending = FindPendingUpdate(userID);

                if(pending != null)
                {
                    pending.Update.SharedFiles.Add(CopyFileInfo(file));
                }
            }
        }

        private void AddChannelListUpdateForAllUsers()
        {
            foreach(UserInfo user in state.Users)
            {
                PendingUserUpdate pending = FindPendingUpdate(user.UserID);

                if(pending != null)
                {
                    pending.Update.Channels = state.Channels.Select(CopyChannel).ToList();

                }
            }
        }

        private void PushMessage(string userID, Chat message)
        {
            ChatCallback callback = GetCallback(userID);

            if(callback == null)
            {
                return;
            }

            try
            {
                callback.MessageReceived(message);
            }

            catch
            {
                RemoveCallback(userID);
            }
        }

        private void PushPrivateMessage(string userID, PrivateNessage message)
        {
            ChatCallback callback = GetCallback(userID);

            if(callback == null)
            {
                return;
            }

            try
            {
                callback.PrivateMessageReceived(message);
            }

            catch
            {
                RemoveCallback(userID);
            }           
        }

        private void PushFile(string userID, SharedFile file)
        {
            ChatCallback callback = GetCallback(userID);

            if(callback == null)
            {
                return;
            }

            try
            {
                callback.FileShared(file);
            }

            catch
            {
                RemoveCallback(userID);
            }
        }

        private void NotifyChannelListChanged()
        {
            List<CallbackInfo> callbacks;

            lock (state.StateLock)
            {
                callbacks = new List<CallbackInfo>(state.CallBacks);
            }

            foreach(CallbackInfo Info in callbacks)
            {
                ChannelInfo[] channels = GetChannels();
                try
                {

                    Info.Callback.ChannelListUpdated(channels);
                }
                catch
                {
                    lock(state.StateLock)
                    {
                        RemoveCallback(Info.UserID);
                    }
                }
            }
        }

        private void NotifyMembersChanged(string channelName, string userID, bool joined)
        {
            List<string> members;

            lock(state.StateLock)
            {
                ChannelInfo channel = FindChannel(channelName);

                if(channel == null)
                {
                    return;
                }

                members = new List<string>(channel.Members);
            }

            foreach(string member in members)
            {
                ChatCallback callback = GetCallback(member);

                if(callback == null)
                {
                    continue;
                }

                try
                {
                    callback.MemberListUpdated(channelName, userID, joined);
                }

                catch
                {
                    RemoveCallback(member);
                }
            }
        }

        private ChatCallback GetCallback(string userID)
        {
            lock(state.StateLock)
            {
                CallbackInfo item = FindCallback(userID);

                if(item == null)
                {
                    return null;
                }

                return item.Callback;
            }
        }

        private ChannelInfo CopyChannel(ChannelInfo channel)
        {
            return new ChannelInfo
            {
                Name = channel.Name,
                Members = new List<string>(channel.Members)
            };
        }

        private SharedFile CopyFileInfo(SharedFile file)
        {
            return new SharedFile
            {
                FileID = file.FileID,
                FileName = file.FileName,
                SharedBy = file.SharedBy,
                ChannelName = file.ChannelName,
                FileSize = file.FileSize,
                SharedTime = file.SharedTime
            };
        }

        private ServerUpdate CopyUpdate(ServerUpdate update)
        {
            return new ServerUpdate
            {
                Channels = update.Channels.Select(CopyChannel).ToList(),
                Messages = new List<Chat>(update.Messages),
                PrivateMessages = new List<PrivateNessage>(update.PrivateMessages),
                SharedFiles = update.SharedFiles.Select(CopyFileInfo).ToList(),
                Members = new List<string>(update.Members)
            };
        }
    }

}
