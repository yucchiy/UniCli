using System;

namespace UniCli.Server.Editor
{
    /// <summary>
    /// Declares the editor state a command requires before it may run.
    ///
    /// Handlers used to assert this themselves, opening ExecuteAsync with
    /// <c>using var scope = _guard.BeginScope(CommandName, ...)</c>. That works only for as long as
    /// every author remembers, and a command that forgets is not obviously broken — it just runs in
    /// a state it was never meant to. Declaring the requirement lets
    /// <see cref="CommandDispatcher"/> enforce it for every command uniformly, including commands
    /// defined outside this package.
    ///
    /// The requirement is also reported through <c>Commands.List</c>, so a client can see why a
    /// command would be refused before calling it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CommandPreconditionAttribute : Attribute
    {
        /// <summary>Editor state this command requires; checked before the handler runs.</summary>
        public GuardCondition EditorState { get; set; }
    }
}
