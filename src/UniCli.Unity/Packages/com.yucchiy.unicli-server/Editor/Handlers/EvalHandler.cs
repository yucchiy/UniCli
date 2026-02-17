using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class EvalHandler : CommandHandler<EvalRequest, EvalResponse>
    {
        public override string CommandName => CommandNames.Eval;
        public override string Description => "Compile and execute C# code dynamically in the Unity Editor context";

        private static int _evalCounter;

        protected override bool TryWriteFormatted(EvalResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                writer.WriteLine($"[{response.ResultType}]");
                writer.WriteLine(response.ResultRaw);
            }
            else
            {
                writer.WriteLine(response.ResultRaw);
            }

            return true;
        }

        protected override async ValueTask<EvalResponse> ExecuteAsync(EvalRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.code))
                throw new ArgumentException("code must not be empty");

            var id = ++_evalCounter;
            var className = $"UniCliEval_{id}";
            var tempDir = Path.Combine("Temp", "UniCliEval");
            var sourcePath = Path.Combine(tempDir, $"{className}.cs");
            var dllPath = Path.Combine(tempDir, $"{className}.dll");

            Directory.CreateDirectory(tempDir);

            try
            {
                var source = WrapUserCode(className, request.code, request.declarations);
                await File.WriteAllTextAsync(sourcePath, source);

                await CompileAsync(sourcePath, dllPath);

                var assembly = System.Reflection.Assembly.Load(await File.ReadAllBytesAsync(dllPath));
                var type = assembly.GetType(className);
                var method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
                object result;

                try
                {
                    result = method.Invoke(null, null);
                }
                catch (TargetInvocationException ex)
                {
                    var inner = ex.InnerException ?? ex;
                    throw new CommandFailedException(
                        $"Runtime error: {inner.Message}",
                        EvalResponse.FromError(
                            $"{inner.Message}\n{inner.StackTrace}",
                            inner.GetType().FullName));
                }

                return EvalResponse.FromResult(result);
            }
            finally
            {
                if (File.Exists(sourcePath)) File.Delete(sourcePath);
                if (File.Exists(dllPath)) File.Delete(dllPath);
            }
        }

        private static string WrapUserCode(string className, string userCode, string declarations)
        {
            return
$@"using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

{declarations ?? ""}

public static class {className}
{{
    public static object Execute()
    {{
        {userCode}
        return null;
    }}
}}";
        }

        private static string[] GetAdditionalReferences()
        {
            var refs = new System.Collections.Generic.List<string>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                try
                {
                    var loc = asm.Location;
                    if (!string.IsNullOrEmpty(loc))
                        refs.Add(loc);
                }
                catch
                {
                    // some assemblies may throw on Location access
                }
            }
            return refs.ToArray();
        }

        private static async Task CompileAsync(string sourcePath, string dllPath)
        {
            var tcs = new TaskCompletionSource<CompilerMessage[]>();

            var builder = new AssemblyBuilder(dllPath, sourcePath)
            {
                referencesOptions = ReferencesOptions.UseEngineModules,
                additionalReferences = GetAdditionalReferences()
            };

            builder.buildFinished += (path, messages) => { tcs.SetResult(messages); };

            if (!builder.Build())
                throw new CommandFailedException("AssemblyBuilder.Build() failed to start",
                    EvalResponse.FromError("Failed to start compilation", "CompileError"));

            var messages = await tcs.Task;

            var errors = new System.Collections.Generic.List<string>();
            foreach (var msg in messages)
            {
                if (msg.type == CompilerMessageType.Error)
                    errors.Add(msg.message);
            }

            if (errors.Count > 0)
            {
                var errorText = string.Join("\n", errors);
                throw new CommandFailedException(
                    $"Compilation failed with {errors.Count} error(s)",
                    EvalResponse.FromError(errorText, "CompileError"));
            }
        }
    }

    [Serializable]
    public class EvalRequest
    {
        public string code;
        public string declarations;
    }

    public class EvalResponse : IRawJsonResponse
    {
        public string ResultJson { get; private set; }
        public string ResultType { get; private set; }
        public string ResultRaw { get; private set; }

        public static EvalResponse FromResult(object result)
        {
            if (result == null)
                return new EvalResponse { ResultJson = "null", ResultType = "null", ResultRaw = "null" };

            var typeName = result.GetType().FullName;
            var (json, raw) = SerializeToJson(result);
            return new EvalResponse { ResultJson = json, ResultType = typeName, ResultRaw = raw };
        }

        public static EvalResponse FromError(string message, string errorType)
        {
            return new EvalResponse
            {
                ResultJson = EscapeJsonString(message),
                ResultType = errorType,
                ResultRaw = message
            };
        }

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\"result\":");
            sb.Append(ResultJson);
            sb.Append(",\"resultType\":");
            sb.Append(EscapeJsonString(ResultType));
            sb.Append('}');
            return sb.ToString();
        }

        private static (string json, string raw) SerializeToJson(object result)
        {
            if (result is string s)
                return (EscapeJsonString(s), s);

            if (result is bool b)
            {
                var val = b ? "true" : "false";
                return (val, val);
            }

            if (result is int or long or short or byte or sbyte or uint or ulong or ushort)
            {
                var val = result.ToString();
                return (val, val);
            }

            if (result is float f)
            {
                var val = f.ToString("G9");
                return (val, val);
            }

            if (result is double d)
            {
                var val = d.ToString("G17");
                return (val, val);
            }

            if (result is UnityEngine.Object unityObj)
            {
                var json = EditorJsonUtility.ToJson(unityObj, true);
                return (json, json);
            }

            try
            {
                var json = ReflectionSerialize(result);
                return (json, json);
            }
            catch
            {
                // fall through to ToString
            }

            var str = result.ToString();
            return (EscapeJsonString(str), str);
        }

        private static string ReflectionSerialize(object obj)
        {
            if (obj == null) return "null";

            var type = obj.GetType();

            if (type == typeof(string))
                return EscapeJsonString((string)obj);

            if (type == typeof(bool))
                return (bool)obj ? "true" : "false";

            if (type.IsPrimitive)
            {
                if (type == typeof(float))
                    return ((float)obj).ToString("G9");
                if (type == typeof(double))
                    return ((double)obj).ToString("G17");
                return obj.ToString();
            }

            if (type.IsEnum)
                return EscapeJsonString(obj.ToString());

            if (type.IsArray)
            {
                var array = (Array)obj;
                var sb = new StringBuilder();
                sb.Append('[');
                for (int i = 0; i < array.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(ReflectionSerialize(array.GetValue(i)));
                }
                sb.Append(']');
                return sb.ToString();
            }

            if (obj is System.Collections.IList list)
            {
                var sb = new StringBuilder();
                sb.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(ReflectionSerialize(list[i]));
                }
                sb.Append(']');
                return sb.ToString();
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            if (fields.Length > 0)
            {
                var sb = new StringBuilder();
                sb.Append('{');
                var first = true;
                foreach (var field in fields)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append('"');
                    sb.Append(field.Name);
                    sb.Append("\":");
                    sb.Append(ReflectionSerialize(field.GetValue(obj)));
                }
                sb.Append('}');
                return sb.ToString();
            }

            return EscapeJsonString(obj.ToString());
        }

        private static string EscapeJsonString(string s)
        {
            if (s == null) return "null";

            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
