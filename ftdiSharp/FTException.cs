using System;

namespace ftdiSharp
{

    public class FTException : Exception
    {
        public FTStatus Status { get; private set; }
        public string API { get; private set; }

        public FTException(string api, FTStatus status)
            : base("'" + api + "' call failed: " + status)
        {
            API = api;
            Status = status;
        }

        public FTException(FTStatus status)
            : base("Call failed: " + status)
        {
            API = "Unknown";
            Status = status;
        }
    }

}