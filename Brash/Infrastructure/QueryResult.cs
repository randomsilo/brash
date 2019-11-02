using System;
using System.Collections.Generic;

namespace Brash.Infrastructure
{
    public class QueryResult<T>
    {
        public List<T> Models { get; set; }
        public QueryStatus Status { get; set; }
        public string Message { get; set; }
        public Exception CaughtException { get; set; }

        public void UpdateStatus(QueryStatus status, string message)
        {
            Status = status;
            Message = message;
        }
    }
}