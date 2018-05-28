using log4net;
using nsConnectionChannel;
using nsConnectionData;
using Stt.Derivatives.Api.Constants;
using System;
using System.Threading;


namespace AvventoAPILibrary
{
    public class ConnectionManager
    {
        static readonly ILog log = LogManager.GetLogger(typeof(ConnectionManager));

        readonly ReceiveHandlerImplementation rhi;
        readonly AvventoAPI _avventoApi;
        readonly SessionManager session;

        public ConnectionManager(AvventoAPI _avventoApi, ReceiveHandlerImplementation rhi, SessionManager session)
        {
            this.rhi = rhi;
            this._avventoApi = _avventoApi;
            this.session = session;
        }

        ClientSocket.ClientSocket clientSocket;

        public void ConnectToMarket(string IPAddress, int port)
        {
            while (!session.IsConnected)
            {
                if (Utilities.PingServer(IPAddress))
                {
                    string[] pServerIPAddress = new[] { IPAddress };
                    int[] pServerPort = new[] { port };
                    int initialCapacity = 101;
                    int concurrencyLevel = Environment.ProcessorCount * 2;

                    var rougeMessageChecker = new STTSocketLibHelper.RogueMessageCheckerManagement(null, null, null);
                    var connectionManagement = new STTSocketLibHelper.ConnectionManagement(initialCapacity, concurrencyLevel, rhi, DisconnectHandler, ErrorHandler, NewConnectionHandler);
                    var socketConstructor = new ClientSocket.ClientSocketConstructor(pServerIPAddress, pServerPort, connectionManagement, rougeMessageChecker, 1000, 3, Market.STT, false);
                    clientSocket = new ClientSocket.ClientSocket(socketConstructor);
                }
                else
                {
                    log.Debug("Failed to ping server");
                }
                Thread.Sleep(5000);
            }
        }

        public void CloseConnection()
        {
            rhi.Stop();
            clientSocket.DisconnectAll();
        }

        
        public bool Send(MessageType messageType, string username)
        {
            return Send(messageType, username, new Utilities.EmptyStruct());
        }
        public bool Send<T>(MessageType messageType, string username, T messageStruct)
            where T : struct
        {
            if (session.IsLoggedIn || messageType == MessageType.MESSAGE_0_LOGIN_REQUEST)
            {
                var messageBytes = Utilities.CreateMessageByteArray(messageType, username, messageStruct);
                _avventoApi.InternalEvents.APIMessageSent?.Invoke(messageType, messageStruct);
                return Send(messageBytes);
            }
            log.Warn("Not logged in, Failed to send message");
            return false;
        }

        private bool Send(byte[] bytes)
        {
            ConnectionData connectionData = new ConnectionData(bytes, clientSocket.GetConnections()[0].GetMyEndPoint); 
            clientSocket.GetConnections()[0].SendMessage(connectionData);
            return true;
        }

        #region Socket Lib Handlers

        public void NewConnectionHandler(Channel client, string socket)
        {
            _avventoApi.InternalEvents.NewConnection(client, socket);
        }

        public void DisconnectHandler(string remoteEndPoint, Exception exception)
        {
            _avventoApi.InternalEvents.DisconnectHandler(remoteEndPoint, exception);
        }

        public void ErrorHandler(string IpEndPoint, string exception)
        {
            _avventoApi.InternalEvents.SocketErrorHandler(IpEndPoint,exception);
        }

        #endregion Socket Lib Handlers
    }
}