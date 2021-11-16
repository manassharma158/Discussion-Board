﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard
{
    public class ClientToServerData
    {
        /// <summary>
        /// Parametric constructor to initialise the fields
        /// </summary>
        /// <param name="eventName"> The name of the event </param>
        /// <param name="clientName"> Name of the user </param>
        /// <param name="clientID"> The ID of the user (-1, if not known/assigned) </param>
        public ClientToServerData(string eventName, string clientName, int clientID = -1)
        {
            eventType = eventName;
            username = clientName;
            userID = clientID;
        }

        public string eventType;
        public string username;
        public int userID;
    }
}
