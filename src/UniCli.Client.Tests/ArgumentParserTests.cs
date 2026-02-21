using System.Buffers;
using System.Text;
using System.Text.Json;
using UniCli.Client;
using UniCli.Protocol;

namespace UniCli.Client.Tests;

public class ParseKeyValueArgsTests
{
    [Fact]
    public void EmptyArgs_ReturnsEmptyDictionary()
    {
        var result = ArgumentParser.ParseKeyValueArgs([]);
        Assert.Empty(result);
    }

    [Fact]
    public void SingleKeyValue_ParsesCorrectly()
    {
        var result = ArgumentParser.ParseKeyValueArgs(["--name", "hello"]);
        Assert.Single(result);
        Assert.Equal(["hello"], result["name"]);
    }

    [Fact]
    public void MultipleKeyValues_ParsesAll()
    {
        var result = ArgumentParser.ParseKeyValueArgs(["--name", "foo", "--count", "3"]);
        Assert.Equal(2, result.Count);
        Assert.Equal(["foo"], result["name"]);
        Assert.Equal(["3"], result["count"]);
    }

    [Fact]
    public void BooleanFlag_WithoutValue_AddsNull()
    {
        var result = ArgumentParser.ParseKeyValueArgs(["--verbose"]);
        Assert.Single(result);
        Assert.Equal([null], result["verbose"]);
    }

    [Fact]
    public void BooleanFlag_FollowedByAnotherFlag_AddsNull()
    {
        var result = ArgumentParser.ParseKeyValueArgs(["--verbose", "--name", "test"]);
        Assert.Equal(2, result.Count);
        Assert.Equal([null], result["verbose"]);
        Assert.Equal(["test"], result["name"]);
    }

    [Fact]
    public void RepeatedKey_AccumulatesValues()
    {
        var result = ArgumentParser.ParseKeyValueArgs(["--tag", "a", "--tag", "b", "--tag", "c"]);
        Assert.Single(result);
        Assert.Equal(["a", "b", "c"], result["tag"]);
    }

    [Fact]
    public void CaseInsensitive_Keys()
    {
        var result = ArgumentParser.ParseKeyValueArgs(["--Name", "foo", "--NAME", "bar"]);
        Assert.Single(result);
        Assert.Equal(["foo", "bar"], result["name"]);
    }

    [Fact]
    public void NonFlagArgs_AreIgnored()
    {
        var result = ArgumentParser.ParseKeyValueArgs(["positional", "--key", "val"]);
        Assert.Single(result);
        Assert.Equal(["val"], result["key"]);
    }
}

public class BuildJsonFromKeyValuesTests
{
    private static string Build(Dictionary<string, List<string?>> pairs, CommandFieldInfo[] fields)
        => ArgumentParser.BuildJsonFromKeyValues(pairs, fields);

