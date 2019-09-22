using System;

namespace Brash.Infrastructure
{
    public class ActionResult<T>
    {
        public T Model { get; set; }
        public ActionStatus Status { get; set; }
        public string Message { get; set; }
        public Exception CaughtException { get; set; }

        public void UpdateStatus(ActionStatus status, string message)
        {
            Status = status;
            Message = message;
        }
    }
}