﻿namespace Networking
{
    public interface ICommunicator
    {
        /// <summary>
        ///     Connects to the server.
        /// </summary>
        /// <param name="serverIp">IP Address of the server.</param>
        /// <param name="serverPort">Port on which the server is running.</param>
        /// <returns>
        ///     Client side: "1" if success, "0" if it fails.
        ///     Server side: The address of the client as "serverIP:serverPort"
        /// </returns>
        string Start(string serverIp = null, string serverPort = null);

        /// <summary>
        ///     Disconnects from the server.
        /// </summary>
        void Stop();

        /// <summary>
        ///     Indicates the joining of a new client to concerned modules.
        /// </summary>
        /// <typeparam name="T">socketObject.</typeparam>
        /// <param name="clientId">Unique ID of thr Client.</param>
        /// <param name="socketObject">socket object associated with the client.</param>
        void AddClient<T>(string clientId, T socketObject);

        /// <summary>
        ///     Notifies all concerned modules regarding the removal of the client.
        /// </summary>
        /// <param name="clientId">Unique ID of the client.</param>
        void RemoveClient(string clientId);

        /// <summary>
        ///     Sends data to the server[Client-Side].
        ///     Broadcasts data to all connected clients[Server-Side].
        /// </summary>
        /// <param name="data">Data to be sent over the network.</param>
        /// <param name="identifier">Module Identifier.</param>
        void Send(string data, string identifier);

        /// <summary>
        ///     Sends the data to one client[Server-Side].
        /// </summary>
        /// <param name="data">Data to be sent over the network.</param>
        /// <param name="identifier">Module Identifier.</param>
        /// <param name="destination">Client ID of the receiver.</param>
        void Send(string data, string identifier, string destination);

        /// <summary>
        ///     Provides a subscription to the modules for listening for the data over the network.
        /// </summary>
        /// <param name="identifier">Module Identifier.</param>
        /// <param name="handler">Module implementation of handler; called to notify about an incoming message.</param>
        /// <param name="priority">Priority Number indicating the weight in queue to be given to the module.</param>
        void Subscribe(string identifier, INotificationHandler handler, int priority = 1);
    }
}