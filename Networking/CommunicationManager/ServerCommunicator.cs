using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Networking
{
    internal class ServerCommunicator : ICommunicator
    {
        /** Declare dictionary variable to store client Id and corresponding socket object*/
        private readonly Dictionary<string, TcpClient> _clientIdSocket = new();

        /**
         * Declare dictionary variable that stores clientId and receiveSocketListener
         */
        private readonly Dictionary<string, ReceiveSocketListener> _clientListener =
            new();

        /** Declare queue variable for receiving messages*/
        private readonly Queue _receiveQueue = new();

        /** Declare queue variable for sending messages*/
        private readonly Queue _sendQueue = new();

        /** Declare dictionary variable to store module Id and corresponding handler*/
        private readonly Dictionary<string, INotificationHandler> _subscribedModules =
            new();

        /** Declare thread variable for accepting request*/
        private Thread _acceptRequest;

        /** Declare variable to control acceptRequest thread*/
        private volatile bool _acceptRequestRun;

        private ReceiveQueueListener _receiveQueueListener;

        /** Declare sendSocketListenerServer variable for sending messages across the network*/
        private SendSocketListenerServer _sendSocketListenerServer;

        /**Declare TcpListener variable of server*/
        private TcpListener _serverSocket;

        /// <summary>
        ///     start the server and return ip and port
        /// </summary>
        /// <returns> String</returns>
        string ICommunicator.Start(string serverIp, string serverPort)
        {
            var ip = IPAddress.Parse(GetLocalIpAddress());
            var port = FreeTcpPort(ip);
            _serverSocket = new TcpListener(ip, port);

            //start server at the scanned port of the ip 
            _serverSocket.Start();

            //start sendSocketListener of server for sending message 
            _sendSocketListenerServer = new SendSocketListenerServer(_sendQueue, _clientIdSocket);
            _sendSocketListenerServer.Start();

            _receiveQueueListener = new ReceiveQueueListener(_receiveQueue, _subscribedModules);
            _receiveQueueListener.Start();

            //start acceptRequest thread of server for accepting request
            _acceptRequest = new Thread(() => AcceptRequest());
            _acceptRequestRun = true;
            _acceptRequest.Start();

            Trace.WriteLine("Server has started with ip = " + ip
                                                            + " and port number = " + port);

            return ip + ":" + port;
        }

        /// <summary>
        ///     closes all the running thread of the server
        /// </summary>
        /// <returns> void </returns>
        void ICommunicator.Stop()
        {
            //stop acceptRequest thread
            _acceptRequestRun = false;
            _serverSocket.Stop();

            //stop receiveSocketListener of all the clients 
            foreach (var listener in _clientListener)
            {
                var receiveSocketListener = listener.Value;
                receiveSocketListener.Stop();
            }

            _receiveQueueListener.Stop();
            // stop sendSocketListener of server
            _sendSocketListenerServer.Stop();
        }

        /// <summary>
        ///     It adds client socket to a map and starts listening on that socket
        ///     also adds corresponding listener to a set
        /// </summary>
        /// <returns> void </returns>
        void ICommunicator.AddClient<T>(string clientId, T socketObject)
        {
            // add clientID and socketObject into Dictionary 
            _clientIdSocket[clientId] = (TcpClient) (object) socketObject;

            //Start receiveSocketListener of the client in the 
            //server for listening message from the client

            var receiveSocketListener =
                new ReceiveSocketListener(_receiveQueue, (TcpClient) (object) socketObject);
            _clientListener[clientId] = receiveSocketListener;
            receiveSocketListener.Start();
        }

        /// <summary>
        ///     It removes client from server
        /// </summary>
        /// <returns> void </returns>
        void ICommunicator.RemoveClient(string clientId)
        {
            // stop the listener of the client 
            var receiveSocketListener = _clientListener[clientId];
            receiveSocketListener.Stop();

            //close stream  and connection of the client
            var tcpClient = _clientIdSocket[clientId];
            tcpClient.GetStream().Close();
            tcpClient.Close();

            // remove the socket object and listener  of the client 
            _clientListener.Remove(clientId);
            _clientIdSocket.Remove(clientId);
        }

        /// <summary>
        ///     It takes data and identifier
        ///     forms packet object  and push to the
        ///     sending queue for broadcast
        /// </summary>
        /// <returns> void </returns>
        void ICommunicator.Send(string data, string identifier)
        {
            var packet = new Packet {ModuleIdentifier = identifier, SerializedData = data};
            try
            {
                _sendQueue.Enqueue(packet);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        ///     It takes data, identifier and destination
        ///     forms packet object and push to the
        ///     sending queue for private messaging
        /// </summary>
        /// <returns> void </returns>
        void ICommunicator.Send(string data, string identifier, string destination)
        {
            if (!_clientIdSocket.ContainsKey(destination)) throw new Exception("Client does not exist in the room!");
            var packet = new Packet {ModuleIdentifier = identifier, SerializedData = data, Destination = destination};
            try
            {
                _sendQueue.Enqueue(packet);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        ///     It adds notification handler of module
        /// </summary>
        /// <returns> void </returns>
        void ICommunicator.Subscribe(string identifier, INotificationHandler handler, int priority)
        {
            _subscribedModules.Add(identifier, handler);
            _sendQueue.RegisterModule(identifier, priority);
            _receiveQueue.RegisterModule(identifier, priority);
        }

        /// <summary>
        ///     It finds IP4 address of machine which does not end with .1
        /// </summary>
        /// <returns>IP4 address </returns>
        private static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    var address = ip.ToString();

                    // check  IP does not end with .1 
                    if (address.Split(".")[3] != "1") return ip.ToString();
                }

            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        /// <summary>
        ///     scan for free Tcp port.
        /// </summary>
        /// <returns>integer </returns>
        private static int FreeTcpPort(IPAddress ip)
        {
            var tcp = new TcpListener(ip, 0);
            tcp.Start();
            var port = ((IPEndPoint) tcp.LocalEndpoint).Port;
            tcp.Stop();
            return port;
        }

        /// <summary>
        ///     It accepts all incoming client request
        /// </summary>
        /// <returns> void </returns>
        private void AcceptRequest()
        {
            // Block and wait for incoming client connection
            while (_acceptRequestRun)
                try
                {
                    var clientSocket = _serverSocket.AcceptTcpClient();

                    //notify subscribed Module handler
                    foreach (var module in _subscribedModules) module.Value.OnClientJoined(clientSocket);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.Interrupted)
                        Trace.WriteLine("socket blocking listener has been closed");
                }
        }
    }
}