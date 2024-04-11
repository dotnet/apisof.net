namespace Terrajobst.ApiCatalog;

public static class MarkupFacts
{
    public static string? GetTokenText(this MarkupTokenKind kind)
    {
        return kind switch {
            MarkupTokenKind.None => null,

            // Whitespace
            MarkupTokenKind.LineBreak => Environment.NewLine,
            MarkupTokenKind.Space => " ",

            // Literals
            MarkupTokenKind.LiteralString => null,
            MarkupTokenKind.LiteralNumber => null,

            // Name
            MarkupTokenKind.ReferenceToken => null,

            // Punctuation
            MarkupTokenKind.AmpersandToken => "&",
            MarkupTokenKind.AsteriskToken => "*",
            MarkupTokenKind.BarToken => "|",
            MarkupTokenKind.CaretToken => "^",
            MarkupTokenKind.CloseBraceToken => "}",
            MarkupTokenKind.CloseBracketToken => "]",
            MarkupTokenKind.CloseParenToken => ")",
            MarkupTokenKind.ColonToken => ":",
            MarkupTokenKind.CommaToken => ",",
            MarkupTokenKind.DotToken => ".",
            MarkupTokenKind.EqualsEqualsToken => "==",
            MarkupTokenKind.EqualsToken => "=",
            MarkupTokenKind.ExclamationEqualsToken => "!=",
            MarkupTokenKind.ExclamationToken => "!",
            MarkupTokenKind.GreaterThanEqualsToken => ">=",
            MarkupTokenKind.GreaterThanGreaterThanGreaterThanToken => ">>>",
            MarkupTokenKind.GreaterThanGreaterThanToken => ">>",
            MarkupTokenKind.GreaterThanToken => ">",
            MarkupTokenKind.LessThanEqualsToken => "<=",
            MarkupTokenKind.LessThanLessThanToken => "<<",
            MarkupTokenKind.LessThanToken => "<",
            MarkupTokenKind.MinusMinusToken => "--",
            MarkupTokenKind.MinusToken => "-",
            MarkupTokenKind.OpenBraceToken => "{",
            MarkupTokenKind.OpenBracketToken => "[",
            MarkupTokenKind.OpenParenToken => "(",
            MarkupTokenKind.PercentToken => "%",
            MarkupTokenKind.PlusPlusToken => "++",
            MarkupTokenKind.PlusToken => "+",
            MarkupTokenKind.QuestionToken => "?",
            MarkupTokenKind.SemicolonToken => ";",
            MarkupTokenKind.SlashToken => "/",
            MarkupTokenKind.TildeToken => "~",

            // Keywords
            MarkupTokenKind.AbstractKeyword => "abstract",
            MarkupTokenKind.AddKeyword => "add",
            MarkupTokenKind.BoolKeyword => "bool",
            MarkupTokenKind.ByteKeyword => "byte",
            MarkupTokenKind.CdeclKeyword => "Cdecl",
            MarkupTokenKind.CharKeyword => "char",
            MarkupTokenKind.ClassKeyword => "class",
            MarkupTokenKind.ConstKeyword => "const",
            MarkupTokenKind.DecimalKeyword => "decimal",
            MarkupTokenKind.DefaultKeyword => "default",
            MarkupTokenKind.DelegateKeyword => "delegate",
            MarkupTokenKind.DoubleKeyword => "double",
            MarkupTokenKind.DynamicKeyword => "dynamic",
            MarkupTokenKind.EnumKeyword => "enum",
            MarkupTokenKind.EventKeyword => "event",
            MarkupTokenKind.ExplicitKeyword => "explicit",
            MarkupTokenKind.FalseKeyword => "false",
            MarkupTokenKind.FastcallKeyword => "Fastcall",
            MarkupTokenKind.FloatKeyword => "float",
            MarkupTokenKind.GetKeyword => "get",
            MarkupTokenKind.ImplicitKeyword => "implicit",
            MarkupTokenKind.InKeyword => "in",
            MarkupTokenKind.IntKeyword => "int",
            MarkupTokenKind.InterfaceKeyword => "interface",
            MarkupTokenKind.InternalKeyword => "internal",
            MarkupTokenKind.LongKeyword => "long",
            MarkupTokenKind.NamespaceKeyword => "namespace",
            MarkupTokenKind.NewKeyword => "new",
            MarkupTokenKind.NotnullKeyword => "notnull",
            MarkupTokenKind.NullKeyword => "null",
            MarkupTokenKind.ObjectKeyword => "object",
            MarkupTokenKind.OperatorKeyword => "operator",
            MarkupTokenKind.OutKeyword => "out",
            MarkupTokenKind.OverrideKeyword => "override",
            MarkupTokenKind.ParamsKeyword => "params",
            MarkupTokenKind.PrivateKeyword => "private",
            MarkupTokenKind.ProtectedKeyword => "protected",
            MarkupTokenKind.PublicKeyword => "public",
            MarkupTokenKind.ReadonlyKeyword => "readonly",
            MarkupTokenKind.RefKeyword => "ref",
            MarkupTokenKind.RemoveKeyword => "remove",
            MarkupTokenKind.ReturnKeyword => "return",
            MarkupTokenKind.SbyteKeyword => "sbyte",
            MarkupTokenKind.SealedKeyword => "sealed",
            MarkupTokenKind.SetKeyword => "set",
            MarkupTokenKind.ShortKeyword => "short",
            MarkupTokenKind.StaticKeyword => "static",
            MarkupTokenKind.StdcallKeyword => "Stdcall",
            MarkupTokenKind.StringKeyword => "string",
            MarkupTokenKind.StructKeyword => "struct",
            MarkupTokenKind.ThisKeyword => "this",
            MarkupTokenKind.ThiscallKeyword => "Thiscall",
            MarkupTokenKind.TrueKeyword => "true",
            MarkupTokenKind.TypeofKeyword => "typeof",
            MarkupTokenKind.UintKeyword => "uint",
            MarkupTokenKind.UlongKeyword => "ulong",
            MarkupTokenKind.UnmanagedKeyword => "unmanaged",
            MarkupTokenKind.UshortKeyword => "ushort",
            MarkupTokenKind.VirtualKeyword => "virtual",
            MarkupTokenKind.VoidKeyword => "void",
            MarkupTokenKind.VolatileKeyword => "volatile",
            MarkupTokenKind.WhereKeyword => "where",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public static MarkupTokenKind GetTokenKind(string? text)
    {
        return text switch {
            // Whitespace
            "\n" or "\r" or "\r\n" => MarkupTokenKind.LineBreak,
            " " => MarkupTokenKind.Space,

            // Punctuation
            "&" => MarkupTokenKind.AmpersandToken,
            "*" => MarkupTokenKind.AsteriskToken,
            "|" => MarkupTokenKind.BarToken,
            "^" => MarkupTokenKind.CaretToken,
            "}" => MarkupTokenKind.CloseBraceToken,
            "]" => MarkupTokenKind.CloseBracketToken,
            ")" => MarkupTokenKind.CloseParenToken,
            ":" => MarkupTokenKind.ColonToken,
            "," => MarkupTokenKind.CommaToken,
            "." => MarkupTokenKind.DotToken,
            "==" => MarkupTokenKind.EqualsEqualsToken,
            "=" => MarkupTokenKind.EqualsToken,
            "!=" => MarkupTokenKind.ExclamationEqualsToken,
            "!" => MarkupTokenKind.ExclamationToken,
            ">=" => MarkupTokenKind.GreaterThanEqualsToken,
            ">>>" => MarkupTokenKind.GreaterThanGreaterThanGreaterThanToken,
            ">>" => MarkupTokenKind.GreaterThanGreaterThanToken,
            ">" => MarkupTokenKind.GreaterThanToken,
            "<=" => MarkupTokenKind.LessThanEqualsToken,
            "<<" => MarkupTokenKind.LessThanLessThanToken,
            "<" => MarkupTokenKind.LessThanToken,
            "--" => MarkupTokenKind.MinusMinusToken,
            "-" => MarkupTokenKind.MinusToken,
            "{" => MarkupTokenKind.OpenBraceToken,
            "[" => MarkupTokenKind.OpenBracketToken,
            "(" => MarkupTokenKind.OpenParenToken,
            "%" => MarkupTokenKind.PercentToken,
            "++" => MarkupTokenKind.PlusPlusToken,
            "+" => MarkupTokenKind.PlusToken,
            "?" => MarkupTokenKind.QuestionToken,
            ";" => MarkupTokenKind.SemicolonToken,
            "/" => MarkupTokenKind.SlashToken,
            "~" => MarkupTokenKind.TildeToken,

            // Keywords
            "Cdecl" => MarkupTokenKind.CdeclKeyword,
            "Fastcall" => MarkupTokenKind.FastcallKeyword,
            "Stdcall" => MarkupTokenKind.StdcallKeyword,
            "Thiscall" => MarkupTokenKind.ThiscallKeyword,
            "abstract" => MarkupTokenKind.AbstractKeyword,
            "add" => MarkupTokenKind.AddKeyword,
            "bool" => MarkupTokenKind.BoolKeyword,
            "byte" => MarkupTokenKind.ByteKeyword,
            "char" => MarkupTokenKind.CharKeyword,
            "class" => MarkupTokenKind.ClassKeyword,
            "const" => MarkupTokenKind.ConstKeyword,
            "decimal" => MarkupTokenKind.DecimalKeyword,
            "default" => MarkupTokenKind.DefaultKeyword,
            "delegate" => MarkupTokenKind.DelegateKeyword,
            "double" => MarkupTokenKind.DoubleKeyword,
            "dynamic" => MarkupTokenKind.DynamicKeyword,
            "enum" => MarkupTokenKind.EnumKeyword,
            "event" => MarkupTokenKind.EventKeyword,
            "explicit" => MarkupTokenKind.ExplicitKeyword,
            "false" => MarkupTokenKind.FalseKeyword,
            "float" => MarkupTokenKind.FloatKeyword,
            "get" => MarkupTokenKind.GetKeyword,
            "implicit" => MarkupTokenKind.ImplicitKeyword,
            "in" => MarkupTokenKind.InKeyword,
            "int" => MarkupTokenKind.IntKeyword,
            "interface" => MarkupTokenKind.InterfaceKeyword,
            "internal" => MarkupTokenKind.InternalKeyword,
            "long" => MarkupTokenKind.LongKeyword,
            "namespace" => MarkupTokenKind.NamespaceKeyword,
            "new" => MarkupTokenKind.NewKeyword,
            "notnull" => MarkupTokenKind.NotnullKeyword,
            "null" => MarkupTokenKind.NullKeyword,
            "object" => MarkupTokenKind.ObjectKeyword,
            "operator" => MarkupTokenKind.OperatorKeyword,
            "out" => MarkupTokenKind.OutKeyword,
            "override" => MarkupTokenKind.OverrideKeyword,
            "params" => MarkupTokenKind.ParamsKeyword,
            "private" => MarkupTokenKind.PrivateKeyword,
            "protected" => MarkupTokenKind.ProtectedKeyword,
            "public" => MarkupTokenKind.PublicKeyword,
            "readonly" => MarkupTokenKind.ReadonlyKeyword,
            "ref" => MarkupTokenKind.RefKeyword,
            "remove" => MarkupTokenKind.RemoveKeyword,
            "return" => MarkupTokenKind.ReturnKeyword,
            "sbyte" => MarkupTokenKind.SbyteKeyword,
            "sealed" => MarkupTokenKind.SealedKeyword,
            "set" => MarkupTokenKind.SetKeyword,
            "short" => MarkupTokenKind.ShortKeyword,
            "static" => MarkupTokenKind.StaticKeyword,
            "string" => MarkupTokenKind.StringKeyword,
            "struct" => MarkupTokenKind.StructKeyword,
            "this" => MarkupTokenKind.ThisKeyword,
            "true" => MarkupTokenKind.TrueKeyword,
            "typeof" => MarkupTokenKind.TypeofKeyword,
            "uint" => MarkupTokenKind.UintKeyword,
            "ulong" => MarkupTokenKind.UlongKeyword,
            "unmanaged" => MarkupTokenKind.UnmanagedKeyword,
            "ushort" => MarkupTokenKind.UshortKeyword,
            "virtual" => MarkupTokenKind.VirtualKeyword,
            "void" => MarkupTokenKind.VoidKeyword,
            "volatile" => MarkupTokenKind.VolatileKeyword,
            "where" => MarkupTokenKind.WhereKeyword,

            _ => MarkupTokenKind.None
        };
    }

    public static bool IsKeyword(this MarkupTokenKind kind)
    {
        switch (kind)
        {
            case MarkupTokenKind.AbstractKeyword:
            case MarkupTokenKind.AddKeyword:
            case MarkupTokenKind.BoolKeyword:
            case MarkupTokenKind.ByteKeyword:
            case MarkupTokenKind.CdeclKeyword:
            case MarkupTokenKind.CharKeyword:
            case MarkupTokenKind.ClassKeyword:
            case MarkupTokenKind.ConstKeyword:
            case MarkupTokenKind.DecimalKeyword:
            case MarkupTokenKind.DefaultKeyword:
            case MarkupTokenKind.DelegateKeyword:
            case MarkupTokenKind.DoubleKeyword:
            case MarkupTokenKind.DynamicKeyword:
            case MarkupTokenKind.EnumKeyword:
            case MarkupTokenKind.EventKeyword:
            case MarkupTokenKind.ExplicitKeyword:
            case MarkupTokenKind.FalseKeyword:
            case MarkupTokenKind.FastcallKeyword:
            case MarkupTokenKind.FloatKeyword:
            case MarkupTokenKind.GetKeyword:
            case MarkupTokenKind.ImplicitKeyword:
            case MarkupTokenKind.InKeyword:
            case MarkupTokenKind.IntKeyword:
            case MarkupTokenKind.InterfaceKeyword:
            case MarkupTokenKind.InternalKeyword:
            case MarkupTokenKind.LongKeyword:
            case MarkupTokenKind.NamespaceKeyword:
            case MarkupTokenKind.NewKeyword:
            case MarkupTokenKind.NotnullKeyword:
            case MarkupTokenKind.NullKeyword:
            case MarkupTokenKind.ObjectKeyword:
            case MarkupTokenKind.OperatorKeyword:
            case MarkupTokenKind.OutKeyword:
            case MarkupTokenKind.OverrideKeyword:
            case MarkupTokenKind.ParamsKeyword:
            case MarkupTokenKind.PrivateKeyword:
            case MarkupTokenKind.ProtectedKeyword:
            case MarkupTokenKind.PublicKeyword:
            case MarkupTokenKind.ReadonlyKeyword:
            case MarkupTokenKind.RefKeyword:
            case MarkupTokenKind.RemoveKeyword:
            case MarkupTokenKind.ReturnKeyword:
            case MarkupTokenKind.SbyteKeyword:
            case MarkupTokenKind.SealedKeyword:
            case MarkupTokenKind.SetKeyword:
            case MarkupTokenKind.ShortKeyword:
            case MarkupTokenKind.StaticKeyword:
            case MarkupTokenKind.StdcallKeyword:
            case MarkupTokenKind.StringKeyword:
            case MarkupTokenKind.StructKeyword:
            case MarkupTokenKind.ThisKeyword:
            case MarkupTokenKind.ThiscallKeyword:
            case MarkupTokenKind.TrueKeyword:
            case MarkupTokenKind.TypeofKeyword:
            case MarkupTokenKind.UintKeyword:
            case MarkupTokenKind.UlongKeyword:
            case MarkupTokenKind.UnmanagedKeyword:
            case MarkupTokenKind.UshortKeyword:
            case MarkupTokenKind.VirtualKeyword:
            case MarkupTokenKind.VoidKeyword:
            case MarkupTokenKind.VolatileKeyword:
            case MarkupTokenKind.WhereKeyword:
                return true;
            default:
                return false;
        }
    }

    public static bool IsPunctuation(this MarkupTokenKind kind)
    {
        switch (kind)
        {
            case MarkupTokenKind.AmpersandToken:
            case MarkupTokenKind.AsteriskToken:
            case MarkupTokenKind.BarToken:
            case MarkupTokenKind.CaretToken:
            case MarkupTokenKind.CloseBraceToken:
            case MarkupTokenKind.CloseBracketToken:
            case MarkupTokenKind.CloseParenToken:
            case MarkupTokenKind.ColonToken:
            case MarkupTokenKind.CommaToken:
            case MarkupTokenKind.DotToken:
            case MarkupTokenKind.EqualsEqualsToken:
            case MarkupTokenKind.EqualsToken:
            case MarkupTokenKind.ExclamationEqualsToken:
            case MarkupTokenKind.ExclamationToken:
            case MarkupTokenKind.GreaterThanEqualsToken:
            case MarkupTokenKind.GreaterThanGreaterThanGreaterThanToken:
            case MarkupTokenKind.GreaterThanGreaterThanToken:
            case MarkupTokenKind.GreaterThanToken:
            case MarkupTokenKind.LessThanEqualsToken:
            case MarkupTokenKind.LessThanLessThanToken:
            case MarkupTokenKind.LessThanToken:
            case MarkupTokenKind.MinusMinusToken:
            case MarkupTokenKind.MinusToken:
            case MarkupTokenKind.OpenBraceToken:
            case MarkupTokenKind.OpenBracketToken:
            case MarkupTokenKind.OpenParenToken:
            case MarkupTokenKind.PercentToken:
            case MarkupTokenKind.PlusPlusToken:
            case MarkupTokenKind.PlusToken:
            case MarkupTokenKind.QuestionToken:
            case MarkupTokenKind.SemicolonToken:
            case MarkupTokenKind.SlashToken:
            case MarkupTokenKind.TildeToken:
                return true;
            default:
                return false;
        }
    }
}