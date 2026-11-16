#### Chat Application – C# WPF and WCF





##### Project Overview







This project is a desktop-based chat application developed using C#, Windows Presentation Foundation (WPF), and Windows Communication Foundation (WCF).







The application allows multiple users to communicate through shared chat channels and private messaging. It also supports file sharing between users.







The project implements two different communication mechanisms:







1. Polling Communication – Clients periodically request updates from the server to receive new messages and other information.



2. Duplex Communication – Uses WCF duplex communication with callback contracts, allowing the server to send updates directly to connected clients.







##### Features





User Authentication: Users can sign in and sign out using unique user IDs.



Channel Management: Create, view, join, and leave chat channels.



Group Messaging: Send and receive messages within shared channels.



Private Messaging: Send messages directly to individual users.



File Sharing: Upload and download files within chat channels.



Real-Time Updates: Receive updates about channel members, messages, and shared files.



User Management: Track connected users and automatically handle disconnected clients.



Two Communication Methods: Supports both polling-based and duplex communication.







##### Technologies Used







C#



.NET Framework 4.7.2



Windows Presentation Foundation (WPF)



Windows Communication Foundation (WCF)



NetTcpBinding



XAML



Visual Studio







##### Project Structure







###### The solution contains four main projects:









ChatServer - Handles server-side operations, user management, channels, messages, and file sharing. 



SharedChat - Contains shared service contracts, callback contracts, data models, and service result classes. 



PollingClient - WPF client that communicates with the server using polling. 



DuplexClient - WPF client that uses WCF duplex communication and callbacks.









##### How to Run







1 Clone or download this repository.



2 Open DC\_Assignment1.slnx using Visual Studio.



3 Build the solution.



4 Start the ChatServer project first.



5 Run either the PollingClient or DuplexClient project.



6 Enter a unique user ID to sign in.



7 Create or join a chat channel and start communicating.





The application uses the following server endpoint: net.tcp://localhost:9000



The server must be running before launching the clients.





##### Learning Outcomes







Through this project, I gained practical experience in:







Developing desktop applications using C# and WPF.



Implementing client-server communication using WCF.



Understanding polling and duplex communication mechanisms.



Working with WCF service contracts and callback contracts.



Implementing group and private messaging functionality.



Handling file uploads and downloads.



Managing multiple connected clients and server-side application state.





