using System;
using UniCli.Remote;
using UnityEngine;

[DebugCommand("Debug.SetTimeScale", "Change game speed (0=pause, 1=normal, 2=fast)")]
public class SetTimeScaleCommand : DebugCommand<SetTimeScaleCommand.Request, SetTimeScaleCommand.Response>
{
    protected override Response ExecuteCommand(Request request)
    {
        Time.timeScale = request.scale;
        return new Response
        {
            scale = Time.timeScale
        };
    }

    [Serializable]
    public class Request
    {
        public float scale = 1f;
    }

    [Serializable]
    public class Response
    {
        public float scale;
    }
}
