﻿/**
 * Owned By: Ashish Kumar Gupta
 * Created By: Ashish Kumar Gupta
 * Date Created: 10/11/2021
 * Date Modified: 10/11/2021
**/

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Whiteboard
{
    /// <summary>
    ///     Client-side state management for Whiteboard.
    ///     Non-extendable class having functionalities to maintain state at client side.
    /// </summary>
    public sealed class ClientBoardStateManager : IClientBoardStateManager, IClientBoardStateManagerInternal,
        IServerUpdateListener
    {
        // Attribute holding the single instance of this class. 
        private static ClientBoardStateManager s_instance;

        // no. of states till the client can undo
        private readonly int _undoRedoCapacity = 7;

        // no. of checkpoints stored on the server
        private int _checkpointsNumber;

        // Instances of other classes
        private IClientBoardCommunicator _clientBoardCommunicator;
        private IClientCheckPointHandler _clientCheckPointHandler;

        // Clients subscribed to state manager
        private Dictionary<string, IClientBoardStateListener> _clients;

        // Attribute holding current user id
        private string _currentUserId;

        // data structures to maintain state
        private Dictionary<string, BoardShape> _mapIdToBoardShape;
        private Dictionary<string, QueueElement> _mapIdToQueueElement;
        private BoardPriorityQueue _priorityQueue;
        private BoardStack _redoStack;

        // data structures required for undo-redo
        private BoardStack _undoStack;

        /// <summary>
        ///     Private constructor.
        /// </summary>
        private ClientBoardStateManager()
        {
        }

        /// <summary>
        ///     Getter for s_instance.
        /// </summary>
        public static ClientBoardStateManager Instance
        {
            get
            {
                Trace.Indent();
                Trace.WriteLineIf(s_instance == null,
                    "Whiteboard.ClientBoardStateManager.Instance: Creating and storing a new instance.");

                // Create a new instance if not yet created.
                s_instance = s_instance is null ? new ClientBoardStateManager() : s_instance;
                Trace.WriteLine("Whiteboard.ClientBoardStateManager.Instance: Returning the stored instance.s");
                Trace.Unindent();
                return s_instance;
            }
        }

        /// <summary>
        ///     Fetches the checkpoint from server and updates the current state.
        /// </summary>
        /// <param name="checkpointNumber">The identifier/number of the checkpoint which needs to fetched.</param>
        /// <returns>List of UXShapes for UX to render.</returns>
        public void FetchCheckpoint(int checkpointNumber)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Creates and saves checkpoint.
        /// </summary>
        /// <returns>The number/identifier of the created checkpoint.</returns>
        public void SaveCheckpoint()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Sets the current user id.
        /// </summary>
        /// <param name="userId">user Id of the current user.</param>
        public void SetUser(string userId)
        {
            _currentUserId = userId;
        }

        /// <summary>
        ///     Initializes state managers attributes.
        /// </summary>
        public void Start()
        {
            // initializing all attributes 
            _checkpointsNumber = 0;
            _clientBoardCommunicator = ClientBoardCommunicator.Instance;
            _clientCheckPointHandler = new ClientCheckPointHandler();
            _clients = new Dictionary<string, IClientBoardStateListener>();
            InitializeDataStructures();

            // subscribing to ClientBoardCommunicator
            _clientBoardCommunicator.Subscribe(this);
            Trace.WriteLine("ClientBoardStateManager.Start: Initialization done.");
        }

        /// <summary>
        ///     Subscribes to notifications from ClientBoardStateManager to get updates.
        /// </summary>
        /// <param name="listener">The subscriber. </param>
        /// <param name="identifier">The identifier of the subscriber. </param>
        public void Subscribe(IClientBoardStateListener listener, string identifier)
        {
            // Cleaning current state since new state will be called
            _mapIdToBoardShape = null;
            _mapIdToQueueElement = null;
            _priorityQueue = null;
            _redoStack = null;
            _undoStack = null;
            GC.Collect();

            // Re-initializing state and adding subscriber 
            InitializeDataStructures();
            _clients.Add(identifier, listener);

            try
            {
                // Creating BoardServerShape object and requesting communicator
                Trace.WriteLine("ClientBoardStateManager.Subscribe: Sending fetch state request to communicator.");
                BoardServerShape boardServerShape = new(null, Operation.FETCH_STATE, _currentUserId);
                _clientBoardCommunicator.Send(boardServerShape);
                Trace.WriteLine("ClientBoardStateManager.Subscribe: Fetch state request sent to communicator.");
            }
            catch (Exception e)
            {
                Trace.WriteLine("ClientBoardStateManager.Subscribe: Exception occurred.");
                Trace.WriteLine(e.Message);
            }
        }

        /// <summary>
        ///     Does the redo operation for client.
        /// </summary>
        /// <returns>List of UXShapes for UX to render.</returns>
        public List<UXShape> DoRedo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Does the undo operation for client.
        /// </summary>
        /// <returns>List of UXShapes for UX to render.</returns>
        public List<UXShape> DoUndo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Fetches the BoardShape object from the map.
        /// </summary>
        /// <param name="id">Unique identifier for a BoardShape object.</param>
        /// <returns>BoardShape object with unique id equal to id.</returns>
        public BoardShape GetBoardShape(string id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Provides the current user's id.
        /// </summary>
        /// <returns>The user id of current user.</returns>
        public string GetUser()
        {
            return _currentUserId;
        }

        /// <summary>
        ///     Saves the update on a shape in the state and sends it to server for broadcast.
        /// </summary>
        /// <param name="boardShape">The object describing shape.</param>
        /// <returns>Boolean to indicate success status of update.</returns>
        public bool SaveOperation(BoardShape boardShape)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Manages state and notifies UX on receiving an update from ClientBoardCommunicator.
        /// </summary>
        /// <param name="serverUpdate">BoardServerShapes signifying the update.</param>
        public void OnMessageReceived(BoardServerShape serverUpdate)
        {
            // a case of state fetching for newly joined client
            if (serverUpdate.OperationFlag == Operation.FETCH_STATE && serverUpdate.RequesterId == _currentUserId)
            {
                // converting network update to UXShapes and sending them to UX
                Trace.WriteLine(
                    "ClientBoardStateManager.OnMessageReceived: FETCH_STATE (subscribe) request's result arrived.");
                var uXShapes = UpdateStateOnFetch(serverUpdate);
                NotifyClients(uXShapes);
                Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: Clients Notified.");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        ///     Initializes the data structures which are maintaining the state.
        /// </summary>
        /// <param name="initializeUndoRedo">Initialize undo-redo stacks or not. By default it is true.</param>
        private void InitializeDataStructures(bool initializeUndoRedo = true)
        {
            _mapIdToBoardShape = new Dictionary<string, BoardShape>();
            _mapIdToQueueElement = new Dictionary<string, QueueElement>();
            _priorityQueue = new BoardPriorityQueue();
            if (initializeUndoRedo)
            {
                _redoStack = new BoardStack(_undoRedoCapacity);
                _undoStack = new BoardStack(_undoRedoCapacity);
            }
        }

        /// <summary>
        ///     Updates local state on Fetch State from server.
        /// </summary>
        /// <param name="boardServerShape">BoardServerShape object having the whole update.</param>
        /// <returns>List of UXShape to notify client.</returns>
        private List<UXShape> UpdateStateOnFetch(BoardServerShape boardServerShape)
        {
            try
            {
                var boardShapes = boardServerShape.ShapeUpdates;
                List<UXShape> uXShapes = new();

                // Sorting boardShapes
                boardShapes.Sort(delegate(BoardShape boardShape1, BoardShape boardShape2)
                {
                    return boardShape1.LastModifiedTime.CompareTo(boardShape2.LastModifiedTime);
                });

                // updating checkpoint number   
                _checkpointsNumber = boardServerShape.CheckpointNumber;
                _clientCheckPointHandler.CheckpointNumber = _checkpointsNumber;

                // updating state
                for (var i = 0; i < boardShapes.Count; i++)
                {
                    var boardShapeId = boardShapes[i].Uid;

                    // insert in id to BoardShape map
                    if (_mapIdToBoardShape.ContainsKey(boardShapeId))
                    {
                        // if already there is some reference present, removing it
                        _mapIdToBoardShape.Remove(boardShapeId);
                        GC.Collect();
                    }

                    _mapIdToBoardShape.Add(boardShapeId, boardShapes[i]);

                    // insert in priority queue and id to QueueElement map
                    var queueElement = new QueueElement(boardShapeId, boardShapes[i].LastModifiedTime);
                    if (_mapIdToQueueElement.ContainsKey(boardShapeId))
                    {
                        // if already there is some reference present, removing it
                        var tempQueueElement = _mapIdToQueueElement[boardShapeId];
                        _priorityQueue.DeleteElement(tempQueueElement);
                        _mapIdToQueueElement.Remove(boardShapeId);
                        GC.Collect();
                    }

                    _mapIdToQueueElement.Add(boardShapeId, queueElement);
                    _priorityQueue.Insert(queueElement);

                    // converting BoardShape to UXShape and adding it in the list
                    uXShapes.Add(new UXShape(UXOperation.CREATE, boardShapes[i].MainShapeDefiner, boardShapeId,
                        _checkpointsNumber, boardServerShape.OperationFlag));
                }

                return uXShapes;
            }
            catch (Exception e)
            {
                Trace.WriteLine("ClientBoardStateManager.UpdateStateOnFetch: Exception occurred.");
                Trace.WriteLine(e.Message);
            }

            return null;
        }

        /// <summary>
        ///     Notifies clients with List of UXShapes.
        /// </summary>
        /// <param name="uXShapes">List of UX Shapes for UX to render</param>
        private void NotifyClients(List<UXShape> uXShapes)
        {
            try
            {
                lock (this)
                {
                    // Sending each client the updated UXShapes. 
                    foreach (var entry in _clients)
                    {
                        Trace.WriteLine("ClientBoardStateManager.NotifyClient: Notifying client - ", entry.Key);
                        entry.Value.OnUpdateFromStateManager(uXShapes);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("ClientBoardStateManager.NotifyClients: Exception occurred.");
                Trace.WriteLine(e.Message);
            }
        }
    }
}