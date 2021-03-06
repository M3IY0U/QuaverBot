using System;

namespace QuaverBot.Entities
{
    public class CommandException : Exception
    {
        // used for aborting commands with a message that don't print a stacktrace
        public CommandException(string message) : base(message) { }
    }
}