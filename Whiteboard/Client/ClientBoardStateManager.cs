﻿/**
 * Owned By: Ashish Kumar Gupta
 * Created By: Ashish Kumar Gupta
 * Date Created: 10/11/2021
 * Date Modified: 11/12/2021
**/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Whiteboard
{
    /// <summary>
    /// Client-side state management for Whiteboard.
    /// Non-extendable class having functionalities to maintain state at client side. 
    /// </summary>
    public sealed class ClientBoardStateManager : IClientBoardStateManager, IClientBoardStateManagerInternal, IServerUpdateListener
    {
        // Attribute holding the single instance of this class. 
        private static ClientBoardStateManager s_instance = null;

        // Instances of other classes
        private IClientBoardCommunicator _clientBoardCommunicator;
        private IClientCheckPointHandler _clientCheckPointHandler;

        // Attribute holding current user id
        private string _currentUserId = null;

        // Clients subscribed to state manager
        private Dictionary<string, IClientBoardStateListener> _clients;

        // no. of checkpoints stored on the server
        private int _checkpointsNumber;

        // no. of states till the client can undo
        private readonly int _undoRedoCapacity = BoardConstants.UNDO_REDO_STACK_SIZE;

        // data structures to maintain state
        private Dictionary<string, BoardShape> _mapIdToBoardShape;
        private Dictionary<string, QueueElement> _mapIdToQueueElement;
        private BoardPriorityQueue _priorityQueue;

        // data structures required for undo-redo
        private BoardStack _undoStack;
        private BoardStack _redoStack;

        // lock for the state
        private readonly object _stateLock = new();

        // The current base state.
        private int _currentCheckpointState;

        // To maintain the Shape-Ids that were recently deleted. 
        // Required in cases of Delete and then Modify situations when updates are not yet reached to all clients
        private HashSet<string> _deletedShapeIds;

        // The level of user.
        private int _userLevel;

        /// <summary>
        /// Private constructor. 
        /// </summary>
        private ClientBoardStateManager() { }

        /// <summary>
        /// Getter for s_instance. 
        /// </summary>
        public static ClientBoardStateManager Instance
        {
            get
            {
                Trace.Indent();
                Trace.WriteLineIf(s_instance == null, "Whiteboard.ClientBoardStateManager.Instance: Creating and storing a new instance.");

                // Create a new instance if not yet created.
                s_instance = s_instance is null ? new ClientBoardStateManager() : s_instance;
                Trace.WriteLine("Whiteboard.ClientBoardStateManager.Instance: Returning the stored instance.s");
                Trace.Unindent();
                return s_instance;
            }
        }

        /// <summary>
        /// Does the redo operation for client. 
        /// </summary>
        /// <returns>List of UXShapes for UX to render.</returns>
        public List<UXShape> DoRedo()
        {
            try
            {
                lock (_stateLock)
                {
                    Trace.WriteLine("ClientBoardStateManager.DoRedo: Redo is called.");
                    
                    if (_redoStack.IsEmpty())
                    {
                        Trace.WriteLine("ClientBoardStateManager.DoRedo: Stack is empty.");
                        return null;
                    }

                    // get the top element and put it in undo stack
                    Tuple<BoardShape, BoardShape> tuple = _redoStack.Top();
                    _redoStack.Pop();
                    _undoStack.Push(tuple.Item2?.Clone(), tuple.Item1?.Clone());

                    // do desired operation and send updates to UX and server
                    List<UXShape> uXShapes = UndoRedoRollback(tuple);
                    
                    // if the shape got deleted from server updates, user can't undo-redo that, hence do redo on next entry.
                    if(uXShapes.Count == 0)
                    {
                        _undoStack.Pop();
                        DoRedo();
                    }
                    return uXShapes;
                }
            }
            catch(Exception e)
            {
                Trace.WriteLine("ClientBoardStateManager.DoRedo: An exception occured.");
                Trace.WriteLine(e.Message);
            }
            return null;
        }

        /// <summary>
        /// Does the undo operation for client. 
        /// </summary>
        /// <returns>List of UXShapes for UX to render.</returns>
        public List<UXShape> DoUndo()
        {
            try
            {
                lock (_stateLock)
                {
                    Trace.WriteLine("ClientBoardStateManager.DoUndo: Undo is called.");

                    if (_undoStack.IsEmpty())
                    {
                        Trace.WriteLine("ClientBoardStateManager.DoUndo: Stack is empty.");
                        return null;
                    }

                    // get the top element and put it in redo stack
                    Tuple<BoardShape, BoardShape> tuple = _undoStack.Top();
                    _undoStack.Pop();
                    _redoStack.Push(tuple.Item2?.Clone(), tuple.Item1?.Clone());

                    // do desired operation and send updates to UX and server
                    List<UXShape> uXShapes = UndoRedoRollback(tuple);

                    // if the shape got deleted from server updates, user can't undo-redo that, hence do undo on next entry.
                    if (uXShapes.Count == 0)
                    {
                        _redoStack.Pop();
                        DoUndo();
                    }
                    return uXShapes;
                }
            }
            catch(Exception e)
            {
                Trace.WriteLine("ClientBoardStateManager.DoUndo: An exception occured.");
                Trace.WriteLine(e.Message);
            }
            return null;
        }

        /// <summary>
        /// Fetches the checkpoint from server and updates the current state. 
        /// </summary>
        /// <param name="checkpointNumber">The identifier/number of the checkpoint which needs to fetched.</param>
        /// <returns>List of UXShapes for UX to render.</returns>
        public void FetchCheckpoint([NotNull] int checkpointNumber)
        {
            Trace.WriteLine("ClientBoardStateManager.FetchCheckpoint: Fetch checkpoint request received.");
            _clientCheckPointHandler.FetchCheckpoint(checkpointNumber);
        }

        /// <summary>
        /// Fetches the BoardShape object from the map.  
        /// </summary>
        /// <param name="id">Unique identifier for a BoardShape object.</param>
        /// <returns>BoardShape object with unique id equal to id.</returns>
        public BoardShape GetBoardShape([NotNull] string id)
        {
            try
            {
                lock (_stateLock)
                {
                    if (_mapIdToBoardShape.ContainsKey(id))
                    {
                        return _mapIdToBoardShape[id];
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("ClientBoardStateManager.GetBoardShape: Exception occured.");
                Trace.WriteLine(e.Message);
            }
            return null;
        }

        /// <summary>
        /// Provides the current user's id. 
        /// </summary>
        /// <returns>The user id of current user.</returns>
        public string GetUser()
        {
            return _currentUserId ?? throw new NullReferenceException("Current user-id not set");
        }

        /// <summary>
        /// Manages state and notifies UX on receiving an update from ClientBoardCommunicator.
        /// </summary>
        /// <param name="serverUpdate">BoardServerShapes signifying the update.</param>
        public void OnMessageReceived([NotNull] BoardServerShape serverUpdate)
        {
            try
            {
                // a case of state fetching for newly joined client
                if (serverUpdate.OperationFlag == Operation.FETCH_STATE && serverUpdate.RequesterId == _currentUserId)
                {
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: FETCH_STATE (subscribe) request's result arrived.");
                    lock (_stateLock)
                    {
                        // converting network update to UXShapes and sending them to UX
                        List<UXShape> uXShapes = UpdateStateOnFetch(serverUpdate);
                        NotifyClients(uXShapes);
                    }
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: Clients Notified.");
                }
                // a case of checkpoint fetching
                else if (serverUpdate.OperationFlag == Operation.FETCH_CHECKPOINT)
                {
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: FETCH_CHECKPOINT request's result arrived.");
                    lock (_stateLock)
                    {
                        // Nullify current state
                        NullifyDataStructures();

                        // converting network update to UXShapes and sending them to UX
                        List<UXShape> uXShapes = UpdateStateOnFetch(serverUpdate);
                        NotifyClients(uXShapes);
                    }
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: Clients Notified.");
                }
                // a case of new checkpoint created
                else if (serverUpdate.OperationFlag == Operation.CREATE_CHECKPOINT)
                {
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: CREATE_CHECKPOINT request's result arrived.");

                    // checking sync conditions
                    CheckCountAndCurrentCheckpoint(serverUpdate, false, true);

                    lock (_stateLock)
                    {
                        // update number of checkpoints in state
                        _checkpointsNumber = serverUpdate.CheckpointNumber;
                        _clientCheckPointHandler.CheckpointNumber = _checkpointsNumber;

                        // notify UX to display new number
                        NotifyClients(new List<UXShape> { new(_checkpointsNumber, Operation.FETCH_CHECKPOINT) });
                    }
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: Clients Notified.");
                }
                // when other users create a shape
                else if (serverUpdate.OperationFlag == Operation.CREATE && serverUpdate.RequesterId != _currentUserId)
                {
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: CREATE request's result arrived.");

                    // checking conditions on server update.
                    CheckCountAndCurrentCheckpoint(serverUpdate);

                    lock (_stateLock)
                    {
                        NotifyClients(ServerOperationUpdate(serverUpdate.ShapeUpdates[0], Operation.CREATE));
                    }
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: Clients Notified.");
                }

                else if(serverUpdate.OperationFlag == Operation.MODIFY && serverUpdate.RequesterId != _currentUserId)
                {
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: MODIFY request's result arrived.");

                    // checking conditions on server update.
                    CheckCountAndCurrentCheckpoint(serverUpdate);
                    
                    // Client deleted the shape but server didn't receive it yet.
                    if (_deletedShapeIds.Contains(serverUpdate.ShapeUpdates[0].Uid))
                    {
                        Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: Modify on deleted shape.");
                        return;
                    }

                    lock (_stateLock)
                    {
                        NotifyClients(ServerOperationUpdate(serverUpdate.ShapeUpdates[0], Operation.MODIFY));
                    }
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: Clients Notified.");
                }
                else if (serverUpdate.OperationFlag == Operation.DELETE && serverUpdate.RequesterId != _currentUserId)
                {
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: DELETE request's result arrived.");

                    // checking conditions on server update.
                    CheckCountAndCurrentCheckpoint(serverUpdate);

                    // Client just deleted the shape and server didn't receive the request yet.
                    if (_deletedShapeIds.Contains(serverUpdate.ShapeUpdates[0].Uid))
                    {
                        Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: Delete on deleted shape.");
                        return;
                    }

                    lock (_stateLock)
                    {
                        NotifyClients(ServerOperationUpdate(serverUpdate.ShapeUpdates[0], Operation.DELETE));
                    }
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: Clients Notified.");
                }
                else if (serverUpdate.OperationFlag == Operation.CLEAR_STATE)
                {
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: CLEAR_STATE request's result arrived.");

                    // checking conditions on server update.
                    CheckCountAndCurrentCheckpoint(serverUpdate, false, true);

                    lock (_stateLock)
                    {
                        // set checkpoint state
                        _currentCheckpointState = serverUpdate.CurrentCheckpointState;

                        // clear the state and notify the UX for the same
                        NullifyDataStructures();
                        NotifyClients(new List<UXShape> { new(_checkpointsNumber, Operation.CLEAR_STATE) });
                    }
                    Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: Clients Notified.");
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: Exception occured");
                Trace.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Creates and saves checkpoint. 
        /// </summary>
        /// <returns>The number/identifier of the created checkpoint.</returns>
        public void SaveCheckpoint()
        {
            Trace.WriteLine("ClientBoardStateManager.SaveCheckpoint: Create checkpoint request received.");
            _clientCheckPointHandler.SaveCheckpoint();
        }

        /// <summary>
        /// Saves the update on a shape in the state and sends it to server for broadcast. 
        /// </summary>
        /// <param name="boardShape">The object describing shape.</param>
        /// <returns>Boolean to indicate success status of update.</returns>
        public bool SaveOperation([NotNull] BoardShape boardShape)
        {
            try
            {
                if (boardShape.RecentOperation == Operation.CREATE)
                {
                    lock (_stateLock)
                    {
                        // Checking pre-conditions for CREATE
                        PreConditionChecker(Operation.CREATE, boardShape.Uid);

                        // New QueueElement
                        QueueElement queueElement = new(boardShape.Uid, boardShape.LastModifiedTime);

                        // Add the update in respective data structures
                        _mapIdToBoardShape.Add(boardShape.Uid, boardShape);
                        _priorityQueue.Insert(queueElement);
                        _mapIdToQueueElement.Add(boardShape.Uid, queueElement);

                        // create a deep copy and put it in undo stack
                        _undoStack.Push(null, boardShape.Clone());

                        _deletedShapeIds.Remove(boardShape.Uid);
                        Trace.WriteLine("ClientBoardStateManager.SaveOperation: State updated for CREATE operation.");
                    }

                    // Send the update to server
                    _clientBoardCommunicator.Send(new(new List<BoardShape> { boardShape }, Operation.CREATE, _currentUserId, currentCheckpointState: _currentCheckpointState));
                    return true;
                }
                else if (boardShape.RecentOperation == Operation.MODIFY)
                {
                    lock (_stateLock)
                    {
                        // A case when server thread got the lock and deleted the shape just before the client thread
                        if (_deletedShapeIds.Contains(boardShape.Uid))
                        {
                            Trace.WriteLine("ClientBoardStateManager.SaveOperation: Modify on deleted shape.");
                            return false;
                        }

                        // Checking pre-conditions for MODIFY
                        PreConditionChecker(Operation.MODIFY, boardShape.Uid);

                        // create a deep copy and add previous & new one to undo-stack
                        _undoStack.Push(_mapIdToBoardShape[boardShape.Uid].Clone(), boardShape.Clone());

                        // Modify accordingly in respective data structures
                        _mapIdToBoardShape[boardShape.Uid] = boardShape;
                        QueueElement queueElement = _mapIdToQueueElement[boardShape.Uid];
                        _priorityQueue.IncreaseTimestamp(queueElement, boardShape.LastModifiedTime);
                        Trace.WriteLine("ClientBoardStateManager.SaveOperation: State updated for MODIFY operation.");
                    }

                    // Send the update to server
                    _clientBoardCommunicator.Send(new(new List<BoardShape> { boardShape }, Operation.MODIFY, _currentUserId, currentCheckpointState: _currentCheckpointState));
                    GC.Collect();
                    return true;
                }
                else if (boardShape.RecentOperation == Operation.DELETE)
                {
                    lock (_stateLock)
                    {
                        // A case when server thread got the lock and deleted the shape just before the client thread
                        if (_deletedShapeIds.Contains(boardShape.Uid))
                        {
                            Trace.WriteLine("ClientBoardStateManager.SaveOperation: Delete on deleted shape.");
                            return false;
                        }

                        // Checking pre-conditions for DELETE
                        PreConditionChecker(Operation.DELETE, boardShape.Uid);

                        // create a deep copy and push it in undo stack
                        _undoStack.Push(boardShape.Clone(), null);

                        // Delete from respective data structures
                        _mapIdToBoardShape.Remove(boardShape.Uid);
                        _priorityQueue.DeleteElement(_mapIdToQueueElement[boardShape.Uid]);
                        _mapIdToQueueElement.Remove(boardShape.Uid);
                        _deletedShapeIds.Add(boardShape.Uid);

                        Trace.WriteLine("ClientBoardStateManager.SaveOperation: State updated for DELETE operation.");
                    }

                    // Send the update to server
                    _clientBoardCommunicator.Send(new(new List<BoardShape> { boardShape }, Operation.DELETE, _currentUserId, currentCheckpointState: _currentCheckpointState));
                    GC.Collect();
                    return true;
                }
                else
                {
                    // No other flags are supported in SaveOperation.
                    throw new NotSupportedException("Operation not supported.");
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("ClientBoardStateManager.SaveOperation: Exception occurred.");
                Trace.WriteLine(e.Message);
            }
            return false;
        }

        /// <summary>
        /// Sets the current user id. 
        /// </summary>
        /// <param name="userId">user Id of the current user.</param>
        public void SetUser([NotNull] string userId)
        {
            Trace.WriteLine("ClientBoardStateManager.SetUser: User-Id is set.");
            _currentUserId = userId;
        }

        /// <summary>
        /// Sets the user level.
        /// </summary>
        /// <param name="userLevel">The user level.</param>
        public void SetUserLevel(int userLevel)
        {
            _userLevel = userLevel;
        }

        /// <summary>
        /// Initializes state managers attributes. 
        /// </summary>
        public void Start()
        {
            // initializing all attributes 
            _checkpointsNumber = BoardConstants.EMPTY_SIZE;
            _currentCheckpointState = BoardConstants.INITIAL_CHECKPOINT_STATE;
            _clientBoardCommunicator = ClientBoardCommunicator.Instance;
            _clientCheckPointHandler = new ClientCheckPointHandler();
            _clients = new Dictionary<string, IClientBoardStateListener>();
            _deletedShapeIds = new HashSet<string>();
            _userLevel = BoardConstants.LOW_USER_LEVEL;

            InitializeDataStructures();

            // subscribing to ClientBoardCommunicator
            _clientBoardCommunicator.Subscribe(this);
            Trace.WriteLine("ClientBoardStateManager.Start: Initialization done.");
        }

        /// <summary>
        /// Subscribes to notifications from ClientBoardStateManager to get updates.
        /// </summary>
        /// <param name="listener">The subscriber. </param>
        /// <param name="identifier">The identifier of the subscriber. </param>
        public void Subscribe([NotNull] IClientBoardStateListener listener, [NotNull] string identifier)
        {
            try
            {
                lock (_stateLock)
                {
                    // Cleaning current state since new state will be called
                    NullifyDataStructures();

                    // Adding subscriber 
                    _clients.Add(identifier, listener);
                }

                // Creating BoardServerShape object and requesting communicator
                Trace.WriteLine("ClientBoardStateManager.Subscribe: Sending fetch state request to communicator.");
                BoardServerShape boardServerShape = new(null, Operation.FETCH_STATE, _currentUserId, currentCheckpointState: _currentCheckpointState);
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
        /// Clears the whiteboard state.
        /// </summary>
        public void ClearWhiteBoard()
        {
            Trace.WriteLine("ClientBoardStateManager.ClearWhiteBoard: Sending Clear_State request to server.");

            // Only user with higher user level can clear complete state.
            if (_userLevel == BoardConstants.HIGH_USER_LEVEL)
            {
                // reset state in sync to currentCheckpointState = 0.
                _clientBoardCommunicator.Send(new(null, Operation.CLEAR_STATE, _currentUserId, currentCheckpointState: BoardConstants.INITIAL_CHECKPOINT_STATE));
            }
        }

        /// <summary>
        /// Initializes the data structures which are maintaining the state. 
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
        /// Nullifies all the state maintaining data structures.
        /// </summary>
        /// <param name="nullifyUndoRedo">Mullify undo-redo stacks or not. By default it is true.</param>
        private void NullifyDataStructures(bool nullifyUndoRedo = true)
        {
            // Emptying current state is equivalent to delete all shapes.
            foreach (string shapeId in _mapIdToBoardShape.Keys)
            {
                _deletedShapeIds.Add(shapeId);
            }

            _mapIdToBoardShape.Clear();
            _mapIdToQueueElement.Clear();
            _priorityQueue.Clear();
            if (nullifyUndoRedo)
            {
                _redoStack.Clear();
                _undoStack.Clear();
            }
            GC.Collect();
        }

        /// <summary>
        /// Updates local state on Fetch State or Fetch Checkpoint from server.
        /// </summary>
        /// <param name="boardServerShape">BoardServerShape object having the whole update.</param>
        /// <returns>List of UXShape to notify client.</returns>
        private List<UXShape> UpdateStateOnFetch(BoardServerShape boardServerShape)
        {
            try
            {
                List<BoardShape> boardShapes = boardServerShape.ShapeUpdates;
                List<UXShape> uXShapes = new();

                // Sorting boardShapes
                boardShapes.Sort(delegate (BoardShape boardShape1, BoardShape boardShape2) { return boardShape1.LastModifiedTime.CompareTo(boardShape2.LastModifiedTime); });

                // updating checkpoint number for subscribe result
                if (boardServerShape.OperationFlag == Operation.FETCH_STATE)
                {
                    _checkpointsNumber = boardServerShape.CheckpointNumber;
                    _clientCheckPointHandler.CheckpointNumber = _checkpointsNumber;
                }

                // updating the state number so its in sync with server
                _currentCheckpointState = boardServerShape.CurrentCheckpointState;

                // updating state
                for (int i = 0; i < boardShapes.Count; i++)
                {
                    string boardShapeId = boardShapes[i].Uid;

                    // insert in id to BoardShape map
                    if (_mapIdToBoardShape.ContainsKey(boardShapeId))
                    {
                        // if already there is some reference present, removing it
                        _mapIdToBoardShape.Remove(boardShapeId);
                        GC.Collect();
                    }
                    _mapIdToBoardShape.Add(boardShapeId, boardShapes[i]);

                    // insert in priority queue and id to QueueElement map
                    QueueElement queueElement = new(boardShapeId, boardShapes[i].LastModifiedTime);
                    if (_mapIdToQueueElement.ContainsKey(boardShapeId))
                    {
                        // if already there is some reference present, removing it
                        QueueElement tempQueueElement = _mapIdToQueueElement[boardShapeId];
                        _priorityQueue.DeleteElement(tempQueueElement);
                        _mapIdToQueueElement.Remove(boardShapeId);
                        GC.Collect();
                    }
                    _mapIdToQueueElement.Add(boardShapeId, queueElement);
                    _priorityQueue.Insert(queueElement);

                    // The ids which were considered to be deleted
                    if (_deletedShapeIds.Contains(boardShapeId))
                    {
                        _deletedShapeIds.Remove(boardShapeId);
                    }

                    // converting BoardShape to UXShape and adding it in the list
                    uXShapes.Add(new(UXOperation.CREATE, boardShapes[i].MainShapeDefiner, boardShapeId, _checkpointsNumber, boardServerShape.OperationFlag));
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
        /// Notifies clients with List of UXShapes. 
        /// </summary>
        /// <param name="uXShapes">List of UX Shapes for UX to render</param>
        private void NotifyClients(List<UXShape> uXShapes)
        {
            try
            {
                lock (this)
                {
                    // Sending each client the updated UXShapes. 
                    foreach (KeyValuePair<string, IClientBoardStateListener> entry in _clients)
                    {
                        Trace.WriteLine("ClientBoardStateManager.NotifyClient: Notifying client.");
                        entry.Value.OnUpdateFromStateManager(uXShapes);
                    }
                    Trace.WriteLine("ClientBoardStateManager.NotifyClient: All clients notified.");
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("ClientBoardStateManager.NotifyClients: Exception occurred.");
                Trace.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Checks pre-condtions for SaveOperation andOnMessageReceived
        /// </summary>
        /// <param name="operation">The operation to be performed.</param>
        /// <param name="id">Id of the shape.</param>
        private void PreConditionChecker(Operation operation, String id)
        {
            if (operation == Operation.CREATE)
            {
                if (_mapIdToBoardShape.ContainsKey(id) || _mapIdToQueueElement.ContainsKey(id))
                {
                    Trace.WriteLine("ClientBoardStateManager.SaveOperation: Create condition failed.");
                    throw new InvalidOperationException("Shape already exists");
                }
            }

            else if (operation == Operation.MODIFY || operation == Operation.DELETE)
            {
                if (!_mapIdToBoardShape.ContainsKey(id) || !_mapIdToQueueElement.ContainsKey(id))
                {
                    Trace.WriteLine("ClientBoardStateManager.SaveOperation: Modify/Delete condition failed.");
                    throw new InvalidOperationException("Shape does not exist");
                }
            }
        }

        /// <summary>
        /// Find all the shapes which were inserted after timestamp
        /// </summary>
        /// <param name="timestamp">Timestamp to compare and find later shapes.</param>
        /// <returns>A Tuple of list of BoardShapes and Elements of Priority Queue.</returns>
        private Tuple<List<BoardShape>, List<QueueElement>> LaterShapes([NotNull] DateTime timestamp)
        {
            List<BoardShape> boardShapes = new();
            List<QueueElement> queueElements = new();
            while(_priorityQueue.GetSize() > BoardConstants.EMPTY_SIZE && _priorityQueue.Top().Timestamp > timestamp)
            {
                boardShapes.Add(_mapIdToBoardShape[_priorityQueue.Top().Id]);
                queueElements.Add(_priorityQueue.Extract());
            }
            return new(boardShapes, queueElements);
        }

        /// <summary>
        /// Converts a list of BoardShapes to UXShapes
        /// </summary>
        /// <param name="boardShapes">BoardShapes to be converted.</param>
        /// <param name="uXOperation">UXoperation for each UXShape</param>
        /// <param name="operationFlag">Operation which requires these changes.</param>
        /// <param name="uXShapes">List of UXShapes in which new UXShapes will be added.</param>
        /// <returns>List of UXShapes corresponding to boardShapes.</returns>
        private List<UXShape> ToUXShapes(List<BoardShape> boardShapes, UXOperation uXOperation, Operation operationFlag, List<UXShape> uXShapes = null)
        {
            // if null then initialize
            if (uXShapes == null)
            {
                uXShapes = new();
            }

            // convert all BoardShapes to UXShapes
            for(int i = 0; i < boardShapes.Count; i++)
            {
                uXShapes.Add(new(uXOperation, boardShapes[i].MainShapeDefiner, boardShapes[i].Uid, _checkpointsNumber, operationFlag));
            }
            return uXShapes;
        }

        /// <summary>
        /// Updates state with boardShape and operation and prepare List of UXShapes to send to UX.
        /// </summary>
        /// <param name="boardShape">The boardShape to be updated.</param>
        /// <param name="operation">The operation to be performed.</param>
        /// <returns>List of UXShapes to update UX.</returns>
        private List<UXShape> ServerOperationUpdate([NotNull] BoardShape boardShape, [NotNull] Operation operation)
        {
            // Only CREATE, MODIFY and DELETE is supported by this function
            if(operation != Operation.CREATE && operation != Operation.DELETE && operation != Operation.MODIFY)
            {
                throw new InvalidOperationException("Operation type not supported.");
            }

            // Recent operation should match the operation desired.
            if(boardShape.RecentOperation != operation)
            {
                throw new InvalidOperationException("Operation type should be same.");
            }

            // checking pre-conditions 
            PreConditionChecker(operation, boardShape.Uid);

            if (operation == Operation.DELETE)
            {
                // updating data structures
                _priorityQueue.DeleteElement(_mapIdToQueueElement[boardShape.Uid]);
                _mapIdToQueueElement.Remove(boardShape.Uid);
                BoardShape tempShape = _mapIdToBoardShape[boardShape.Uid].Clone();
                tempShape.RecentOperation = Operation.DELETE;
                _mapIdToBoardShape.Remove(boardShape.Uid);
                _deletedShapeIds.Add(boardShape.Uid);

                Trace.WriteLine("ClientBoardStateManager.ServerOperationUpdate: Delete case - state successfully updated.");
                return new List<UXShape> { new(UXOperation.DELETE, tempShape.MainShapeDefiner, tempShape.Uid, operationType: Operation.DELETE) };
            }
            else
            {
                // Shapes having last modified time before the current update needs to be deleted in UX first
                Tuple<List<BoardShape>, List<QueueElement>> tuple = LaterShapes(boardShape.LastModifiedTime);
                List<UXShape> uXShapes = ToUXShapes(tuple.Item1, UXOperation.DELETE, operation);

                if(operation == Operation.CREATE)
                {
                    // update data structures
                    _mapIdToBoardShape.Add(boardShape.Uid, boardShape);
                    QueueElement queueElement = new(boardShape.Uid, boardShape.LastModifiedTime);
                    _priorityQueue.Insert(queueElement);
                    _mapIdToQueueElement.Add(boardShape.Uid, queueElement);
                    _deletedShapeIds.Remove(boardShape.Uid);

                    Trace.WriteLine("ClientBoardStateManager.ServerOperationUpdate: Create case - state successfully updated.");
                }
                else
                {
                    // delete previous shape
                    uXShapes.Add(new(UXOperation.DELETE, _mapIdToBoardShape[boardShape.Uid].MainShapeDefiner, boardShape.Uid, operationType: Operation.MODIFY));

                    // update data structures
                    _mapIdToBoardShape[boardShape.Uid] = boardShape;
                    _priorityQueue.IncreaseTimestamp(_mapIdToQueueElement[boardShape.Uid], boardShape.LastModifiedTime);

                    Trace.WriteLine("ClientBoardStateManager.ServerOperationUpdate: Modify case - state successfully updated.");
                }

                // Inserting new shape and reinserting temporarily deleted shapes
                uXShapes.Add(new(UXOperation.CREATE, boardShape.MainShapeDefiner, boardShape.Uid, _checkpointsNumber, operationType: operation));
                uXShapes = ToUXShapes(tuple.Item1, UXOperation.CREATE, operation, uXShapes);

                // populating the priority queue back
                _priorityQueue.Insert(tuple.Item2);
                return uXShapes;
            }
        }

        /// <summary>
        /// Does a state rollback operation for undo-redo.
        /// </summary>
        /// <param name="tuple">Tuple containing the previous state of a shape and latest state of a shape.</param>
        /// <returns>List of UXShapes to notify UX of the change.</returns>
        private List<UXShape> UndoRedoRollback([NotNull] Tuple<BoardShape, BoardShape> tuple)
        {
            // both can't be null
            if (tuple.Item1 == null && tuple.Item2 == null)
            {
                throw new Exception("Both items in tuples are null.");
            }

            // when original operation was create
            else if(tuple.Item1 == null && tuple.Item2 != null)
            {
                Trace.WriteLine("ClientBoardStateManager.UndoRedoRollback: Case of rollback on create.");

                // clone the item and mark it to delete
                BoardShape boardShape = tuple.Item2.Clone();
                boardShape.RecentOperation = Operation.DELETE;
                
                // If server update just deleted the object then return empty list
                if (_deletedShapeIds.Contains(boardShape.Uid))
                {
                    Trace.WriteLine("ClientBoardStateManager.UndoRedoRollback: The item got deleted from server update. Delete on deleted.");
                    return new();
                }

                // adding the id to set
                _deletedShapeIds.Add(boardShape.Uid);

                // send update to server
                _clientBoardCommunicator.Send(new(new List<BoardShape> { boardShape }, Operation.DELETE, _currentUserId, currentCheckpointState: _currentCheckpointState));
                Trace.WriteLine("ClientBoardStateManager.UndoRedoRollback: Sent delete request to server.");

                // update state and send UXShapes to UX
                return ServerOperationUpdate(boardShape, Operation.DELETE);
            }

            // when original operation was delete
            else if(tuple.Item1 != null && tuple.Item2 == null)
            {
                Trace.WriteLine("ClientBoardStateManager.UndoRedoRollback: Case of rollback on delete.");

                // clone the item and mark it to create
                BoardShape boardShape = tuple.Item1.Clone();
                boardShape.RecentOperation = Operation.CREATE;
                
                // send update to server
                _clientBoardCommunicator.Send(new(new List<BoardShape> { boardShape }, Operation.CREATE, _currentUserId, currentCheckpointState: _currentCheckpointState));
                Trace.WriteLine("ClientBoardStateManager.UndoRedoRollback: Sent create request to server.");

                // update state and send UXShapes to UX
                return ServerOperationUpdate(boardShape, Operation.CREATE);
            }

            // when original operation was modify
            else
            {
                Trace.WriteLine("ClientBoardStateManager.UndoRedoRollback: Case of rollback on modify.");

                // mark new one to delete and previous one to create
                BoardShape boardShapePrev = tuple.Item1.Clone();
                BoardShape boardShapeNew = tuple.Item2.Clone();
                boardShapeNew.RecentOperation = Operation.DELETE;
                boardShapePrev.RecentOperation = Operation.CREATE;

                // If server update just deleted the object then return empty list
                if (_deletedShapeIds.Contains(boardShapeNew.Uid))
                {
                    Trace.WriteLine("ClientBoardStateManager.UndoRedoRollback: The item got deleted from server update. Delete on deleted.");
                    return new();
                }

                // send updates to server
                _clientBoardCommunicator.Send(new(new List<BoardShape> { boardShapeNew }, Operation.DELETE, _currentUserId, currentCheckpointState: _currentCheckpointState));
                Trace.WriteLine("ClientBoardStateManager.UndoRedoRollback: Sent delete request to server for new.");
                _clientBoardCommunicator.Send(new(new List<BoardShape> { boardShapePrev }, Operation.CREATE, _currentUserId, currentCheckpointState: _currentCheckpointState));
                Trace.WriteLine("ClientBoardStateManager.UndoRedoRollback: Sent create request to server for old.");

                // Get respective UXShapes and update state
                List<UXShape> uXShapes = ServerOperationUpdate(boardShapeNew, Operation.DELETE);
                uXShapes.AddRange(ServerOperationUpdate(boardShapePrev, Operation.CREATE));
                return uXShapes;
            }
        }

        /// <summary>
        /// Checks the update count and checkpointState conditions. 
        /// </summary>
        /// <param name="serverUpdate">The BoardServerShape containing the update. </param>
        /// <param name="count">Checks update count condition if true.</param>
        /// <param name="checkpointState">Checks current checkpoint state condition if true.</param>
        private void CheckCountAndCurrentCheckpoint(BoardServerShape serverUpdate, bool count=true, bool checkpointState=true)
        {
            // only single update is supported.
            if (count && serverUpdate.ShapeUpdates.Count != BoardConstants.SINGLE_UPDATE_SIZE)
            {
                throw new NotSupportedException("Multiple Shape Operation.");
            }

            // state number should match with the server
            if (checkpointState && serverUpdate.CurrentCheckpointState != _currentCheckpointState)
            {
                Trace.WriteLine("ClientBoardStateManager.OnMessageReceived: Current State doesn't match.");
                throw new Exception("CurrentCheckpointState equality condition failed. Server-Client out of sync.");
            }
        }

    }

}