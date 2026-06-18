using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor
{
    internal static class UnityObjectIdUtility
    {
#if UNITY_6000_5_OR_NEWER
        // Unity 6.5 EntityIds are no longer safely representable as ints, so keep
        // the existing CLI int contract as session-local handles.
        private static readonly Dictionary<EntityId, int> IdsByEntityId = new();
        private static readonly Dictionary<int, EntityId> EntityIdsById = new();
        private static int NextId = 1;
#endif

        public static int GetId(Object obj)
        {
#if UNITY_6000_5_OR_NEWER
            var entityId = obj.GetEntityId();
            if (IdsByEntityId.TryGetValue(entityId, out var id))
                return id;

            id = NextId++;
            IdsByEntityId[entityId] = id;
            EntityIdsById[id] = entityId;
            return id;
#else
            return obj.GetInstanceID();
#endif
        }

        public static Object ToObject(int id)
        {
            if (id == 0)
                return null;

#if UNITY_6000_5_OR_NEWER
            if (!EntityIdsById.TryGetValue(id, out var entityId))
                return null;

            var obj = EditorUtility.EntityIdToObject(entityId);
            if (obj != null)
                return obj;

            EntityIdsById.Remove(id);
            IdsByEntityId.Remove(entityId);
            return null;
#else
            return EditorUtility.InstanceIDToObject(id);
#endif
        }
    }
}
