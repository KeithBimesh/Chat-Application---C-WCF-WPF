using System;
using System.Windows;
using System.ServiceModel;
using SharedChat;

namespace DuplexClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DuplexChatService chatService;
        private CallbackManager callbackManager;
        public MainWindow()
        {
            InitializeComponent();

            NetTcpBinding tcp = new NetTcpBinding();

            string URL = "net.tcp://localhost:9000/DuplexChatService";

            callbackManager = new CallbackManager();

            InstanceContext instanceContext = new InstanceContext(callbackManager);

            DuplexChannelFactory<DuplexChatService> factory = new DuplexChannelFactory<DuplexChatService>(instanceContext, tcp, new EndpointAddress(URL));

            chatService = factory.CreateChannel();
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            string userID = User_ID.Text.Trim();

            if(string.IsNullOrWhiteSpace(userID))
            {
                MessageBox.Show("Enter a user ID");
                return;
            }

            ServiceResult result = chatService.SignIn(userID);

            if(result.Success)
            {
                ServiceResult callbackResult = chatService.RegisterCallBack(userID);

                if(!callbackResult.Success)
                {
                    MessageBox.Show("Callback registration failed");
                    return;
                }
                DuplexChannelList channelWindow = new DuplexChannelList(chatService, userID, callbackManager);

                callbackManager.SetChannelWindow(channelWindow);

                channelWindow.Show();

                Hide();
            }
            else
            {
                MessageBox.Show("Sign in failed");
            }
        }
    }
}
