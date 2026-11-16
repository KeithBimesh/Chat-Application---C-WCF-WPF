using System;
using System.Windows;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using SharedChat;

namespace PollingClient
{
    /// <summary>
    /// Interaction logic for ConversationWindow.xaml
    /// </summary>
    public partial class ConversationWindow : Window
    {
        private ChatServiceInterface chatService;
        private string CurrentUserID;
        private string CurrentChannel;
        private Thread PollingThread;
        private bool Polling;
        private List<PrivateConversationWindow> PrivateWin;
        private List<SharedFile> sharedFiles;
        private ChannelListWindows channelWindow;

        public ConversationWindow(ChatServiceInterface service, string userID, string channelName, ChannelListWindows channelWindow)
        {
            InitializeComponent();

            chatService = service;
            CurrentUserID = userID;
            CurrentChannel = channelName;
            this.channelWindow = channelWindow;
            PrivateWin = new List<PrivateConversationWindow>();
            sharedFiles = new List<SharedFile>();

            Channel_Name.Content = "Channel " + CurrentChannel;

            User_Name.Content = "User " + CurrentUserID;

            LoadData();

            Polling = true;

            PollingThread = new Thread(PollServer);
            PollingThread.IsBackground = true;
            PollingThread.Start();
        }

        private void LoadData()
        {
            ServerUpdate update = chatService.GetUpdates(CurrentUserID);

            UpdateMessages(update);
            UpdateMembers(update);
            UpdatePrivateMessage(update);
            UpdateFiles(update);
        }

        private void UpdateMessages(ServerUpdate update)
        {
            Messages_list.Items.Clear();

            foreach(Chat message in update.Messages)
            {
                if(message.ChannelName == CurrentChannel)
                {
                    Messages_list.Items.Add(message.SenderID + ":" + message.Message);
                }
            }
        }

        private void UpdateMembers(ServerUpdate update)
        {
            MembersList.Items.Clear();

            foreach(string member in update.Members)
            {
                MembersList.Items.Add(member);
            }
        }
        private void UpdatePrivateMessage(ServerUpdate update)
        {
            foreach(PrivateNessage message in update.PrivateMessages)
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
                PrivateConversationWindow PrivateWindow = FindPrivateWindow(user2);

                if (PrivateWindow == null)
                {
                    PrivateWindow = new PrivateConversationWindow(chatService, CurrentUserID, user2);

                    PrivateWin.Add(PrivateWindow);

                    PrivateWindow.Show();
                }
                PrivateWindow.AddMessage(message);
            }
        }

        private void UpdateFiles(ServerUpdate update)
        {
            foreach(SharedFile file in update.SharedFiles)
            {
                if(file.ChannelName != CurrentChannel)
                {
                    continue;
                }

                bool Exists = false;

                foreach(SharedFile ExistingFile in sharedFiles)
                {
                    if(ExistingFile.FileID == file.FileID)
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
        }

        private PrivateConversationWindow FindPrivateWindow(string userID)
        {
            foreach(PrivateConversationWindow window in PrivateWin)
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

            if(string.IsNullOrWhiteSpace(message))
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

            if(result.Success)
            {
                StopPolling();
                channelWindow.Show();
                Close();
            }
            else
            {
                MessageBox.Show(result.Message);
            }
        }

        private void PrivateConversationButton_Click(object sender, RoutedEventArgs e)
        {
            if(MembersList.SelectedItem == null)
            {
                MessageBox.Show("Please select a member");
                return;
            }
            string SelectUser = MembersList.SelectedItem.ToString();

            if(SelectUser == CurrentUserID)
            {
                MessageBox.Show("You can send private message to yourself");
                return;
            }

            PrivateConversationWindow PrivateWindow = FindPrivateWindow(SelectUser);

            if(PrivateWindow != null)
            {
                PrivateWindow.Activate();

                return;
            }
            PrivateWindow = new PrivateConversationWindow(chatService, CurrentUserID, SelectUser);

            PrivateWin.Add(PrivateWindow);

            PrivateWindow.Show();
        }

        private void SharedFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "Allowed Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.txt";
            dialog.Title = "Select a file";

            bool? result = dialog.ShowDialog();

            if(result != true)
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

                foreach(string allowedextension in AllowedExtensions)
                {
                    if(extension == allowedextension)
                    {
                        Allowed = true;
                        break;
                    }
                }
                if(!Allowed)
                {
                    MessageBox.Show("That file type is not allowed");
                    return;
                }

                FileInfo fileInfo = new FileInfo(dialog.FileName);

                const long Size = 2 * 1024 * 1024;

                if(fileInfo.Length > Size)
                {
                    MessageBox.Show("File size is larger than 2MB");
                    return;
                }

                byte[] Content = File.ReadAllBytes(dialog.FileName);

                ServiceResult serviceResult = chatService.UploadFile(CurrentUserID, filename, Content);

                if(serviceResult.Success)
                {
                    MessageBox.Show("File shared successfully");
                }
                else
                {
                    MessageBox.Show(serviceResult.Message);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Can't share the file " + ex.Message);
            }
        }
        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if(SharedFileList.SelectedIndex < 0)
            {
                MessageBox.Show("Select a file");
                return;
            }

            int selectedIndex = SharedFileList.SelectedIndex;

            if(selectedIndex >= sharedFiles.Count)
            {
                return;
            }

            SharedFile selectFile = sharedFiles[selectedIndex];

            FileData fileData = chatService.DownloadFile(CurrentUserID, selectFile.FileID);

            if(fileData == null || fileData.Content == null)
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

        private void PollServer()
        {
            while(Polling)
            {
                ServerUpdate update = chatService.GetUpdates(CurrentUserID);

                Messages_list.Dispatcher.Invoke(
                    new Action(() =>
                    {
                        UpdateMessages(update);
                        UpdateMembers(update);
                        UpdatePrivateMessage(update);
                        UpdateFiles(update);
                    }));

                Thread.Sleep(3000);
            }
        }

        private void StopPolling()
        {
            Polling = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            StopPolling();
            base.OnClosed(e);
        }
    }
}    
                             
