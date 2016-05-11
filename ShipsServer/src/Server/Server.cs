﻿using System;
using System.Collections.Generic;
using System.Linq;
using ShipsServer.Networking;
using ShipsServer.Server.Battle;

namespace ShipsServer.Server
{
    class Server
    {
        public static UInt32 ServerLoopCounter { get; set; }

        private static Server _instance;

        public Queue<Session> _sessionsQueue;
        public List<Session> _sessionsList;

        private object _sessionQueueLock = new object();
        private object _sessionLock = new object();

        public Server()
        {
            _sessionsQueue = new Queue<Session>();
            _sessionsList = new List<Session>();
        }

        public static Server Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Server();
                return _instance;
            }
        }

        public void Update()
        {
            lock (_sessionQueueLock)
            {
                while (_sessionsQueue.Count != 0)
                {
                    var session = _sessionsQueue.Dequeue();
                    AddSession(session);
                }
            }

            if (_sessionsList.Count == 0)
                return;

            var oldSessions = new List<Session>();
            lock (_sessionLock)
            {
                oldSessions.AddRange(_sessionsList.Where(session => !session.Update()));
            }

            if (oldSessions.Count != 0)
            {
                foreach (var session in oldSessions)      
                    RemoveSession(session.AccountId);
            }

            BattleMgr.Instance.Update();
        }

        public void AddSessionQueue(Session session)
        {
            lock (_sessionQueueLock)
            {
                _sessionsQueue.Enqueue(session);
            }
        }

        private void AddSession(Session session)
        {
            RemoveSession(session.AccountId);
            lock (_sessionLock)
            {
                _sessionsList.Add(session);
            }
        }

        private void RemoveSession(UInt32 accountId)
        {
            lock (_sessionLock)
            {
                _sessionsList.RemoveAll(x => x.AccountId == accountId);
            }
        }
    }
}
