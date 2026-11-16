using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using SharedChat;

namespace DuplexClient
{
    /// <summary>
    /// Interaction logic for DuplexConversationWindow.xaml
    /// </summary>
    public partial class DuplexConversationWindow : Window
    {
        private DuplexChatService chatService;
        private string CurrentUserID;
        private string CurrentChannel;

        private List<DuplexPrivateConversationWindow> PrivateWin;
        private List<SharedFile> sharedFiles;

        private DuplexChannelList channelList;
        private CallbackManager callbackManager;

        public DuplexConversationWindow(DuplexChatService service, string userID, string channel, DuplexChannelList channelList, CallbackManager callbackManager)
        {
            InitializeComponent();

            chatService = service;
            CurrentUserID = userID;
            CurrentChannel = channel;

            this.channelList = channelList;
            this.callbackManager = callbackManager;

            PrivateWin = new List<DuplexPrivateConversationWindow>();

            sharedFiles = new List<SharedFile>();

            User_Name.Content = CurrentUserID;

            Channel_Name.Content = CurrentChannel;

            LoadMembers();
        }

        public void LoadMembers()
        {
            MembersList.Items.Clear();

            ChannelInfo[] channels = chatService.GetChannels();

            foreach(ChannelInfo channel in channels)
            {
                if(channel.Name == CurrentChannel)
                {
                    foreach(string member in channel.Members)
                    {
                        MembersList.Items.Add(member);
                    }
                    break;
                }
            }
        }
        private DuplexPrivateConversationWindow FindPrivateWindow(string userID)
        {
            foreach (DuplexPrivateConversationWindow window in PrivateWin)
            {
                if (window.userID2 == userID)
                {
                    return window;
                }
            }
            return null;
        }
        public void AddMessage(Chat message)
        {
            if(message.ChannelName != CurrentChannel)
            {
                return;
            }
            Messages_list.Items.Add(message.SenderID + " : " + message.Message);
        }

        public void AddPrivateMessage(PrivateNessage message)
        {
            string user2;

            if(message.SenderID == CurrentUserID)
            {
                user2 = message.ReceiverID;
            }
            else
            {
                user2 = message.SenderID;
            }
            DuplexPrivateConversationWindow privateWindow = FindPrivateWindow(user2);

            if(privateWindow == null)
            {
                privateWindow = new DuplexPrivateConversationWindow(chatService, CurrentUserID, user2);

                PrivateWin.Add(privateWindow);

                privateWindow.Show();
            }
            privateWindow.AddMessage(message);
        }
        public void AddSharedFile(SharedFile file)
        {
            if(file.ChannelName != CurrentChannel)
            {
                return;
            }
            bool Exists = false;

            foreach(SharedFile ExistFile in sharedFiles)
            {
                if(ExistFile.FileID == file.FileID)
                {
                    Exists = true;
                    break;

                }
            }
            if(!Exists)
            {
                sharedFiles.Add(file);
                SharedFileList.Items.Add(file.FileName + " - shared by " + file.SharedBy);
            }
        }

        private DuplexPrivateConversationWindow FindPrivateWin(string userID)
        {
            foreach(DuplexPrivateConversationWindow window in PrivateWin)
            {
                if(window.userID2 == userID)
                {
                    return window;
                }
            }

            return null;
        }


        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            ServiceResult result = chatService.SendMessage(CurrentUserID, message);

            if (result.Success)
            {
                MessageInput.Clear();
            }
            else
            {
                MessageBox.Show(result.Message);
            }
        }

        private void LeaveButton_Click(object sender, RoutedEventArgs e)
        {
            ServiceResult result = chatService.LeaveChannel(CurrentUserID);

            if (result.Success)
            {
                callbackManager.SetConversationWindow(null);
            
                channelList.Show();
                Close();
            }
            else
            {
                MessageBox.Show(result.Message);
            }
        }

        private void PrivateConversationButton_Click(object sender, RoutedEventArgs e)
        {
            if (MembersList.SelectedItem == null)
            {
                MessageBox.Show("Please select a member");
                return;
            }
            string SelectUser = MembersList.SelectedItem.ToString();

            if (SelectUser == CurrentUserID)
            {
                MessageBox.Show("You can send private message to yourself");
                return;
            }

            DuplexPrivateConversationWindow PrivateWindow = FindPrivateWin(SelectUser);

            if (PrivateWindow != null)
            {
                PrivateWindow.Activate();

                return;
            }
            PrivateWindow = new DuplexPrivateConversationWindow(chatService, CurrentUserID, SelectUser);

            PrivateWin.Add(PrivateWindow);

            PrivateWindow.Show();
        }

        private void SharedFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "Allowed Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.txt";
            dialog.Title = "Select a file";

            bool? result = dialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            try
            {
                string filename = Path.GetFileName(dialog.FileName);
                string extension = Path.GetExtension(filename).ToLowerInvariant();

                string[] AllowedExtensions =
                {
                    ".png",
                    ".jpg",
                    ".jpeg",
                    ".gif",
                    ".bmp",
                    ".txt"
                };

                bool Allowed = false;

                foreach (string allowedextension in AllowedExtensions)
                {
                    if (extension == allowedextension)
                    {
                        Allowed = true;
                        break;
                    }
                }
                if (!Allowed)
                {
                    MessageBox.Show("That file type is not allowed");
                    return;
                }

                FileInfo fileInfo = new FileInfo(dialog.FileName);

                const long Size = 2 * 1024 * 1024;

                if (fileInfo.Length > Size)
                {
                    MessageBox.Show("File size is larger than 2MB");
                    return;
                }

                byte[] Content = File.ReadAllBytes(dialog.FileName);

                ServiceResult serviceResult = chatService.UploadFile(CurrentUserID, filename, Content);

                if (serviceResult.Success)
                {
                    MessageBox.Show("File shared successfully");
                }
                else
                {
                    MessageBox.Show(serviceResult.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't share the file " + ex.Message);
            }
        }
        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedFileList.SelectedIndex < 0)
            {
                MessageBox.Show("Select a file");
                return;
            }

            int selectedIndex = SharedFileList.SelectedIndex;

            if (selectedIndex >= sharedFiles.Count)
            {
                return;
            }

            SharedFile selectFile = sharedFiles[selectedIndex];

            FileData fileData = chatService.DownloadFile(CurrentUserID, selectFile.FileID);

            if (fileData == null || fileData.Content == null)
            {
                MessageBox.Show("Can't download the file");
                return;
            }
            string filePath = Path.Combine(Path.GetTempPath(), fileData.FileName);

            File.WriteAllBytes(filePath, fileData.Content);

            MessageBox.Show("File downloaded successfully");

            Process.Start(new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            });
        }
        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            chatService.SignOut(CurrentUserID);

            Close();
        }
    }
}
