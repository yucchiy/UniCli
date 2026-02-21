using UnityEngine.Scripting;

namespace UniCli.Remote
{
    [Preserve]
    public static class RuntimeMessageGuids
    {
        public static readonly System.Guid CommandRequest =
            new System.Guid("a1b2c3d4-e5f6-7890-abcd-ef0123456789");

        public static readonly System.Guid CommandResponse =
            new System.Guid("a1b2c3d4-e5f6-7890-abcd-ef0123456790");

        public static readonly System.Guid ListRequest =
            new System.Guid("a1b2c3d4-e5f6-7890-abcd-ef0123456791");

        public static readonly System.Guid ListResponse =
            new System.Guid("a1b2c3d4-e5f6-7890-abcd-ef0123456792");
    }
}
