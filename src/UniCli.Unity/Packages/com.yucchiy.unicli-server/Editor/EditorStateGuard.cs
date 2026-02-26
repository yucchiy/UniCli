using System;
using UnityEditor;

namespace UniCli.Server.Editor
{
    [Flags]
    public enum GuardCondition
    {
        NotPlaying = 1,
        NotCompiling = 2,
        NotPlayingOrCompiling = NotPlaying | NotCompiling,
    }

    public sealed class EditorStateGuard
    {
        private string _activeCommand;

        public GuardScope BeginScope(string commandName, GuardCondition condition)
        {
            if ((condition & GuardCondition.NotPlaying) != 0 && EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException($"Cannot execute '{commandName}' while in Play Mode.");

            if ((condition & GuardCondition.NotCompiling) != 0 && EditorApplication.isCompiling)
                throw new InvalidOperationException($"Cannot execute '{commandName}' while compiling.");

            _activeCommand = commandName;
            return new GuardScope(this);
        }

        public readonly struct GuardScope : IDisposable
        {
            private readonly EditorStateGuard _guard;

            internal GuardScope(EditorStateGuard guard) => _guard = guard;

            public void Dispose()
            {
                if (_guard != null)
                    _guard._activeCommand = null;
            }
        }
    }
}
