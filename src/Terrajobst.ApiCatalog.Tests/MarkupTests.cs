namespace Terrajobst.ApiCatalog.Tests;

public class MarkupTests
{
    [Theory]
    [MemberData(nameof(GetTokenKinds))]
    public void Token_FromText_AgreesWith_GetTokenText(MarkupTokenKind markupToken)
    {
        var tokenText = markupToken.GetTokenText();
        if (tokenText is null)
            return;

        var tokenKind = MarkupFacts.GetTokenKind(tokenText);
        Assert.Equal(markupToken, tokenKind);
    }

    [Theory]
    [MemberData(nameof(GetKeywordKinds))]
    public void Token_MembersEndingWithKeyword_ReturnTrueForIsKeyword(MarkupTokenKind keyword)
    {
        Assert.True(keyword.IsKeyword());
    }

    [Theory]
    [MemberData(nameof(GetKeywordKinds))]
    public void Token_MembersEndingWithKeyword_HaveTokenText(MarkupTokenKind keyword)
    {
        Assert.False(string.IsNullOrEmpty(keyword.GetTokenText()));
    }

    [Theory]
    [MemberData(nameof(GetPunctuationKinds))]
    public void Token_MembersEndingWithToken_ReturnTrueForIsPunctuation(MarkupTokenKind punctuation)
    {
        Assert.True(punctuation.IsPunctuation());
    }

    [Theory]
    [MemberData(nameof(GetPunctuationKinds))]
    public void Token_MembersEndingWithToken_HaveTokenText(MarkupTokenKind punctuation)
    {
        Assert.False(string.IsNullOrEmpty(punctuation.GetTokenText()));
    }

    public static IEnumerable<object[]> GetKeywordKinds()
    {
        return Enum.GetValues<MarkupTokenKind>()
                   .Where(t => t.ToString().EndsWith("Keyword"))
                   .Select(t => new object[] { t });
    }

    public static IEnumerable<object[]> GetPunctuationKinds()
    {
        return Enum.GetValues<MarkupTokenKind>()
                   .Where(t => t.ToString().EndsWith("Token") && t != MarkupTokenKind.ReferenceToken)
                   .Select(t => new object[] { t });
    }

    public static IEnumerable<object[]> GetTokenKinds()
    {
        return Enum.GetValues<MarkupTokenKind>()
                   .Select(t => new object[] { t });
    }
}