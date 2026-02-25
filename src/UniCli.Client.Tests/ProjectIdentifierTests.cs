using UniCli.Client;

namespace UniCli.Client.Tests;

public class NormalizePathForHashTests
{
    [Fact]
    public void BackslashesAreReplacedWithForwardSlashes()
    {
        var result = ProjectIdentifier.NormalizePathForHash(@"C:\Users\dev\MyProject\Assets");

        Assert.Equal("C:/Users/dev/MyProject/Assets", result);
    }

    [Fact]
    public void ForwardSlashesArePreserved()
    {
        var result = ProjectIdentifier.NormalizePathForHash("C:/Users/dev/MyProject/Assets");

        Assert.Equal("C:/Users/dev/MyProject/Assets", result);
    }

    [Fact]
    public void MixedSeparatorsAreNormalized()
    {
        var result = ProjectIdentifier.NormalizePathForHash(@"C:\Users/dev\MyProject/Assets");

        Assert.Equal("C:/Users/dev/MyProject/Assets", result);
    }
}

public class GetProjectHashTests
{
    [Fact]
    public void SamePathWithDifferentSeparators_ProducesSameHash()
    {
        var hashForward = ProjectIdentifier.GetProjectHash(
            ProjectIdentifier.NormalizePathForHash("C:/Users/dev/MyProject/Assets"));
        var hashBackslash = ProjectIdentifier.GetProjectHash(
            ProjectIdentifier.NormalizePathForHash(@"C:\Users\dev\MyProject\Assets"));

        Assert.Equal(hashForward, hashBackslash);
    }

    [Fact]
    public void DifferentPaths_ProduceDifferentHashes()
    {
        var hash1 = ProjectIdentifier.GetProjectHash(
            ProjectIdentifier.NormalizePathForHash("C:/Users/dev/ProjectA/Assets"));
        var hash2 = ProjectIdentifier.GetProjectHash(
            ProjectIdentifier.NormalizePathForHash("C:/Users/dev/ProjectB/Assets"));

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashIsEightCharacters()
    {
        var hash = ProjectIdentifier.GetProjectHash(
            ProjectIdentifier.NormalizePathForHash("/Users/dev/MyProject/Assets"));

        Assert.Equal(8, hash.Length);
    }

    [Fact]
    public void HashIsLowercaseHex()
    {
        var hash = ProjectIdentifier.GetProjectHash(
            ProjectIdentifier.NormalizePathForHash("/Users/dev/MyProject/Assets"));

        Assert.Matches("^[0-9a-f]{8}$", hash);
    }
}
