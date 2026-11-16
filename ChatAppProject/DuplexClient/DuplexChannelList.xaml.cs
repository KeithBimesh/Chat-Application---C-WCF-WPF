using System;
using System.Windows;
using SharedChat;

namespace DuplexClient
{
    /// <summary>
    /// Interaction logic for DuplexChannelList.xaml
    /// </summary>
    public partial class DuplexChannelList : Window
    {
        private DuplexChatService ChatService;
        private string CurrentUserID;
        private CallbackManager callbackManager;
        public DuplexChannelList(DuplexChatService service, string userID, CallbackManager manager)
        {
            InitializeComponent();

            ChatService = service;
            CurrentUserID = userID;
            callbackManager = manager;

            User.Content = "User " + CurrentUserID;

            LoadChannels();
        }

        public void LoadChannels()
        {
            ChannelList.Items.Clear();

            ChannelInfo[] channels = ChatService.GetChannels();

            foreach(ChannelInfo channel in channels)
            {
                ChannelList.Items.Add(channel.Name);
            }
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            if(ChannelList.SelectedItem == null)
            {
                MessageBox.Show("Select a channel");
                return;
            }

            string channelName = ChannelList.SelectedItem.ToString();

            ServiceResult result = ChatService.JoinChannel(CurrentUserID, channelName);

            if(result.Success)
            {
                DuplexConversationWindow conversation = new DuplexConversationWindow(ChatService, CurrentUserID, channelName, this, callbackManager);

                callbackManager.SetConversationWindow(conversation);

                conversation.Show();

                Hide();
            }
            else
            {
                MessageBox.Show("Join failed");
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string channelName = Channel_Name.Text.Trim();

            if (string.IsNullOrWhiteSpace(channelName))
            {
                MessageBox.Show("Please enter a channel name.");

                return;
            }

            ServiceResult result = ChatService.CreateChannel(CurrentUserID, channelName);

            if (result.Success)
            {
                MessageBox.Show("Channel created successfuly");

                Channel_Name.Clear();
            }
            else
            {
                MessageBox.Show(result.Message);
            }
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        { 
            
            ChatService.SignOut(CurrentUserID);

            Close();
        }

    }
}

    

