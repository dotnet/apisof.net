namespace Terrajobst.ApiCatalog.Tests;

public class TokenizerTests
{
    [Fact]
    public void Tokenizer_Splits_OnCaseChanges()
    {
        var input = "System.TheThing";
        var expected = new string[] {
            "system",
            ".",
            "the",
            "thing",
        };
        
        var actual = Tokenizer.Tokenize(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Tokenizer_Strips_Generics()
    {
        var input = "System.Outer<T>.Inner.M()";
        var expected = new string[] {
            "system",
            ".",
            "outer",
            ".",
            "inner",
            ".",
            "m"
        };
        
        var actual = Tokenizer.Tokenize(input);
        Assert.Equal(expected, actual);
    }
}