    private static Dictionary<string, List<string?>> Pairs(params (string key, string? value)[] items)
    {
        var dict = new Dictionary<string, List<string?>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in items)
        {
            if (!dict.TryGetValue(key, out var list))
            {
                list = [];
                dict[key] = list;
            }
            list.Add(value);
        }
        return dict;
    }

    [Fact]
    public void EmptyPairs_ReturnsEmptyObject()
    {
        var json = Build(Pairs(), []);
        Assert.Equal("{}", json);
    }

    [Fact]
    public void StringField_WritesString()
    {
        var fields = new[] { new CommandFieldInfo { name = "name", type = "string" } };
        var json = Build(Pairs(("name", "hello")), fields);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("hello", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void IntField_WritesNumber()
    {
        var fields = new[] { new CommandFieldInfo { name = "count", type = "int" } };
        var json = Build(Pairs(("count", "42")), fields);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(42, doc.RootElement.GetProperty("count").GetInt32());
    }

    [Fact]
    public void FloatField_WritesNumber()
    {
        var fields = new[] { new CommandFieldInfo { name = "rate", type = "float" } };
        var json = Build(Pairs(("rate", "1.5")), fields);
        using var doc = JsonDocument.Parse(json);
        Assert.True(Math.Abs(1.5 - doc.RootElement.GetProperty("rate").GetDouble()) < 0.001);
    }

    [Fact]
    public void BoolField_WritesBool()
    {
        var fields = new[] { new CommandFieldInfo { name = "active", type = "bool" } };
        var json = Build(Pairs(("active", "true")), fields);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("active").GetBoolean());
    }

    [Fact]
    public void NullValue_WritesTrue()
    {
        var fields = new[] { new CommandFieldInfo { name = "verbose", type = "bool" } };
        var json = Build(Pairs(("verbose", null)), fields);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("verbose").GetBoolean());
    }

    [Fact]
    public void ArrayField_WritesArray()
    {
        var fields = new[] { new CommandFieldInfo { name = "tags", type = "string[]" } };
        var pairs = Pairs(("tags", "a"), ("tags", "b"));
        var json = Build(pairs, fields);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement.GetProperty("tags");
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
        Assert.Equal(2, arr.GetArrayLength());
        Assert.Equal("a", arr[0].GetString());
        Assert.Equal("b", arr[1].GetString());
    }

    [Fact]
    public void IntArrayField_WritesNumberArray()
    {
        var fields = new[] { new CommandFieldInfo { name = "ids", type = "int[]" } };
        var pairs = Pairs(("ids", "1"), ("ids", "2"), ("ids", "3"));
        var json = Build(pairs, fields);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement.GetProperty("ids");
        Assert.Equal(3, arr.GetArrayLength());
        Assert.Equal(1, arr[0].GetInt32());
        Assert.Equal(2, arr[1].GetInt32());
        Assert.Equal(3, arr[2].GetInt32());
    }

    [Fact]
    public void UnknownField_DefaultsToString()
    {
        var json = Build(Pairs(("unknown", "value")), []);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("value", doc.RootElement.GetProperty("unknown").GetString());
    }

    [Fact]
    public void FieldNameCase_UsesSchemaCase()
    {
        var fields = new[] { new CommandFieldInfo { name = "MyField", type = "string" } };
        var json = Build(Pairs(("myfield", "val")), fields);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("val", doc.RootElement.GetProperty("MyField").GetString());
    }

    [Fact]
    public void RepeatedScalar_UsesLastValue()
    {
        var fields = new[] { new CommandFieldInfo { name = "name", type = "string" } };
        var pairs = Pairs(("name", "first"), ("name", "second"));
        var json = Build(pairs, fields);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("second", doc.RootElement.GetProperty("name").GetString());
    }
}

public class WriteScalarValueTests
{
    private static string WriteScalar(string value, string fieldType)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WritePropertyName("v");
        ArgumentParser.WriteScalarValue(writer, value, fieldType);
        writer.WriteEndObject();
        writer.Flush();
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("v").GetRawText();
    }

    [Fact]
    public void Int_WritesNumber() => Assert.Equal("42", WriteScalar("42", "int"));

    [Fact]
    public void Float_WritesNumber()
    {
        var result = WriteScalar("1.5", "float");
        Assert.True(double.TryParse(result, out var val));
        Assert.True(Math.Abs(1.5 - val) < 0.001);
    }

    [Fact]
    public void Double_WritesNumber()
    {
        var result = WriteScalar("3.14", "double");
        Assert.True(double.TryParse(result, out var val));
        Assert.True(Math.Abs(3.14 - val) < 0.001);
    }

    [Fact]
    public void Bool_WritesBool() => Assert.Equal("true", WriteScalar("true", "bool"));

    [Fact]
    public void BoolFalse_WritesBool() => Assert.Equal("false", WriteScalar("false", "bool"));

    [Fact]
    public void InvalidInt_FallsBackToString() => Assert.Equal("\"abc\"", WriteScalar("abc", "int"));

    [Fact]
    public void UnknownType_WritesString() => Assert.Equal("\"hello\"", WriteScalar("hello", "custom"));
}

public class IsRetryableErrorTests
{
    [Fact]
    public void ServerClosedConnection_IsRetryable()
        => Assert.True(ArgumentParser.IsRetryableError("Server closed connection unexpectedly"));

    [Fact]
    public void CommunicationError_IsRetryable()
        => Assert.True(ArgumentParser.IsRetryableError("Communication error: broken pipe"));

    [Fact]
    public void UnknownCommand_IsNotRetryable()
        => Assert.False(ArgumentParser.IsRetryableError("Unknown command: Foo"));

    [Fact]
    public void EmptyString_IsNotRetryable()
        => Assert.False(ArgumentParser.IsRetryableError(""));
}

public class ArrayTypeHelperTests
{
    [Fact]
    public void StringArray_IsArrayType() => Assert.True(ArgumentParser.IsArrayType("string[]"));

    [Fact]
    public void IntArray_IsArrayType() => Assert.True(ArgumentParser.IsArrayType("int[]"));

    [Fact]
    public void PlainString_IsNotArrayType() => Assert.False(ArgumentParser.IsArrayType("string"));

    [Fact]
    public void GetElementType_StringArray() => Assert.Equal("string", ArgumentParser.GetArrayElementType("string[]"));

    [Fact]
    public void GetElementType_IntArray() => Assert.Equal("int", ArgumentParser.GetArrayElementType("int[]"));
}
