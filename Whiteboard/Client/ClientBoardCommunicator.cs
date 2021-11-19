/**
 * Owned By: Gurunadh Pachappagari
 * Created By: Gurunadh Pachappagari
 * Date Created: 13 Oct 2021
 * Date Modified: 01 Nov 2021
**/

using System.Collections.Generic;
using Networking;

namespace Whiteboard
{
    /// <summary>
    ///     Bridge the gap between Server side White Board Modules and Networking module
    /// </summary>
    public sealed class ClientBoardCommunicator : IClientBoardCommunicator, INotificationHandler
    {
        private static ClientBoardCommunicator instance;
        private static ISerializer serializer;
        private static ICommunicator communicator;
        private static readonly string moduleIdentifier = "Whiteboard";
        private static HashSet<IServerUpdateListener> subscribers;

        /// <summary>
        ///     private constructor for a singleton
        /// </summary>
        private ClientBoardCommunicator()
        {
        }

        /// <summary>
        ///     instance getter
        /// </summary>
        public static ClientBoardCommunicator Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ClientBoardCommunicator();
                    serializer = new Serializer();
                    // communicator = new ClientCommunicator(); TODO: Correct this error
                    communicator.Subscribe(moduleIdentifier, instance);
                    subscribers = new HashSet<IServerUpdateListener>();
                }

                return instance;
            }
        }

        /// <summary>
        ///     serializes the shape objects and passes it to communicator.send()
        /// </summary>
        /// <param name="clientUpdate"> the object to be passed to server</param>
        public void Send(BoardServerShape clientUpdate)
        {
            // TODO: Correct this error
            // var xml_obj = serializer.Serialize(clientUpdate);
            // communicator.Send(xml_obj, moduleIdentifier);
        }

        /// <summary>
        ///     publishes deserialized objects to listeners
        /// </summary>
        /// <param name="listener">subscriber</param>
        public void Subscribe(IServerUpdateListener listener)
        {
            subscribers.Add(listener);
        }

        public void OnDataReceived(string data)
        {
            var deserializedShape = serializer.Deserialize<BoardServerShape>(data);
            foreach (var subscriber in subscribers) subscriber.OnMessageReceived(deserializedShape);
        }
    }
}