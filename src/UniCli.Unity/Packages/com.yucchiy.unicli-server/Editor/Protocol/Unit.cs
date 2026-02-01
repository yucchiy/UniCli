using System;

namespace UniCli.Protocol
{
    /// <summary>
    /// Represents a type with no value
    /// </summary>
    [Serializable]
    public struct Unit
    {
        public static readonly Unit Value = new Unit();
    }
}
