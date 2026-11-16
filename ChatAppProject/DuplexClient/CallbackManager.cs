using System;
using System.Windows;
using System.ServiceModel;
using SharedChat;

namespace DuplexClient
{
    [CallbackBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    public class CallbackManager : ChatCallback
    {
        private DuplexChannelList channelWindow;
        private DuplexConversationWindow conversationWindow;

        public CallbackManager()
        {

        }
        public void SetChannelWindow(DuplexChannelList window)
        {
            channelWindow = window;
        }

        public void SetConversationWindow(DuplexConversationWindow window)
        {
            conversationWindow = window;
        }

        public void ChannelListUpdated(ChannelInfo[] channels)
        {
            if(channelWindow != null)
            {
                channelWindow.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        channelWindow.LoadChannels();
                    }));
            }
        }

        public void MemberListUpdated(string ChannelName, string userID, bool joined)
        {
            if(conversationWindow != null)
            {
                conversationWindow.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        conversationWindow.LoadMembers();
                    }));
            }
        }

        public void MessageReceived(Chat message)
        {
            if(conversationWindow != null)
            {
                conversationWindow.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        conversationWindow.AddMessage(message);
                    }));
            }
        }

        public void PrivateMessageReceived(PrivateNessage message)
        {
            if(conversationWindow != null)
            {
                conversationWindow.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        conversationWindow.AddPrivateMessage(message);
                    }));
            }
        }

        public void FileShared(SharedFile file)
        {
            if (conversationWindow != null)
            {
                conversationWindow.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        conversationWindow.AddSharedFile(file);
                    }));
            }
        }
    }
}
