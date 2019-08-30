using System;
using CommandLine;
using Serilog;
using brashcli.Option;
using brashcli.Process;

namespace brashcli
{
    public class Global
    {
        public const string IDPATTERN_ASKID = "AskId";
        public const string IDPATTERN_ASKGUID = "AskGuid";
        public const string IDPATTERN_ASKVERSION = "AskVersion";

        public const string TRACKINGPATTERN_NONE = "None";
        public const string TRACKINGPATTERN_AUDIT = "Audit";
        public const string TRACKINGPATTERN_AUDITPRESERVE = "AuditPreserve";
        public const string TRACKINGPATTERN_VERSION = "AuditVersion";
        public const string TRACKINGPATTERN_SESSION = "Session";
        public const string TRACKINGPATTERN_DEVICE = "Device";

        public const string ADDITIONALPATTERN_CHOICE = "Choice";
    }
}