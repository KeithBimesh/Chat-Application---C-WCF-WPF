using System;
using System.Windows;
using System.ServiceModel;
using SharedChat;


namespace PollingClient
{
    /// <summary>
    /// Interaction logic for ChannelListWindows.xaml
    /// </summary>
    public partial class ChannelListWindows : Window
    {
        private ChatServiceInterface chatService;
        private string CurrentUserID;
        public ChannelListWindows(ChatServiceInterface service, string userID)
        {
            InitializeComponent();

            chatService = service;
            CurrentUserID = userID;

            User.Content = "User " + CurrentUserID;

            LoadChannels();
        }

        private void LoadChannels()
        {
            ChannelList.Items.Clear();

            var channels = chatService.GetChannels();

            foreach(ChannelInfo channel in channels)
            {
                ChannelList.Items.Add(channel.Name);
            }
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            if(ChannelList.SelectedItems == null)
            {
                MessageBox.Show("Please select a channel");

                return;
            }

            string channelName = ChannelList.SelectedItem.ToString();

            ServiceResult result = chatService.JoinChannel(CurrentUserID, channelName);

            if(result.Success)
            {
                ConversationWindow conversation = new ConversationWindow(chatService, CurrentUserID, channelName, this);

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

            ServiceResult result = chatService.CreateChannel(CurrentUserID, channelName);

            if (result.Success)
            {
                MessageBox.Show("Channel created successfuly");

                Channel_Name.Clear();

                LoadChannels();
            }
            else
            {
                MessageBox.Show("Channel creation failed");
            }
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            chatService.SignOut(CurrentUserID);

            Close();
        }

    }
}
