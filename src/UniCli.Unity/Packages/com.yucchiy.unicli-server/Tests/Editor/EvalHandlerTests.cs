using NUnit.Framework;
using UniCli.Server.Editor.Handlers;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class EvalHandlerTests
    {
        [TestCase("MonoBleedingEdge/lib/mono/unityjit-macos/Facades/System.Threading.dll")]
        [TestCase(@"MonoBleedingEdge\lib\mono\unityjit-macos\Facades\System.Threading.dll")]
        [TestCase("NetStandard/ref/2.1.0/System.Runtime.dll")]
        [TestCase(@"NetStandard\compat\2.1.0\shims\netstandard\System.Threading.dll")]
        [TestCase("UnityReferenceAssemblies/SomeAssembly.dll")]
        public void IsImplicitUnityReferenceRelativePath_ImplicitReference_ReturnsTrue(string relativePath)
        {
            Assert.That(EvalHandler.IsImplicitUnityReferenceRelativePath(relativePath), Is.True);
        }

        [TestCase("Managed/UnityEngine.CoreModule.dll")]
        [TestCase("Library/ScriptAssemblies/MyProject.dll")]
        public void IsImplicitUnityReferenceRelativePath_NonImplicitReference_ReturnsFalse(string relativePath)
        {
            Assert.That(EvalHandler.IsImplicitUnityReferenceRelativePath(relativePath), Is.False);
        }

        [Test]
        public void IsImplicitUnityReference_Unity6CompatShimPath_ReturnsTrue()
        {
            const string unityContentsPath = "/Applications/Unity/Hub/Editor/6000.2.13f1/Unity.app/Contents";
            const string assemblyPath = "/Applications/Unity/Hub/Editor/6000.2.13f1/Unity.app/Contents/NetStandard/compat/2.1.0/shims/netstandard/System.Threading.dll";

            Assert.That(EvalHandler.IsImplicitUnityReference(assemblyPath, unityContentsPath), Is.True);
        }

        [Test]
        public void IsImplicitUnityReference_PathOutsideUnityContents_ReturnsFalse()
        {
            const string unityContentsPath = "/Applications/Unity/Hub/Editor/6000.2.13f1/Unity.app/Contents";
            const string assemblyPath = "/tmp/System.Threading.dll";

            Assert.That(EvalHandler.IsImplicitUnityReference(assemblyPath, unityContentsPath), Is.False);
        }
    }
}
