namespace UniCli.Server.Editor
{
    public static class CommandNames
    {
        public const string Compile = "Compile";
        public const string Eval = "Eval";
        public const string Search = "Search";

        public static class BuildPlayer
        {
            public const string Build = "BuildPlayer.Build";
            public const string Compile = "BuildPlayer.Compile";
        }

        public static class Console
        {
            public const string GetLog = "Console.GetLog";
            public const string Clear = "Console.Clear";
        }

        public static class PlayMode
        {
            public const string Enter = "PlayMode.Enter";
            public const string Exit = "PlayMode.Exit";
            public const string Pause = "PlayMode.Pause";
            public const string Status = "PlayMode.Status";
        }

        public static class Menu
        {
            public const string List = "Menu.List";
            public const string Execute = "Menu.Execute";
        }

        public static class TestRunner
        {
            public const string RunEditMode = "TestRunner.RunEditMode";
            public const string RunPlayMode = "TestRunner.RunPlayMode";
        }

        public static class GameObject
        {
            public const string Find = "GameObject.Find";
            public const string Create = "GameObject.Create";
            public const string GetComponents = "GameObject.GetComponents";
            public const string SetActive = "GameObject.SetActive";
            public const string GetHierarchy = "GameObject.GetHierarchy";
            public const string AddComponent = "GameObject.AddComponent";
            public const string RemoveComponent = "GameObject.RemoveComponent";
            public const string Destroy = "GameObject.Destroy";
            public const string SetTransform = "GameObject.SetTransform";
            public const string Duplicate = "GameObject.Duplicate";
            public const string Rename = "GameObject.Rename";
            public const string SetParent = "GameObject.SetParent";
            public const string CreatePrimitive = "GameObject.CreatePrimitive";
        }

        public static class Component
        {
            public const string SetProperty = "Component.SetProperty";
        }

        public static class AssetDatabase
        {
            public const string Find = "AssetDatabase.Find";
            public const string Import = "AssetDatabase.Import";
            public const string GetPath = "AssetDatabase.GetPath";
            public const string Delete = "AssetDatabase.Delete";
        }

        public static class Project
        {
            public const string Inspect = "Project.Inspect";
        }

        public static class PackageManager
        {
            public const string List = "PackageManager.List";
            public const string Add = "PackageManager.Add";
            public const string Remove = "PackageManager.Remove";
            public const string Search = "PackageManager.Search";
            public const string GetInfo = "PackageManager.GetInfo";
            public const string Update = "PackageManager.Update";
        }

        public static class Prefab
        {
            public const string GetStatus = "Prefab.GetStatus";
            public const string Instantiate = "Prefab.Instantiate";
            public const string Save = "Prefab.Save";
            public const string Apply = "Prefab.Apply";
            public const string Unpack = "Prefab.Unpack";
        }

        public static class AssemblyDefinition
        {
            public const string List = "AssemblyDefinition.List";
            public const string Get = "AssemblyDefinition.Get";
            public const string Create = "AssemblyDefinition.Create";
            public const string AddReference = "AssemblyDefinition.AddReference";
            public const string RemoveReference = "AssemblyDefinition.RemoveReference";
        }

        public static class Scene
        {
            public const string List = "Scene.List";
            public const string GetActive = "Scene.GetActive";
            public const string SetActive = "Scene.SetActive";
            public const string Open = "Scene.Open";
            public const string Close = "Scene.Close";
            public const string Save = "Scene.Save";
            public const string New = "Scene.New";
        }

        public static class Connection
        {
            public const string List = "Connection.List";
            public const string Connect = "Connection.Connect";
            public const string Status = "Connection.Status";
        }

        public static class Material
        {
            public const string Create = "Material.Create";
        }

        public static class AnimatorController
        {
            public const string Create = "AnimatorController.Create";
            public const string Inspect = "AnimatorController.Inspect";
            public const string AddParameter = "AnimatorController.AddParameter";
            public const string RemoveParameter = "AnimatorController.RemoveParameter";
            public const string AddState = "AnimatorController.AddState";
            public const string AddTransition = "AnimatorController.AddTransition";
            public const string AddTransitionCondition = "AnimatorController.AddTransitionCondition";
        }

        public static class Animator
        {
            public const string Inspect = "Animator.Inspect";
            public const string SetController = "Animator.SetController";
            public const string SetParameter = "Animator.SetParameter";
            public const string Play = "Animator.Play";
            public const string CrossFade = "Animator.CrossFade";
        }

        public static class Profiler
        {
            public const string Inspect = "Profiler.Inspect";
            public const string StartRecording = "Profiler.StartRecording";
            public const string StopRecording = "Profiler.StopRecording";
            public const string SaveProfile = "Profiler.SaveProfile";
            public const string LoadProfile = "Profiler.LoadProfile";
            public const string GetFrameData = "Profiler.GetFrameData";
            public const string TakeSnapshot = "Profiler.TakeSnapshot";
            public const string AnalyzeFrames = "Profiler.AnalyzeFrames";
            public const string FindSpikes = "Profiler.FindSpikes";
        }

        public static class Selection
        {
            public const string Get = "Selection.Get";
            public const string SetGameObject = "Selection.SetGameObject";
            public const string SetGameObjects = "Selection.SetGameObjects";
            public const string SetAsset = "Selection.SetAsset";
            public const string SetAssets = "Selection.SetAssets";
        }

        public static class Window
        {
            public const string Open = "Window.Open";
            public const string Create = "Window.Create";
            public const string List = "Window.List";
            public const string Focus = "Window.Focus";
        }
    }
}
