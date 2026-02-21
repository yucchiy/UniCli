using UnityEngine;
using UnityEngine.Scripting;

namespace UniCli.Remote
{
    [Preserve]
    [RequireDerived]
    public abstract class DebugCommand
    {
        public abstract string Execute(string requestJson);
    }

    [Preserve]
    [RequireDerived]
    public abstract class DebugCommand<TRequest, TResponse> : DebugCommand
    {
        public override string Execute(string requestJson)
        {
            TRequest request;
            if (typeof(TRequest) == typeof(RuntimeUnit))
            {
                request = (TRequest)(object)RuntimeUnit.Value;
            }
            else if (string.IsNullOrEmpty(requestJson))
            {
                request = JsonUtility.FromJson<TRequest>("{}");
            }
            else
            {
                request = JsonUtility.FromJson<TRequest>(requestJson);
            }

            var response = ExecuteCommand(request);

            if (response is RuntimeUnit)
                return "";

            return JsonUtility.ToJson(response);
        }

        protected abstract TResponse ExecuteCommand(TRequest request);
    }
}
