namespace UniCli.Server.Editor
{
    /// <summary>
    /// Unity 6.5 (6000.5) made Object.GetInstanceID() and EditorUtility.InstanceIDToObject(int)
    /// obsolete errors, so the version branch is centralized here. The wire format stays int.
    /// The EntityId -> int implicit conversion is itself an obsolete error in 6000.5, so ids
    /// are extracted through EntityId.ToULong (low 32 bits); int -> EntityId stays implicit.
    /// The boundary is UNITY_6000_5_OR_NEWER: GetEntityId is confirmed present in 6000.5 and
    /// absent in 6000.0; 6000.1-6000.4 are unverified, so they fall back to the legacy APIs.
    /// </summary>
    internal static class UnityObjectIdentity
    {
        public static int GetId(UnityEngine.Object obj)
        {
#if UNITY_6000_5_OR_NEWER
            return unchecked((int)UnityEngine.EntityId.ToULong(obj.GetEntityId()));
#else
            return obj.GetInstanceID();
#endif
        }

        public static UnityEngine.Object Resolve(int id)
        {
#if UNITY_6000_5_OR_NEWER
            return UnityEditor.EditorUtility.EntityIdToObject(id);
#else
            return UnityEditor.EditorUtility.InstanceIDToObject(id);
#endif
        }

        public static T Resolve<T>(int id) where T : UnityEngine.Object
            => Resolve(id) as T;
    }
}
