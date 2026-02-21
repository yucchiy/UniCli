using System;
using UniCli.Remote;
using UnityEngine;

[DebugCommand("Debug.ToggleObject", "Toggle a GameObject's visibility by name")]
public class ToggleObjectCommand : DebugCommand<ToggleObjectCommand.Request, ToggleObjectCommand.Response>
{
    protected override Response ExecuteCommand(Request request)
    {
        var go = GameObject.Find(request.name);
        if (go == null)
            throw new InvalidOperationException($"GameObject '{request.name}' not found");

        go.SetActive(!go.activeSelf);
        return new Response
        {
            name = go.name,
            isActive = go.activeSelf
        };
    }

    [Serializable]
    public class Request
    {
        public string name;
    }

    [Serializable]
    public class Response
    {
        public string name;
        public bool isActive;
    }
}
