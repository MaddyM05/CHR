using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Chorus.WindowsReader.Common.Logger
{
    [Serializable]
    public class ChorusCustomException : Exception
    {
        public string EventName { get; set; }
        public Dictionary<string, string> Parameters { get; set; }

        public ChorusCustomException() : base() { }

        public ChorusCustomException(string message)
            : base(message) { }

        public ChorusCustomException(string message, Exception inner)
            : base(message, inner) { }

        public ChorusCustomException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        public ChorusCustomException(Exception inner)
            : base(inner.Message, inner) { }

        public ChorusCustomException(string message, string eventName)
            : this(message)
        {
            EventName = eventName;
        }

        public ChorusCustomException(string message, string eventName, Exception inner)
            : this(message, inner)
        {
            EventName = eventName;
        }

        public ChorusCustomException(string message, string eventName, Dictionary<string, string> parameters)
            : this(message)
        {
            EventName = eventName;
            Parameters = parameters;
        }
    }

}
