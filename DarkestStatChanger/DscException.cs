using System;

namespace DarkestStatChanger
{
    /// <summary>
    /// Application-specific exception carrying a unique error code.
    /// Format: [Exxx] message
    /// </summary>
    public class DscException : Exception
    {
        public string Code { get; }

        public DscException(string code, string message, Exception inner = null)
            : base($"[{code}] {message}", inner)
        {
            Code = code;
        }
    }
}
