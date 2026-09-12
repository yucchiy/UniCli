using System;
using System.Collections.Generic;
using UnityEditor;

namespace UniCli.Server.Editor
{
    /// <summary>
    /// Reads the editor state a precondition check needs. Exists so the check itself can be unit
    /// tested — <see cref="EditorApplication"/>'s statics cannot be put into a chosen state from a
    /// test.
    /// </summary>
    public interface IEditorStateProbe
    {
        bool IsPlaying { get; }
        bool IsCompiling { get; }
    }

    internal sealed class EditorApplicationStateProbe : IEditorStateProbe
    {
        public static readonly EditorApplicationStateProbe Instance = new();
        public bool IsPlaying => EditorApplication.isPlayingOrWillChangePlaymode;
        public bool IsCompiling => EditorApplication.isCompiling;
    }

    /// <summary>What a command declared about itself, resolved once per handler type.</summary>
    public readonly struct CommandPrecondition
    {
        public static readonly CommandPrecondition None = default;

        public readonly GuardCondition EditorState;

        public CommandPrecondition(GuardCondition editorState) => EditorState = editorState;

        public bool IsEmpty => EditorState == 0;

        /// <summary>The requirement as a stable string for command metadata; null when none.</summary>
        public string EditorStateName => EditorState == 0 ? null : EditorState.ToString();
    }

    /// <summary>
    /// The pre-execution checks the dispatcher runs for every command.
    ///
    /// <see cref="Handlers.DirtyScenePolicy"/> anticipated this: "If a third kind of pre-execution
    /// check appears, consolidate them into a precondition pipeline on CommandDispatcher rather
    /// than merging the checks into one type." This is that pipeline, starting with the editor
    /// state checks, which are the ones decidable from the command's identity alone.
    ///
    /// The dirty-scene policy deliberately stays where it is: it is driven by a request field and
    /// needs to know which scenes a given request affects, and request deserialization happens
    /// below this layer.
    /// </summary>
    public static class CommandPreconditions
    {
        private static readonly Dictionary<Type, CommandPrecondition> s_Cache = new();

        /// <summary>Reads a handler type's declaration. Cached; safe to call per dispatch.</summary>
        public static CommandPrecondition Resolve(Type handlerType)
        {
            if (handlerType == null)
                return CommandPrecondition.None;

            lock (s_Cache)
            {
                if (s_Cache.TryGetValue(handlerType, out var cached))
                    return cached;
            }

            var attribute = (CommandPreconditionAttribute)Attribute.GetCustomAttribute(
                handlerType, typeof(CommandPreconditionAttribute), inherit: false);

            var resolved = attribute == null
                ? CommandPrecondition.None
                : new CommandPrecondition(attribute.EditorState);

            lock (s_Cache)
            {
                s_Cache[handlerType] = resolved;
            }

            return resolved;
        }

        /// <summary>
        /// Returns null when the command may proceed, or the reason it may not. The message is what
        /// the caller sees, so it names the command and says what to do about it.
        /// </summary>
        public static string Check(CommandPrecondition precondition, string commandName, IEditorStateProbe probe)
        {
            if (probe == null)
                throw new ArgumentNullException(nameof(probe));

            if ((precondition.EditorState & GuardCondition.NotPlaying) != 0 && probe.IsPlaying)
                return $"Cannot execute '{commandName}' while in Play Mode. Exit Play Mode first (PlayMode.Exit).";

            if ((precondition.EditorState & GuardCondition.NotCompiling) != 0 && probe.IsCompiling)
                return $"Cannot execute '{commandName}' while compiling. Wait for compilation to finish and retry.";

            return null;
        }

        /// <summary>Convenience overload using the live editor state.</summary>
        public static string Check(CommandPrecondition precondition, string commandName)
            => Check(precondition, commandName, EditorApplicationStateProbe.Instance);

        internal static void ClearCacheForTesting()
        {
            lock (s_Cache)
            {
                s_Cache.Clear();
            }
        }
    }
}
