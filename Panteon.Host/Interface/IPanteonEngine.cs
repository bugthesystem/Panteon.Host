using System;
using System.Collections.Generic;
using Panteon.Sdk;
using Panteon.Sdk.History;

namespace Panteon.Host.Interface
{
    public interface IPanteonEngine
    {
        IEnumerable<IPanteonWorker> GetTasks();
        void Start();
        void Stop(bool immediate);
        bool StopTask(string name);
        bool StartTask(string name, DateTimeOffset lastKnownEvent=(default(DateTimeOffset)));
        bool UpdateTask(string name, string scheduleExpression);
        IEnumerable<HistoryModel> LoadHistory(string name, DateTime? @from, DateTime? to);
    }
}