using System;

namespace Brash.Infrastructure
{
    public class ServiceResult<T>
    {
        public ActionResult<T> PreWorkResult { get; set; }
        public ActionResult<T> WorkResult { get; set; }
        public ActionResult<T> PostWorkResult { get; set; }

        public bool HasError()
        {
            bool errorFound = false;

            if (PreWorkResult?.Status == ActionStatus.ERROR)
            {
                errorFound = true;
            }

            if (WorkResult?.Status == ActionStatus.ERROR)
            {
                errorFound = true;
            }

            if (PostWorkResult?.Status == ActionStatus.ERROR)
            {
                errorFound = true;
            }

            return errorFound;
        }
    }
}