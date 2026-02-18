using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SetGameObjectsSelectionHandler : CommandHandler<SetGameObjectsSelectionRequest, SetGameObjectsSelectionResponse>
    {
        public override string CommandName => CommandNames.Selection.SetGameObjects;
        public override string Description => "Select multiple GameObjects by paths";

        protected override bool TryWriteFormatted(SetGameObjectsSelectionResponse response, bool success, IFormatWriter writer)
        {
            if (!success) return true;

            writer.WriteLine($"Selected {response.selected.Length} GameObjects:");
            foreach (var item in response.selected)
                writer.WriteLine($"  {item.path} (instanceId={item.instanceId})");

            if (response.notFound != null && response.notFound.Length > 0)
            {
                writer.WriteLine($"Not found ({response.notFound.Length}):");
                foreach (var path in response.notFound)
                    writer.WriteLine($"  {path}");
            }

            return true;
        }

        protected override ValueTask<SetGameObjectsSelectionResponse> ExecuteAsync(SetGameObjectsSelectionRequest request, CancellationToken cancellationToken)
        {
            if (request.paths == null || request.paths.Length == 0)
            {
                throw new CommandFailedException(
                    "paths is required",
                    new SetGameObjectsSelectionResponse
                    {
                        selected = Array.Empty<SelectedGameObjectInfo>(),
                        notFound = Array.Empty<string>()
                    });
            }

            var objects = new List<GameObject>();
            var selected = new List<SelectedGameObjectInfo>();
            var notFound = new List<string>();

            foreach (var path in request.paths)
            {
                var go = GameObjectResolver.Resolve(0, path);
                if (go != null)
                {
                    objects.Add(go);
                    selected.Add(new SelectedGameObjectInfo
                    {
                        instanceId = go.GetInstanceID(),
                        name = go.name,
                        path = GameObjectResolver.BuildPath(go.transform)
                    });
                }
                else
                {
                    notFound.Add(path);
                }
            }

            Selection.objects = objects.ToArray();

            return new ValueTask<SetGameObjectsSelectionResponse>(new SetGameObjectsSelectionResponse
            {
                selected = selected.ToArray(),
                notFound = notFound.ToArray()
            });
        }
    }

    [Serializable]
    public class SetGameObjectsSelectionRequest
    {
        public string[] paths;
    }

    [Serializable]
    public class SetGameObjectsSelectionResponse
    {
        public SelectedGameObjectInfo[] selected;
        public string[] notFound;
    }
}
