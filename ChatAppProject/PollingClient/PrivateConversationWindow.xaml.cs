using System;
using System.Windows;
using SharedChat;

namespace PollingClient
{
    /// <summary>
    /// Interaction logic for PrivateConversationWindow.xaml
    /// </summary>
    public partial class PrivateConversationWindow : Window
    {
        private ChatServiceInterface chatService;
        private string CurrentUserID;
        private string UserID2;
        public string userID2
        {
            get
            {
                return UserID2;
            }
        }
        public PrivateConversationWindow(ChatServiceInterface service, string userID, string User2)
        {
            InitializeComponent();

            chatService = service;
            CurrentUserID = userID;
            UserID2 = User2;

            Private_Conversation.Content = "Private conversation with " + UserID2;
            Title = "Private conversation - " + UserID2;
        }

        private void PrivateMsgSendButton_Click(object sender, RoutedEventArgs e)
        {
            string Message = PrivateMessageText.Text.Trim();

            if(string.IsNullOrWhiteSpace(Message))
            {
                MessageBox.Show("Enter a message");

                return;
            }

            ServiceResult result = chatService.SendMessagePrivate(CurrentUserID, UserID2, Message);

            if(result.Success)
            {
                PrivateMessageText.Clear();
            }
            else
            {
                MessageBox.Show(result.Message, "Message sent");
            }
        }

        public void AddMessage(PrivateNessage message)
        {
            PrivateMessageList.Items.Add(message.SenderID + " : " + message.Message);
        }
    }
}
