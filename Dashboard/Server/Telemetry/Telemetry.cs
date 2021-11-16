using System;
using System.Collections.Generic;
using Content;

namespace Dashboard.Server.Telemetry
{
    public class Telemetry : ITelemetryAnalysisModel
    {
        /// <summary>
        ///     used by SM to simplify the ChatContext and save it by persistance module.
        /// </summary>
        /// <params> Array of ChatContext objects which contains information about messages of each thread </params>
        public void SaveAllAnalytics(ChatContext[] AllMessages)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        ///     Used by SM to get the TelemetryAnalyticsModel to transfer
        ///     back to UX module to display the analytics
        /// </summary>
        /// <params> Array of ChatContext objects which contains information about messages of each thread </params>
        /// <returns>
        ///     Returns TelemetryAnalyticsModel object which contains analytics of session
        /// </returns>
        public TelemetryAnalyticsModel getTelemetryAnalytics(ChatContext[] AllMessages)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     returns a dictionary with DateTime as key and int as value
        ///     which indicates UserCount at corresponding DateTime
        /// </summary>
        /// <params>
        ///     takes the session data which contains the users list
        ///     and whenever the session data changes, I get notified,
        ///     based on it I can store the timestamp
        /// </params>
        /// <returns>
        ///     A dictionary of DateTime as key and userCount as value
        /// </returns>
        private Dictionary<DateTime, int> UserCountVsTimeStamp(SessionData newSession)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        ///     returns a dictionary of UserData as key and chatCount as value
        ///     indicating chat count of each user.
        /// </summary>
        /// <params> Takes array of ChatContext object which contains information about Threads </params>
        /// <returns>
        ///     Dictionary of userData as key and int as value
        /// </returns>
        private Dictionary<int, int> UserVsChatCount(ChatContext[] AllMess)
        {
            var UserChatCountDic = new Dictionary<int, int>();
            foreach (var currThread in AllMess)
            foreach (var currMessage in currThread.MsgList)
                UserChatCountDic[currMessage.SenderId]++;
            return UserChatCountDic;
        }

        /// <summary>
        ///     returns the list of UserData which indicates insincere members
        ///     in the session who were present in the session but for less
        ///     than a certain minimum time
        /// </summary>
        /// <params> Takes the session data which contains the list of users </params>
        /// <returns> A list of type UserDat  </returns>
        private List<int> irrelevantMembers(SessionData newSession)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     returns a dictionary which indicates score for each session
        /// </summary>
        /// <params> Takes the list of ServerDataToSave, which contains information about each session </params>
        /// <returns>
        ///     Dictionary with SessionData as key and its score(chatcount * no. of users) as value
        /// </returns>
        private Dictionary<SessionData, int> sessionVsScore(List<ServerDataToSave> AllSessionData)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        ///     returns a dictionary which indicates user count in each session
        /// </summary>
        /// <params> Takes the list of ServerDataToSave, which contains information about each session </params>
        /// <returns>
        ///     Dictionary with SessionData as key and User as value
        /// </returns>
        private Dictionary<SessionData, int> sessionVsUserCount(List<ServerDataToSave> AllSessionData)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        ///     returns a dictionary which indicates chat count in each session
        /// </summary>
        /// <params> Takes the list of ServerDataToSave, which contains information about each session </params>
        /// <returns>
        ///     Dictionary with SessionData as key and Chat count(int) as value
        /// </returns>
        private Dictionary<SessionData, int> sessionVsChatCount(List<ServerDataToSave> AllSessionData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Updates the SessionData when it changes
        /// </summary>
        /// <params> Takes the list of ServerData </params>
        private void OnAnalyticsChanged(SessionData newSession)
        {
            throw new NotImplementedException();
        }
    }
}