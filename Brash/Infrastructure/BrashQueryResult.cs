using System;
using System.Collections.Generic;

namespace Brash.Infrastructure
{
    public class BrashQueryResult<T>
    {
        public List<T> Models { get; set; }
        public BrashQueryStatus Status { get; set; }
        public string Message { get; set; }
        public Exception CaughtException { get; set; }

        public void UpdateStatus(BrashQueryStatus status, string message)
        {
            Status = status;
            Message = message;
        }
    }
}