namespace UniCli.Server.Editor
{
    /// <summary>
    /// Unity 6.5 (6000.5) made Object.GetInstanceID() and EditorUtility.InstanceIDToObject(int)
    /// obsolete errors and widened object ids to the 64-bit EntityId, so the version branch is
    /// centralized here. The wire format is long: on Unity 6.5 ids carry the full EntityId raw
    /// value (EntityId.ToULong / FromULong, the only sanctioned raw round-trip), on older
    /// versions the legacy int instance id widened to long.
    /// Ids are session-scoped opaque handles; do not persist them or interpret their bits.
    /// The boundary is UNITY_6000_5_OR_NEWER: GetEntityId is confirmed present in 6000.5 and
    /// absent in 6000.0; 6000.1-6000.4 are unverified, so they fall back to the legacy APIs.
    /// </summary>
    internal static class UnityObjectIdentity
    {
        public static long GetId(UnityEngine.Object obj)
        {
#if UNITY_6000_5_OR_NEWER
            // EntityId is 64-bit in 6000.5 with meaningful upper bits (measured); never truncate.
            return unchecked((long)UnityEngine.EntityId.ToULong(obj.GetEntityId()));
#else
            return obj.GetInstanceID();
#endif
        }

        public static UnityEngine.Object Resolve(long id)
        {
#if UNITY_6000_5_OR_NEWER
            // Raw lookup via FromULong, the exact inverse of GetId. Ids truncated to 32 bits
            // are intentionally not resolved: the official migration guide treats truncated
            // EntityId values as errors, and no released UniCli exchanged 32-bit ids with a
            // Unity 6.5 server.
            return UnityEditor.EditorUtility.EntityIdToObject(
                UnityEngine.EntityId.FromULong(unchecked((ulong)id)));
#else
            // Legacy ids always fit in int; treat out-of-range values as nonexistent objects.
            if (id < int.MinValue || id > int.MaxValue)
                return null;
            return UnityEditor.EditorUtility.InstanceIDToObject((int)id);
#endif
        }

        public static T Resolve<T>(long id) where T : UnityEngine.Object
            => Resolve(id) as T;
    }
}
