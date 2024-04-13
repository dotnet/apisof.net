namespace Terrajobst.ApiCatalog.Tests;

public class SuffixTreeTest
{
    [Fact]
    public void Empty_Stats_Zero()
    {
        var stats = SuffixTree.Empty.GetStats();

        Assert.Equal(0, stats.Strings);
        Assert.Equal(0, stats.Nodes);
        Assert.Equal(0, stats.Nodes_NoChildren_NoValues);
        Assert.Equal(0, stats.Nodes_NoChildren_SingleValue);
        Assert.Equal(0, stats.Nodes_NoChildren_MultipleValues);
        Assert.Equal(0, stats.Nodes_SingleChild_NoValues);
        Assert.Equal(0, stats.Nodes_SingleChild_SingleValue);
        Assert.Equal(0, stats.Nodes_SingleChild_MultipleValues);
        Assert.Equal(0, stats.Nodes_MultipleChildren_NoValues);
        Assert.Equal(0, stats.Nodes_MultipleChildren_SingleValue);
        Assert.Equal(0, stats.Nodes_MultipleChildren_MultipleValues);
    }

    [Fact]
    public void Empty_Lookup_ReturnsEmpty()
    {
        var suffixTree = SuffixTree.Empty;

        var result = suffixTree.Lookup("test");
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Empty_WriteDot_ReturnsEmpty()
    {
        var suffixTree = SuffixTree.Empty;

        var stringWriter = new StringWriter();
        suffixTree.WriteDot(stringWriter);

        var expectedText = """
                           digraph {
                           }
                           """;

        var actualText = stringWriter.ToString().Trim();

        Assert.Equal(expectedText, actualText);
    }
}