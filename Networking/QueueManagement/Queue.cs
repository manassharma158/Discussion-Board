﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Networking
{
    public class Queue : IQueue
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<Packet>> _multiLevelQueue;
        private readonly ConcurrentDictionary<string, int> _priorityMap;
        private int _avoidStateChange;
        private string _currentModuleIdentifier;
        private int _currentQueue;
        private int _currentWeight;
        private List<string> _moduleIdentifiers;
        private int _queueSize;
        private readonly object lockObj = new();

        /// <summary>
        ///     Queue constructor initializes multilevel queue, priority map dictionaries and also the state of the queue.
        /// </summary>
        public Queue()
        {
            _multiLevelQueue = new ConcurrentDictionary<string, ConcurrentQueue<Packet>>();
            _priorityMap = new ConcurrentDictionary<string, int>();
            _currentQueue = 0;
            _currentWeight = 0;
            _avoidStateChange = 0;
            _queueSize = 0;
            Trace.WriteLine("Initializing Queue Module");
        }

        /// <summary>
        ///     Register Module into the multilevel queue.
        /// </summary>
        /// <param name="moduleId">Unique Id for module.</param>
        /// <param name="priority">Priority Number indicating the weight to be given to the module.</param>
        public void RegisterModule(string moduleId, int priority)
        {
            if (priority <= 0) throw new Exception("Priority should be positive integer");

            // Adding <moduleId, Queue> keyValuePair to the _multiLevelQueue dictionary 
            if (!_multiLevelQueue.TryAdd(moduleId, new ConcurrentQueue<Packet>()))
                throw new Exception("Adding Queue to MultiLevelQueue Failed!");

            // Adding <moduleId, priority> keyValuePair to the _priorityMap dictionary
            if (!_priorityMap.TryAdd(moduleId, priority))
            {
                _multiLevelQueue.TryRemove(moduleId, out _);
                throw new Exception("Priority Map cannot overwrite existing key");
            }

            // Getting the moduleIds in the priority order
            var orderedIdPairs = _priorityMap.OrderByDescending(s => s.Value);
            _moduleIdentifiers = new List<string>();
            foreach (var keyValuePair in orderedIdPairs)
                _moduleIdentifiers.Add(keyValuePair.Key);

            if (_avoidStateChange == 0)
            {
                // Assigning the _currentWeight to the top priority queue's weight
                var moduleIdentifier = _moduleIdentifiers[_currentQueue];
                _currentWeight = _priorityMap[moduleIdentifier];
                _currentModuleIdentifier = moduleIdentifier;
            }
            else
            {
                _currentQueue = _moduleIdentifiers.FindIndex(x => x == _currentModuleIdentifier);
            }

            Trace.WriteLine($"Module Registered with ModuleIdentifier: {moduleId} and Priority: {priority.ToString()}");
        }

        /// <summary>
        ///     Size of the queue.
        /// </summary>
        /// <returns>The number of packets the queue holds.</returns>
        public int Size()
        {
            return _queueSize;
        }

        /// <summary>
        ///     Dequeues all the elements.
        /// </summary>
        public void Clear()
        {
            Trace.WriteLine("Clearing all packets from the queue");
            lock (lockObj)
            {
                _queueSize = 0;
            }

            foreach (var keyValuePair in _multiLevelQueue) keyValuePair.Value.Clear();
        }

        /// <summary>
        ///     Enqueues an object of IPacket.
        /// </summary>
        public void Enqueue(Packet item)
        {
            var moduleIdentifier = item.ModuleIdentifier;

            // Check if the _multiLevelQueue dictionary contains the moduleIdentifier
            if (_multiLevelQueue.ContainsKey(moduleIdentifier))
            {
                lock (lockObj)
                {
                    _queueSize += 1;
                }

                _multiLevelQueue[moduleIdentifier].Enqueue(item);
            }
            else
            {
                throw new Exception("Key Error: Packet holds invalid module identifier");
            }

            Trace.WriteLine("Packet Enqueued");
        }

        /// <summary>
        ///     Dequeues an item from the queue and returns the item.
        /// </summary>
        /// <returns>Returns the dequeued packet from the queue.</returns>
        public Packet Dequeue()
        {
            if (!IsEmpty())
            {
                FindNext(); // Populates the fields of _currentQueue, _currentWeight corresponding to the next packet

                var moduleIdentifier = _moduleIdentifiers[_currentQueue];
                _multiLevelQueue[moduleIdentifier].TryDequeue(out var packet);
                _currentWeight -= 1;
                _avoidStateChange = 1;
                Trace.WriteLine("Dequeuing Packet");
                lock (lockObj)
                {
                    _queueSize -= 1;
                }

                return packet;
            }

            throw new Exception("Cannot Dequeue empty queue");
        }

        /// <summary>
        ///     Peeks into the first element of the queue.
        /// </summary>
        /// <returns>Returns the peeked packet from the queue.</returns>
        public Packet Peek()
        {
            if (!IsEmpty())
            {
                FindNext(); // Populates the fields of _currentQueue, _currentWeight corresponding to the next packet

                var moduleIdentifier = _moduleIdentifiers[_currentQueue];
                _multiLevelQueue[moduleIdentifier].TryPeek(out var packet);

                Trace.WriteLine("Peeking into the queue");
                return packet;
            }

            throw new Exception("Cannot Peek into empty queue");
        }

        /// <summary>
        ///     Checks if the queue is empty or not.
        /// </summary>
        /// <returns>True if queue is empty and false otherwise.</returns>
        public bool IsEmpty()
        {
            return _queueSize == 0;
        }

        /// <summary>
        ///     Sets the _currentQueue, _currentWeight variables corresponding to the next packet to be dequeued/peeked.
        /// </summary>
        private void FindNext()
        {
            var moduleIdentifier = _moduleIdentifiers[_currentQueue];

            if (_currentWeight == 0)
            {
                // Go to the next queue and set the _currentWeight
                _currentQueue = (_currentQueue + 1) % _multiLevelQueue.Count;
                moduleIdentifier = _moduleIdentifiers[_currentQueue];
                _currentWeight = _priorityMap[moduleIdentifier];
                _currentModuleIdentifier = moduleIdentifier;
                FindNext(); // To circumvent the case of the next queue having no packets
            }
            else
            {
                // If the current queue has no packets, otherwise do nothing
                if (_multiLevelQueue[moduleIdentifier].Count == 0)
                    // Finding the next queue with packets
                    while (_multiLevelQueue[moduleIdentifier].Count == 0)
                    {
                        _currentQueue = (_currentQueue + 1) % _moduleIdentifiers.Count;
                        moduleIdentifier = _moduleIdentifiers[_currentQueue];
                        _currentWeight = _priorityMap[moduleIdentifier];
                        _currentModuleIdentifier = moduleIdentifier;
                    }
            }
        }
    }
}