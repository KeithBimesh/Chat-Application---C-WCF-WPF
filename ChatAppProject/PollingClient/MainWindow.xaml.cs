using System;
using System.Windows;
using System.ServiceModel;
using SharedChat;

namespace PollingClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ChatServiceInterface chatService;
        private string CurrentUserID;
        public MainWindow()
        {
            InitializeComponent();

            var tcp = new NetTcpBinding();

            var URL = "net.tcp://localhost:9000/ChatService";

            var chanFactory = new ChannelFactory<ChatServiceInterface>(tcp, URL);

            chatService = chanFactory.CreateChannel();
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            string userID = User_ID.Text.Trim();

            if(string.IsNullOrWhiteSpace(userID))
            {
                MessageBox.Show("Please enter a user ID");

                return;
            }

            ServiceResult result = chatService.SignIn(userID);

            if(result.Success)
            {
                CurrentUserID = userID;

                ChannelListWindows channelWindow = new ChannelListWindows(chatService, CurrentUserID);

                channelWindow.Show();

                this.Hide();
            }
            else
            {
                MessageBox.Show(result.Message, "Sign In Failed");
            }
        }
    }
}
