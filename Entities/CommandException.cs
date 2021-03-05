using System;

namespace QuaverBot.Entities
{
    public class CommandException : Exception
    {
        public CommandException(string message) : base(message) { }
    }
}