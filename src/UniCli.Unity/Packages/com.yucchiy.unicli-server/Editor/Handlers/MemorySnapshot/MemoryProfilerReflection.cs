#if UNICLI_MEMORY_PROFILER
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UniCli.Server.Editor.Handlers
{
    internal sealed class MemoryProfilerReflection
    {
        private const BindingFlags InstanceMembers =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticMembers =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static MemoryProfilerReflection _cached;
        private static string _cachedUnavailableReason;

        private readonly ConstructorInfo _fileReaderCtor;
        private readonly ConstructorInfo _cachedSnapshotCtor;
        private readonly ConstructorInfo _summaryBuilderCtor;

        private readonly MethodInfo _fileReaderOpen;
        private readonly MethodInfo _fileReaderClose;
        private readonly MethodInfo _cachedSnapshotDispose;
        private readonly MethodInfo _managedDataCrawlerCrawl;
        private readonly MethodInfo _summaryBuild;

        private readonly PropertyInfo _snapshotMetaData;
        private readonly PropertyInfo _snapshotTimeStamp;
        private readonly PropertyInfo _snapshotHasSystemMemoryResidentPages;
        private readonly PropertyInfo _snapshotCrawledData;
        private readonly FieldInfo _snapshotNativeObjects;
        private readonly FieldInfo _snapshotNativeTypes;
        private readonly FieldInfo _snapshotTypeDescriptions;

        private readonly FieldInfo _nativeObjectsCount;
        private readonly FieldInfo _nativeObjectsNames;
        private readonly FieldInfo _nativeObjectsSizes;
        private readonly FieldInfo _nativeObjectsTypeIndices;
        private readonly FieldInfo _nativeObjectsInstanceIds;

        private readonly FieldInfo _nativeTypesNames;
        private readonly FieldInfo _typeDescriptionsNames;

        private readonly FieldInfo _managedDataManagedObjects;
        private readonly FieldInfo _managedObjectTypeDescription;
        private readonly FieldInfo _managedObjectSize;

        private readonly FieldInfo _metadataPlatform;
        private readonly FieldInfo _metadataUnityVersion;
        private readonly FieldInfo _metadataIsEditorCapture;
        private readonly FieldInfo _metadataCaptureFlags;

        private readonly PropertyInfo _summaryRows;
        private readonly PropertyInfo _summaryRowName;
        private readonly PropertyInfo _summaryRowBaseSize;
        private readonly PropertyInfo _summaryRowResidentSizeUnavailable;
        private readonly FieldInfo _memorySizeCommitted;
        private readonly FieldInfo _memorySizeResident;

        private static readonly Dictionary<Type, DynamicArrayAccessor> DynamicArrayAccessors =
            new Dictionary<Type, DynamicArrayAccessor>();
        private static readonly Dictionary<Type, DynamicArrayStructFieldAccessor> DynamicArrayStructFieldAccessors =
            new Dictionary<Type, DynamicArrayStructFieldAccessor>();

        private MemoryProfilerReflection(Assembly assembly)
        {
            var fileReaderType = GetRequiredType(assembly, "Unity.MemoryProfiler.Editor.Format.QueriedSnapshot.FileReader");
            var cachedSnapshotType = GetRequiredType(assembly, "Unity.MemoryProfiler.Editor.CachedSnapshot");
            var managedDataCrawlerType = GetRequiredType(assembly, "Unity.MemoryProfiler.Editor.ManagedDataCrawler");
            var summaryBuilderType = GetRequiredType(assembly, "Unity.MemoryProfiler.Editor.UI.AllMemorySummaryModelBuilder");
            var summaryModelType = GetRequiredType(assembly, "Unity.MemoryProfiler.Editor.UI.MemorySummaryModel");
            var memorySizeType = GetRequiredType(assembly, "Unity.MemoryProfiler.Editor.MemorySize");
            var managedDataType = GetRequiredType(assembly, "Unity.MemoryProfiler.Editor.ManagedData");
            var managedObjectInfoType = GetRequiredType(assembly, "Unity.MemoryProfiler.Editor.ManagedObjectInfo");

            var nativeObjectsType = GetRequiredNestedType(cachedSnapshotType, "NativeObjectEntriesCache");
            var nativeTypesType = GetRequiredNestedType(cachedSnapshotType, "NativeTypeEntriesCache");
            var typeDescriptionsType = GetRequiredNestedType(cachedSnapshotType, "TypeDescriptionEntriesCache");
            var summaryRowType = GetRequiredNestedType(summaryModelType, "Row");

            _fileReaderCtor = GetRequiredConstructor(fileReaderType, Type.EmptyTypes);
            _cachedSnapshotCtor = GetRequiredConstructor(cachedSnapshotType, new[] { fileReaderType.GetInterface("IFileReader") ?? GetRequiredType(assembly, "Unity.MemoryProfiler.Editor.Format.QueriedSnapshot.IFileReader") });
            _summaryBuilderCtor = GetRequiredConstructor(summaryBuilderType, new[] { cachedSnapshotType, cachedSnapshotType });

            _fileReaderOpen = GetRequiredMethod(fileReaderType, "Open", new[] { typeof(string) });
            _fileReaderClose = GetRequiredMethod(fileReaderType, "Close", Type.EmptyTypes);
            _cachedSnapshotDispose = GetRequiredMethod(cachedSnapshotType, "Dispose", Type.EmptyTypes);
            _managedDataCrawlerCrawl = GetRequiredMethod(managedDataCrawlerType, "Crawl", new[] { cachedSnapshotType });
            _summaryBuild = GetRequiredMethod(summaryBuilderType, "Build", Type.EmptyTypes);

            _snapshotMetaData = GetRequiredProperty(cachedSnapshotType, "MetaData");
            _snapshotTimeStamp = GetRequiredProperty(cachedSnapshotType, "TimeStamp");
            _snapshotHasSystemMemoryResidentPages = GetRequiredProperty(cachedSnapshotType, "HasSystemMemoryResidentPages");
            _snapshotCrawledData = GetRequiredProperty(cachedSnapshotType, "CrawledData");
            _snapshotNativeObjects = GetRequiredField(cachedSnapshotType, "NativeObjects");
            _snapshotNativeTypes = GetRequiredField(cachedSnapshotType, "NativeTypes");
            _snapshotTypeDescriptions = GetRequiredField(cachedSnapshotType, "TypeDescriptions");

            _nativeObjectsCount = GetRequiredField(nativeObjectsType, "Count");
            _nativeObjectsNames = GetRequiredField(nativeObjectsType, "ObjectName");
            _nativeObjectsSizes = GetRequiredField(nativeObjectsType, "Size");
            _nativeObjectsTypeIndices = GetRequiredField(nativeObjectsType, "NativeTypeArrayIndex");
            _nativeObjectsInstanceIds = GetRequiredField(nativeObjectsType, "InstanceId");

            _nativeTypesNames = GetRequiredField(nativeTypesType, "TypeName");
            _typeDescriptionsNames = GetRequiredField(typeDescriptionsType, "TypeDescriptionName");

            _managedDataManagedObjects = GetRequiredField(managedDataType, "m_ManagedObjects");
            _managedObjectTypeDescription = GetRequiredField(managedObjectInfoType, "ITypeDescription");
            _managedObjectSize = GetRequiredField(managedObjectInfoType, "Size");

            var metaDataType = _snapshotMetaData.PropertyType;
            _metadataPlatform = GetRequiredField(metaDataType, "Platform");
            _metadataUnityVersion = GetRequiredField(metaDataType, "UnityVersion");
            _metadataIsEditorCapture = GetRequiredField(metaDataType, "IsEditorCapture");
            _metadataCaptureFlags = GetRequiredField(metaDataType, "CaptureFlags");

            _summaryRows = GetRequiredProperty(summaryModelType, "Rows");
            _summaryRowName = GetRequiredProperty(summaryRowType, "Name");
            _summaryRowBaseSize = GetRequiredProperty(summaryRowType, "BaseSize");
            _summaryRowResidentSizeUnavailable = GetRequiredProperty(summaryRowType, "ResidentSizeUnavailable");
            _memorySizeCommitted = GetRequiredField(memorySizeType, "Committed");
            _memorySizeResident = GetRequiredField(memorySizeType, "Resident");
        }

        public static bool TryGet(out MemoryProfilerReflection reflection, out string unavailableReason)
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

                _cached = new MemoryProfilerReflection(assembly);
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

        public object CreateFileReader()
        {
            return _fileReaderCtor.Invoke(null);
        }

        public string OpenFileReader(object reader, string path)
        {
            var result = _fileReaderOpen.Invoke(reader, new object[] { path });
            return result?.ToString() ?? "Unknown";
        }

        public void CloseFileReader(object reader)
        {
            _fileReaderClose.Invoke(reader, null);
        }

        public object CreateCachedSnapshot(object reader)
        {
            return _cachedSnapshotCtor.Invoke(new[] { reader });
        }

        public void DisposeCachedSnapshot(object snapshot)
        {
            _cachedSnapshotDispose.Invoke(snapshot, null);
        }

        public void CrawlManagedData(object snapshot)
        {
            var enumerator = (IEnumerator)_managedDataCrawlerCrawl.Invoke(null, new[] { snapshot });
            while (enumerator.MoveNext())
            {
            }
        }

        public SnapshotAnalysis ExtractAnalysis(object snapshot, string snapshotPath)
        {
            var fileInfo = new FileInfo(snapshotPath);
            var categories = ExtractCategories(snapshot);
            var nativeData = ExtractNativeData(snapshot);
            var managedData = ExtractManagedData(snapshot);
            ExtractBreakdownTree(
                snapshot,
                categories.Any(category => category.residentAvailable),
                out var breakdownTree,
                out var breakdownTreeUnavailableReason);

            return new SnapshotAnalysis
            {
                Path = fileInfo.FullName,
                FileSize = fileInfo.Length,
                FileMtimeTicks = fileInfo.LastWriteTimeUtc.Ticks,
                Metadata = ExtractMetadata(snapshot),
                Categories = categories,
                NativeTypeStats = nativeData.TypeStats,
                ManagedTypeStats = managedData.TypeStats,
                TopNativeObjects = nativeData.TopObjects,
                BreakdownTree = breakdownTree,
                BreakdownTreeUnavailableReason = breakdownTreeUnavailableReason,
                NativeObjectCount = nativeData.NativeObjectCount,
                TotalNativeSize = nativeData.TotalNativeSize,
                ManagedObjectCount = managedData.ManagedObjectCount,
                TotalManagedObjectSize = managedData.TotalManagedObjectSize
            };
        }

        private MemorySnapshotMetadataInfo ExtractMetadata(object snapshot)
        {
            var metadata = _snapshotMetaData.GetValue(snapshot);
            var captureDate = (DateTime)_snapshotTimeStamp.GetValue(snapshot, null);

            return new MemorySnapshotMetadataInfo
            {
                unityVersion = GetStringField(metadata, _metadataUnityVersion),
                platform = GetStringField(metadata, _metadataPlatform),
                isEditorCapture = (bool)_metadataIsEditorCapture.GetValue(metadata),
                captureDate = captureDate.ToString("o"),
                captureFlags = _metadataCaptureFlags.GetValue(metadata)?.ToString() ?? ""
            };
        }

        private MemorySnapshotCategoryInfo[] ExtractCategories(object snapshot)
        {
            var hasResidentPages = (bool)_snapshotHasSystemMemoryResidentPages.GetValue(snapshot, null);
            var builder = _summaryBuilderCtor.Invoke(new[] { snapshot, null });
            var model = _summaryBuild.Invoke(builder, null);
            var rows = (IEnumerable)_summaryRows.GetValue(model, null);
            var categories = new List<MemorySnapshotCategoryInfo>();

            foreach (var row in rows)
            {
                var size = _summaryRowBaseSize.GetValue(row, null);
                var residentUnavailable = (bool)_summaryRowResidentSizeUnavailable.GetValue(row, null);
                var residentAvailable = hasResidentPages && !residentUnavailable;

                categories.Add(new MemorySnapshotCategoryInfo
                {
                    name = (string)_summaryRowName.GetValue(row, null),
                    committed = ToLong((ulong)_memorySizeCommitted.GetValue(size)),
                    resident = residentAvailable ? ToLong((ulong)_memorySizeResident.GetValue(size)) : 0,
                    residentAvailable = residentAvailable
                });
            }

            return categories.ToArray();
        }

        private static void ExtractBreakdownTree(
            object snapshot,
            bool residentAvailable,
            out MemorySnapshotBreakdownTreeNode[] breakdownTree,
            out string unavailableReason)
        {
            breakdownTree = null;
            unavailableReason = "";

            if (!AllTrackedMemoryReflection.TryGet(out var reflection, out unavailableReason))
                return;

            try
            {
                breakdownTree = reflection.BuildBreakdownTree(snapshot, residentAvailable);
            }
            catch (Exception ex)
            {
                breakdownTree = null;
                unavailableReason = ex.InnerException?.Message ?? ex.Message;
            }
        }

        private NativeExtractionResult ExtractNativeData(object snapshot)
        {
            var nativeObjects = _snapshotNativeObjects.GetValue(snapshot);
            var nativeTypes = _snapshotNativeTypes.GetValue(snapshot);

            var objectCount = (long)_nativeObjectsCount.GetValue(nativeObjects);
            var objectNames = (string[])_nativeObjectsNames.GetValue(nativeObjects);
            var objectSizes = _nativeObjectsSizes.GetValue(nativeObjects);
            var typeIndices = _nativeObjectsTypeIndices.GetValue(nativeObjects);
            var instanceIds = _nativeObjectsInstanceIds.GetValue(nativeObjects);
            var typeNames = (string[])_nativeTypesNames.GetValue(nativeTypes);

            var typeStats = new MemorySnapshotNativeTypeStat[typeNames.Length];
            for (var i = 0; i < typeNames.Length; i++)
            {
                typeStats[i] = new MemorySnapshotNativeTypeStat
                {
                    typeName = typeNames[i] ?? "",
                    count = 0,
                    totalSize = 0
                };
            }

            var retainer = new NativeObjectRetainer(globalCapacity: 1000, perTypeCapacity: 50);
            long totalNativeSize = 0;

            for (long index = 0; index < objectCount; index++)
            {
                var typeIndex = ReadInt32(typeIndices, index);
                if (typeIndex < 0 || typeIndex >= typeStats.Length)
                    continue;

                var size = ToLong(ReadUInt64(objectSizes, index));
                var typeStat = typeStats[typeIndex];
                typeStat.count++;
                typeStat.totalSize += size;
                totalNativeSize += size;

                retainer.Add(new RetainedNativeObjectInfo
                {
                    NativeObjectIndex = index,
                    Name = objectNames[index] ?? "",
                    TypeName = typeStat.typeName,
                    Size = size,
                    InstanceId = ReadInstanceId(instanceIds, index)
                });
            }

            return new NativeExtractionResult
            {
                TypeStats = typeStats,
                TopObjects = retainer.Build(),
                NativeObjectCount = objectCount,
                TotalNativeSize = totalNativeSize
            };
        }

        private ManagedExtractionResult ExtractManagedData(object snapshot)
        {
            var crawledData = _snapshotCrawledData.GetValue(snapshot, null);
            if (crawledData == null)
            {
                return new ManagedExtractionResult
                {
                    TypeStats = Array.Empty<MemorySnapshotNativeTypeStat>()
                };
            }

            var managedObjects = _managedDataManagedObjects.GetValue(crawledData);
            var accessor = GetDynamicArrayStructFieldAccessor(managedObjects.GetType());
            var objectCount = accessor.GetCount(managedObjects);

            var typeDescriptions = _snapshotTypeDescriptions.GetValue(snapshot);
            var typeNames = (string[])_typeDescriptionsNames.GetValue(typeDescriptions);
            var statsByName = new Dictionary<string, MemorySnapshotNativeTypeStat>(StringComparer.Ordinal);
            long totalManagedSize = 0;

            for (long index = 0; index < objectCount; index++)
            {
                var typeIndex = accessor.ReadInt32(managedObjects, index, _managedObjectTypeDescription);
                var size = accessor.ReadInt32(managedObjects, index, _managedObjectSize);
                if (size < 0)
                    size = 0;

                var typeName = typeIndex >= 0 && typeIndex < typeNames.Length
                    ? typeNames[typeIndex] ?? ""
                    : "<unknown>";

                if (!statsByName.TryGetValue(typeName, out var stat))
                {
                    stat = new MemorySnapshotNativeTypeStat
                    {
                        typeName = typeName,
                        count = 0,
                        totalSize = 0
                    };
                    statsByName[typeName] = stat;
                }

                stat.count++;
                stat.totalSize += size;
                totalManagedSize += size;
            }

            return new ManagedExtractionResult
            {
                TypeStats = statsByName.Values.ToArray(),
                ManagedObjectCount = objectCount,
                TotalManagedObjectSize = totalManagedSize
            };
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

        private static MethodInfo GetRequiredMethod(Type type, string name, Type[] parameterTypes)
        {
            var method = type.GetMethod(name, InstanceMembers | StaticMembers, null, parameterTypes, null);
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

        private static string GetStringField(object target, FieldInfo field)
        {
            return field.GetValue(target) as string ?? "";
        }

        private static long ToLong(ulong value)
        {
            return value > long.MaxValue ? long.MaxValue : (long)value;
        }

        private static uint ToUInt32(ulong value)
        {
            return value > uint.MaxValue ? uint.MaxValue : (uint)value;
        }

        private static int ReadInt32(object dynamicArray, long index)
        {
            var accessor = GetDynamicArrayAccessor(dynamicArray.GetType());
            return Marshal.ReadInt32(accessor.GetElementPointer(dynamicArray, index));
        }

        private static ulong ReadUInt64(object dynamicArray, long index)
        {
            var accessor = GetDynamicArrayAccessor(dynamicArray.GetType());
            return unchecked((ulong)Marshal.ReadInt64(accessor.GetElementPointer(dynamicArray, index)));
        }

        private static string ReadInstanceId(object dynamicArray, long index)
        {
            var accessor = GetDynamicArrayAccessor(dynamicArray.GetType());
            var pointer = accessor.GetElementPointer(dynamicArray, index);

            if (accessor.ElementSize >= 8)
            {
                var value = unchecked((ulong)Marshal.ReadInt64(pointer));
                if (value <= uint.MaxValue)
                    return unchecked((int)ToUInt32(value)).ToString();
                return value.ToString();
            }

            if (accessor.ElementSize >= 4)
                return Marshal.ReadInt32(pointer).ToString();

            return "0";
        }

        private static DynamicArrayAccessor GetDynamicArrayAccessor(Type dynamicArrayType)
        {
            if (DynamicArrayAccessors.TryGetValue(dynamicArrayType, out var accessor))
                return accessor;

            accessor = new DynamicArrayAccessor(dynamicArrayType);
            DynamicArrayAccessors[dynamicArrayType] = accessor;
            return accessor;
        }

        private static DynamicArrayStructFieldAccessor GetDynamicArrayStructFieldAccessor(Type dynamicArrayType)
        {
            if (DynamicArrayStructFieldAccessors.TryGetValue(dynamicArrayType, out var accessor))
                return accessor;

            accessor = new DynamicArrayStructFieldAccessor(dynamicArrayType);
            DynamicArrayStructFieldAccessors[dynamicArrayType] = accessor;
            return accessor;
        }

        private sealed class NativeExtractionResult
        {
            public MemorySnapshotNativeTypeStat[] TypeStats;
            public RetainedNativeObjectInfo[] TopObjects;
            public long NativeObjectCount;
            public long TotalNativeSize;
        }

        private sealed class ManagedExtractionResult
        {
            public MemorySnapshotNativeTypeStat[] TypeStats;
            public long ManagedObjectCount;
            public long TotalManagedObjectSize;
        }

        private sealed class DynamicArrayAccessor
        {
            private readonly FieldInfo _dataField;
            public readonly int ElementSize;

            public DynamicArrayAccessor(Type dynamicArrayType)
            {
                _dataField = GetRequiredField(dynamicArrayType, "m_Data");
                var elementType = dynamicArrayType.GetGenericArguments()[0];
                ElementSize = GetElementSize(elementType);
            }

            public unsafe IntPtr GetElementPointer(object dynamicArray, long index)
            {
                var boxedPointer = _dataField.GetValue(dynamicArray);
                var pointer = new IntPtr(Pointer.Unbox(boxedPointer));
                return new IntPtr(pointer.ToInt64() + checked(index * ElementSize));
            }

            private static int GetElementSize(Type elementType)
            {
                if (elementType == typeof(int))
                    return sizeof(int);
                if (elementType == typeof(long) || elementType == typeof(ulong))
                    return sizeof(long);

                try
                {
                    return Marshal.SizeOf(elementType);
                }
                catch (ArgumentException)
                {
                    foreach (var field in elementType.GetFields(InstanceMembers))
                    {
                        if (field.FieldType == typeof(long) || field.FieldType == typeof(ulong))
                            return sizeof(long);
                        if (field.FieldType == typeof(int) || field.FieldType == typeof(uint))
                            return sizeof(int);
                    }

                    throw;
                }
            }
        }

        private sealed class DynamicArrayStructFieldAccessor
        {
            private readonly FieldInfo _dataField;
            private readonly PropertyInfo _countProperty;
            private readonly int _elementSize;

            public DynamicArrayStructFieldAccessor(Type dynamicArrayType)
            {
                _dataField = GetRequiredField(dynamicArrayType, "m_Data");
                _countProperty = GetRequiredProperty(dynamicArrayType, "Count");
                _elementSize = GetElementSize(dynamicArrayType.GetGenericArguments()[0]);
            }

            public long GetCount(object dynamicArray)
            {
                return (long)_countProperty.GetValue(dynamicArray, null);
            }

            public int ReadInt32(object dynamicArray, long index, FieldInfo elementField)
            {
                var offset = (int)Marshal.OffsetOf(elementField.DeclaringType, elementField.Name);
                return Marshal.ReadInt32(IntPtr.Add(GetElementPointer(dynamicArray, index), offset));
            }

            private unsafe IntPtr GetElementPointer(object dynamicArray, long index)
            {
                var boxedPointer = _dataField.GetValue(dynamicArray);
                var pointer = new IntPtr(Pointer.Unbox(boxedPointer));
                return new IntPtr(pointer.ToInt64() + checked(index * _elementSize));
            }

            private static int GetElementSize(Type elementType)
            {
                var unsafeUtility = Type.GetType("Unity.Collections.LowLevel.Unsafe.UnsafeUtility, UnityEngine.CoreModule");
                var sizeOf = unsafeUtility?
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(method =>
                        method.Name == "SizeOf" &&
                        method.IsGenericMethodDefinition &&
                        method.GetParameters().Length == 0);

                if (sizeOf != null)
                    return (int)sizeOf.MakeGenericMethod(elementType).Invoke(null, null);

                return Marshal.SizeOf(elementType);
            }
        }
    }
}
#endif
