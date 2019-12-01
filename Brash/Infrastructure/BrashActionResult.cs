using System;

namespace Brash.Infrastructure
{
    public class BrashActionResult<T>
    {
        public T Model { get; set; }
        public BrashActionStatus Status { get; set; }
        public string Message { get; set; }
        public Exception CaughtException { get; set; }

        public void UpdateStatus(BrashActionStatus status, string message)
        {
            Status = status;
            Message = message;
        }
    }
}