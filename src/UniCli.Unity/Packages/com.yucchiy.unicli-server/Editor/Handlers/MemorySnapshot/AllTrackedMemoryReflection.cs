#if UNICLI_MEMORY_PROFILER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UniCli.Server.Editor.Handlers
{
    internal sealed class AllTrackedMemoryReflection
    {
        private const BindingFlags InstanceMembers =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticMembers =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static AllTrackedMemoryReflection _cached;
        private static string _cachedUnavailableReason;

        private readonly ConstructorInfo _builderCtor;
        private readonly ConstructorInfo _buildArgsCtor;
        private readonly MethodInfo _build;
        private readonly PropertyInfo _rootNodes;
        private readonly PropertyInfo _itemDataName;
        private readonly PropertyInfo _itemDataSize;
        private readonly FieldInfo _memorySizeCommitted;
        private readonly FieldInfo _memorySizeResident;
        private readonly MemberInfo _treeItemData;
        private readonly MemberInfo _treeItemChildren;
        private readonly MemberInfo _treeItemHasChildren;

        private AllTrackedMemoryReflection(Assembly assembly)
        {
            var builderType = GetRequiredType(assembly, "Unity.MemoryProfiler.Editor.UI.AllTrackedMemoryModelBuilder");
            var modelType = GetRequiredType(assembly, "Unity.MemoryProfiler.Editor.UI.AllTrackedMemoryModel");
            var memorySizeType = GetRequiredType(assembly, "Unity.MemoryProfiler.Editor.MemorySize");
            var itemDataType = GetRequiredNestedType(modelType, "ItemData");
            var buildArgsType = GetRequiredNestedType(builderType, "BuildArgs");

            _builderCtor = GetRequiredConstructor(builderType, Type.EmptyTypes);
            _buildArgsCtor = GetRequiredConstructor(buildArgsType, parameterCount: 8);
            _build = GetRequiredMethod(builderType, "Build", parameterCount: 2);
            _rootNodes = GetRequiredProperty(modelType, "RootNodes");
            _itemDataName = GetRequiredProperty(itemDataType, "Name");
            _itemDataSize = GetRequiredProperty(itemDataType, "Size");
            _memorySizeCommitted = GetRequiredField(memorySizeType, "Committed");
            _memorySizeResident = GetRequiredField(memorySizeType, "Resident");

            var treeItemType = _rootNodes.PropertyType.GetGenericArguments()[0];
            _treeItemData = GetRequiredFieldOrProperty(treeItemType, "data");
            _treeItemChildren = GetRequiredFieldOrProperty(treeItemType, "children");
            _treeItemHasChildren = GetRequiredFieldOrProperty(treeItemType, "hasChildren");
        }

        public static bool TryGet(out AllTrackedMemoryReflection reflection, out string unavailableReason)
        {
            if (_cached != null)
            {
                reflection = _cached;
                unavailableReason = "";
                return true;
            }

            if (!string.IsNullOrEmpty(_cachedUnavailableReason))
            {
                reflection = null;
                unavailableReason = _cachedUnavailableReason;
                return false;
            }

            try
            {
                var assembly = FindMemoryProfilerAssembly();
                if (assembly == null)
                    throw new InvalidOperationException("Assembly Unity.MemoryProfiler.Editor is not loaded.");

                _cached = new AllTrackedMemoryReflection(assembly);
                reflection = _cached;
                unavailableReason = "";
                return true;
            }
            catch (Exception ex)
            {
                _cachedUnavailableReason = ex.Message;
                reflection = null;
                unavailableReason = _cachedUnavailableReason;
                return false;
            }
        }

        public MemorySnapshotBreakdownTreeNode[] BuildBreakdownTree(object snapshot, bool residentAvailable)
        {
            var builder = _builderCtor.Invoke(null);
            var args = _buildArgsCtor.Invoke(new object[]
            {
                null,
                null,
                null,
                false,
                false,
                false,
                false,
                null
            });
            var model = _build.Invoke(builder, new[] { snapshot, args });
            var rootItems = ToObjectList((IEnumerable)_rootNodes.GetValue(model, null));
            var nodes = new List<MemorySnapshotBreakdownTreeNode>(
                Math.Min(MemorySnapshotBreakdownTreeLimits.MaxAnalysisNodes, rootItems.Count));

            foreach (var item in SortItemsForRetention(rootItems))
            {
                if (nodes.Count >= MemorySnapshotBreakdownTreeLimits.MaxAnalysisNodes)
                    break;

                AddNode(item, "", 0, residentAvailable, nodes);
            }

            return nodes.ToArray();
        }

        private void AddNode(
            object item,
            string parentPath,
            int depth,
            bool residentAvailable,
            List<MemorySnapshotBreakdownTreeNode> nodes)
        {
            var data = GetValue(_treeItemData, item);
            var name = _itemDataName.GetValue(data, null) as string ?? "";
            var size = _itemDataSize.GetValue(data, null);
            var path = string.IsNullOrEmpty(parentPath) ? name : parentPath + "/" + name;
            var node = new MemorySnapshotBreakdownTreeNode
            {
                Path = path,
                Name = name,
                Depth = depth,
                Allocated = ToLong((ulong)_memorySizeCommitted.GetValue(size)),
                Resident = residentAvailable ? ToLong((ulong)_memorySizeResident.GetValue(size)) : 0,
                ResidentAvailable = residentAvailable
            };

            nodes.Add(node);

            var children = GetChildren(item);
            if (children.Count == 0)
                return;

            if (depth >= MemorySnapshotBreakdownTreeLimits.MaxRetainedDepth)
            {
                node.ChildrenTruncated = true;
                return;
            }

            var retainedChildren = SortItemsForRetention(children)
                .Take(MemorySnapshotBreakdownTreeLimits.MaxChildrenPerNode)
                .ToArray();
            foreach (var child in retainedChildren)
            {
                if (nodes.Count >= MemorySnapshotBreakdownTreeLimits.MaxAnalysisNodes)
                {
                    node.ChildrenTruncated = true;
                    break;
                }

                AddNode(child, path, depth + 1, residentAvailable, nodes);
                node.RetainedChildrenCount++;
            }

            if (retainedChildren.Length < children.Count)
                node.ChildrenTruncated = true;
        }

        private IEnumerable<object> SortItemsForRetention(IEnumerable<object> items)
        {
            return items
                .OrderByDescending(GetRetentionPriority)
                .ThenBy(GetItemName, StringComparer.Ordinal);
        }

        private long GetRetentionPriority(object item)
        {
            var size = GetItemSize(item);
            var committed = ToLong((ulong)_memorySizeCommitted.GetValue(size));
            var resident = ToLong((ulong)_memorySizeResident.GetValue(size));
            return Math.Max(committed, resident);
        }

        private string GetItemName(object item)
        {
            var data = GetValue(_treeItemData, item);
            return _itemDataName.GetValue(data, null) as string ?? "";
        }

        private object GetItemSize(object item)
        {
            var data = GetValue(_treeItemData, item);
            return _itemDataSize.GetValue(data, null);
        }

        private List<object> GetChildren(object item)
        {
            if (!((bool)GetValue(_treeItemHasChildren, item)))
                return new List<object>();

            var children = GetValue(_treeItemChildren, item) as IEnumerable;
            return children == null ? new List<object>() : ToObjectList(children);
        }

        private static List<object> ToObjectList(IEnumerable items)
        {
            var list = new List<object>();
            foreach (var item in items)
                list.Add(item);
            return list;
        }

        private static Assembly FindMemoryProfilerAssembly()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Unity.MemoryProfiler.Editor")
                    return assembly;
            }

            try
            {
                return Assembly.Load("Unity.MemoryProfiler.Editor");
            }
            catch
            {
                return null;
            }
        }

        private static Type GetRequiredType(Assembly assembly, string name)
        {
            return assembly.GetType(name, throwOnError: true);
        }

        private static Type GetRequiredNestedType(Type type, string name)
        {
            var nested = type.GetNestedType(name, BindingFlags.Public | BindingFlags.NonPublic);
            if (nested == null)
                throw new MissingMemberException(type.FullName, name);
            return nested;
        }

        private static ConstructorInfo GetRequiredConstructor(Type type, Type[] parameterTypes)
        {
            var constructor = type.GetConstructor(InstanceMembers, null, parameterTypes, null);
            if (constructor == null)
                throw new MissingMethodException(type.FullName, ".ctor");
            return constructor;
        }

        private static ConstructorInfo GetRequiredConstructor(Type type, int parameterCount)
        {
            var constructor = type
                .GetConstructors(InstanceMembers)
                .FirstOrDefault(candidate => candidate.GetParameters().Length == parameterCount);
            if (constructor == null)
                throw new MissingMethodException(type.FullName, ".ctor");
            return constructor;
        }

        private static MethodInfo GetRequiredMethod(Type type, string name, int parameterCount)
        {
            var method = type
                .GetMethods(InstanceMembers | StaticMembers)
                .FirstOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == parameterCount);
            if (method == null)
                throw new MissingMethodException(type.FullName, name);
            return method;
        }

        private static PropertyInfo GetRequiredProperty(Type type, string name)
        {
            var property = type.GetProperty(name, InstanceMembers);
            if (property == null)
                throw new MissingMemberException(type.FullName, name);
            return property;
        }

        private static FieldInfo GetRequiredField(Type type, string name)
        {
            var field = type.GetField(name, InstanceMembers);
            if (field == null)
                throw new MissingFieldException(type.FullName, name);
            return field;
        }

        private static MemberInfo GetRequiredFieldOrProperty(Type type, string name)
        {
            var field = type.GetField(name, InstanceMembers);
            if (field != null)
                return field;

            var property = type.GetProperty(name, InstanceMembers);
            if (property != null)
                return property;

            throw new MissingMemberException(type.FullName, name);
        }

        private static object GetValue(MemberInfo member, object target)
        {
            var field = member as FieldInfo;
            if (field != null)
                return field.GetValue(target);

            return ((PropertyInfo)member).GetValue(target, null);
        }

        private static long ToLong(ulong value)
        {
            return value > long.MaxValue ? long.MaxValue : (long)value;
        }
    }
}
#endif
