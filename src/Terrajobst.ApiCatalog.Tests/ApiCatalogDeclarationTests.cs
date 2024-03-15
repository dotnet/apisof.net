using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Xunit;

namespace Terrajobst.ApiCatalog.Tests;

public class ApiCatalogDeclarationTests
{
    [Fact]
    public void Enum()
    {
        var source = @"
            namespace Test
            {
                public enum TheEnum
                {
                    A,
                    B
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public enum TheEnum
                {
                    A = 0,
                    B = 1
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Enum_WithBase()
    {
        var source = @"
            namespace Test
            {
                public enum TheEnum : byte
                {
                    A,
                    B
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public enum TheEnum : byte
                {
                    A = 0,
                    B = 1
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Enum_Flags()
    {
        var source = @"
            using System;
            namespace Test
            {
                [Flags]
                public enum TheEnum
                {
                    None = 0,
                    A = 1,
                    B = 2,
                    C = A | B
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [Flags]
                public enum TheEnum
                {
                    None = 0,
                    A = 1,
                    B = 2,
                    C = A | B
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Struct()
    {
        var source = @"
            namespace Test
            {
                public struct TheStruct
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public struct TheStruct
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Struct_ReadOnly()
    {
        var source = @"
            namespace Test
            {
                public readonly struct TheStruct
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public readonly struct TheStruct
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Struct_WithInterface()
    {
        var source = @"
            using System;
            namespace Test
            {
                public struct TheStruct : IComparable
                {
                    int IComparable.CompareTo(object other) => 0;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public struct TheStruct : IComparable
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Struct_WithInterfaces()
    {
        var source = @"
            using System;
            namespace Test
            {
                public struct TheStruct : IComparable, ICloneable
                {
                    int IComparable.CompareTo(object other) => 0;
                    object ICloneable.Clone() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public struct TheStruct : ICloneable, IComparable
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Class()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Class_Derived()
    {
        var source = @"
            namespace Test
            {
                public class TheBase
                {
                    internal TheBase() { }
                }
                public class TheDerived : TheBase
                {
                    internal TheDerived() { }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheBase
                {
                }
                public class TheDerived : TheBase
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Class_DerivedAndInterface()
    {
        var source = @"
            using System;
            namespace Test
            {
                public class TheBase
                {
                    internal TheBase() { }
                }
                public class TheDerived : TheBase, IComparable
                {
                    internal TheDerived() { }
                    int IComparable.CompareTo(object other) => 0;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheBase
                {
                }
                public class TheDerived : TheBase, IComparable
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Class_DerivedAndInterfaces()
    {
        var source = @"
            using System;
            namespace Test
            {
                public class TheBase
                {
                    internal TheBase() { }
                }
                public class TheDerived : TheBase, IComparable, ICloneable
                {
                    internal TheDerived() { }
                    int IComparable.CompareTo(object other) => 0;
                    object ICloneable.Clone() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheBase
                {
                }
                public class TheDerived : TheBase, ICloneable, IComparable
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Class_Abstract()
    {
        var source = @"
            namespace Test
            {
                public abstract class TheClass
                {
                    internal TheClass() { }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public abstract class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Class_Sealed()
    {
        var source = @"
            namespace Test
            {
                public sealed class TheClass
                {
                    internal TheClass() { }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public sealed class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Class_Static()
    {
        var source = @"
            namespace Test
            {
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Class_Nested_Public()
    {
        var source = @"
            namespace Test
            {
                public class TheOuterClass
                {
                    private TheOuterClass() { }
                    public class TheInnerClass
                    {
                        private TheInnerClass() { }
                    }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheOuterClass
                {
                    public class TheInnerClass
                    {
                    }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Class_Nested_Protected()
    {
        var source = @"
            namespace Test
            {
                public class TheOuterClass
                {
                    private TheOuterClass() { }
                    protected class TheInnerClass
                    {
                        private TheInnerClass() { }
                    }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheOuterClass
                {
                    protected class TheInnerClass
                    {
                    }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Interface()
    {
        var source = @"
            namespace Test
            {
                public interface TheInterface
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public interface TheInterface
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Interface_WithInterface()
    {
        var source = @"
            using System;
            namespace Test
            {
                public interface TheInterface : IComparable
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public interface TheInterface : IComparable
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Interface_WithInterfaces()
    {
        var source = @"
            using System;
            namespace Test
            {
                public interface TheInterface : IComparable, ICloneable
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public interface TheInterface : ICloneable, IComparable
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Interface_Property()
    {
        var source = @"
            namespace Test
            {
                public interface TheInterface
                {
                    int Length { get; }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public interface TheInterface
                {
                    int Length { get; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Interface_Property_Default()
    {
        var source = @"
            namespace Test
            {
                public interface TheInterface
                {
                    virtual int Length { get { return -1; } }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public interface TheInterface
                {
                    virtual int Length { get; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Interface_Event()
    {
        var source = @"
            using System;
            namespace Test
            {
                public interface TheInterface
                {
                    event EventHandler OnChanged;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public interface TheInterface
                {
                    event EventHandler OnChanged;
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Interface_Event_Default()
    {
        var source = @"
            using System;
            namespace Test
            {
                public interface TheInterface
                {
                    virtual event EventHandler OnChanged { add { } remove { } }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public interface TheInterface
                {
                    virtual event EventHandler OnChanged;
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Interface_Method()
    {
        var source = @"
            namespace Test
            {
                public interface TheInterface
                {
                    int Test();
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public interface TheInterface
                {
                    int Test();
                }
            }
        ";

        Assert(source, expected);
    }

    // TODO: Interface statics

    [Fact]
    public void Delegate()
    {
        var source = @"
            namespace Test
            {
                public delegate void TheDelegate();
            }
        ";

        var expected = @"
            namespace Test
            {
                public delegate void TheDelegate();
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Delegate_WithArg()
    {
        var source = @"
            namespace Test
            {
                public delegate void TheDelegate(int arg0);
            }
        ";

        var expected = @"
            namespace Test
            {
                public delegate void TheDelegate(int arg0);
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Delegate_WithArgs()
    {
        var source = @"
            namespace Test
            {
                public delegate void TheDelegate(int arg0, string arg1);
            }
        ";

        var expected = @"
            namespace Test
            {
                public delegate void TheDelegate(int arg0, string arg1);
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Property_Get_Only()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public int Length { get { return 0; } }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public int Length { get; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Property_Set_Only()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public int Length { set { } }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public int Length { set; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Property_Get_And_Set()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public int Length { get; set; }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public int Length { get; set; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Property_Virtual()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public virtual int Length { get; set; }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public virtual int Length { get; set; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Property_Abstract()
    {
        var source = @"
            namespace Test
            {
                public abstract class TheClass
                {
                    private TheClass() { }
                    public abstract int Length { get; }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public abstract class TheClass
                {
                    public abstract int Length { get; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Property_Override()
    {
        var source = @"
            namespace Test
            {
                public abstract class TheBase
                {
                    internal TheBase() { }
                    public abstract int Length { get; }
                }
                public class TheDerived : TheBase
                {
                    private TheDerived() { }
                    public override int Length { get { return -1; } }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public abstract class TheBase
                {
                    public abstract int Length { get; }
                }
                public class TheDerived : TheBase
                {
                    public override int Length { get; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Property_Sealed()
    {
        var source = @"
            namespace Test
            {
                public abstract class TheBase
                {
                    internal TheBase() { }
                    public abstract int Length { get; }
                }
                public class TheDerived : TheBase
                {
                    private TheDerived() { }
                    public override sealed int Length { get { return -1; } }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public abstract class TheBase
                {
                    public abstract int Length { get; }
                }
                public class TheDerived : TheBase
                {
                    public override sealed int Length { get; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Property_Attribute()
    {
        var source = @"
            using System;
            namespace Test
            {
                public static class TheClass
                {
                    [Obsolete]
                    public static int Length { get; set; }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    [Obsolete]
                    public static int Length { get; set; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Property_Get_Attribute()
    {
        var source = @"
            using System;
            namespace Test
            {
                public static class TheClass
                {
                    public static int Length
                    {
                        [Obsolete]
                        get;
                    }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static int Length
                    {
                        [Obsolete]
                        get;
                    }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Property_Set_Attribute()
    {
        var source = @"
            using System;
            namespace Test
            {
                public static class TheClass
                {
                    public static int Length
                    {
                        [Obsolete]
                        set { }
                    }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static int Length
                    {
                        [Obsolete]
                        set;
                    }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Property_Get_Attribute_And_Set()
    {
        var source = @"
            using System;
            namespace Test
            {
                public static class TheClass
                {
                    public static int Length
                    {
                        [Obsolete]
                        get;
                        set;
                    }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static int Length
                    {
                        [Obsolete]
                        get;
                        set;
                    }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Property_Get_Attribute_And_Set_Attribute()
    {
        var source = @"
            using System;
            using System.Diagnostics;
            namespace Test
            {
                public static class TheClass
                {
                    public static int Length
                    {
                        [Obsolete]
                        get;
                        [DebuggerStepThrough]
                        set;
                    }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static int Length
                    {
                        [Obsolete]
                        get;
                        [DebuggerStepThrough]
                        set;
                    }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Indexer_Get_Only()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public char this[int index] { get => throw null; }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultMember(""Item"")]
                public class TheClass
                {
                    public char this[int index] { get; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Indexer_Set_Only()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public char this[int index] { set { } }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultMember(""Item"")]
                public class TheClass
                {
                    public char this[int index] { set; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Indexer_Get_And_Set()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public char this[int index] { set { } get => throw null; }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultMember(""Item"")]
                public class TheClass
                {
                    public char this[int index] { get; set; }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Event()
    {
        var source = @"
            using System;
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public event EventHandler Changed;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public event EventHandler Changed;
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Event_Virtual()
    {
        var source = @"
            using System;
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public virtual event EventHandler Changed;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public virtual event EventHandler Changed;
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Event_Abstract()
    {
        var source = @"
            using System;
            namespace Test
            {
                public abstract class TheClass
                {
                    private TheClass() { }
                    public abstract event EventHandler Changed;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public abstract class TheClass
                {
                    public abstract event EventHandler Changed;
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Event_Override()
    {
        var source = @"
            using System;
            namespace Test
            {
                public abstract class TheBase
                {
                    internal TheBase() { }
                    public abstract event EventHandler Changed;
                }
                public class TheDerived : TheBase
                {
                    private TheDerived() { }
                    public override event EventHandler Changed;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public abstract class TheBase
                {
                    public abstract event EventHandler Changed;
                }
                public class TheDerived : TheBase
                {
                    public override event EventHandler Changed;
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Event_Sealed()
    {
        var source = @"
            using System;
            namespace Test
            {
                public abstract class TheBase
                {
                    internal TheBase() { }
                    public abstract event EventHandler Changed;
                }
                public class TheDerived : TheBase
                {
                    private TheDerived() { }
                    public override sealed event EventHandler Changed;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public abstract class TheBase
                {
                    public abstract event EventHandler Changed;
                }
                public class TheDerived : TheBase
                {
                    public override sealed event EventHandler Changed;
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Event_Attribute()
    {
        var source = @"
            using System;
            namespace Test
            {
                public static class TheClass
                {
                    [Obsolete]
                    public static event EventHandler Changed;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    [Obsolete]
                    public static event EventHandler Changed;
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Event_Add_Attribute()
    {
        var source = @"
            using System;
            using System.Diagnostics;
            namespace Test
            {
                public static class TheClass
                {
                    public static event EventHandler Changed
                    {
                        [DebuggerHidden]
                        add { }
                        remove { }
                    }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static event EventHandler Changed
                    {
                        [DebuggerHidden]
                        add;
                        remove;
                    }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Event_Remove_Attribute()
    {
        var source = @"
            using System;
            using System.Diagnostics;
            namespace Test
            {
                public static class TheClass
                {
                    public static event EventHandler Changed
                    {
                        add { }
                        [DebuggerHidden]
                        remove { }
                    }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static event EventHandler Changed
                    {
                        add;
                        [DebuggerHidden]
                        remove;
                    }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Event_Add_Attribute_And_Remove_Attribute()
    {
        var source = @"
            using System;
            using System.Diagnostics;
            namespace Test
            {
                public static class TheClass
                {
                    public static event EventHandler Changed
                    {
                        [DebuggerHidden]
                        add { }
                        [DebuggerStepThrough]
                        remove { }
                    }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static event EventHandler Changed
                    {
                        [DebuggerHidden]
                        add;
                        [DebuggerStepThrough]
                        remove;
                    }
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Field()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public int TheField;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public int TheField;
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Field_Static()
    {
        var source = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static int TheField;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static int TheField;
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Field_ReadOnly()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public readonly int TheField;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public readonly int TheField;
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Field_Const()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public const int TheField = 42;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public const int TheField = 42;
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public void TheMethod() {}
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public void TheMethod();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_Static()
    {
        var source = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static void TheMethod() {}
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static void TheMethod();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_Virtual()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public virtual void TheMethod() {}
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public virtual void TheMethod();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_Abstract()
    {
        var source = @"
            namespace Test
            {
                public abstract class TheClass
                {
                    private TheClass() { }
                    public abstract void TheMethod();
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public abstract class TheClass
                {
                    public abstract void TheMethod();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_Override()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public override string ToString() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public override string ToString();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_Sealed()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public override sealed string ToString() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public override sealed string ToString();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_RefReturn()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public ref int TheMethod() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public ref int TheMethod();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_WithArg()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public void TheMethod(int arg0) { }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public void TheMethod(int arg0);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_WithArgs()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public void TheMethod(int arg0, string arg1) { }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public void TheMethod(int arg0, string arg1);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_WithoutParams()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public void TheMethod(object[] args) { }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public void TheMethod(object[] args);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_WithParams()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public void TheMethod(params object[] args) { }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public void TheMethod(params object[] args);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_Attribute()
    {
        var source = @"
            using System;
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    [Obsolete]
                    public int TheMethod(int arg0) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    [Obsolete]
                    public int TheMethod(int arg0);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_Attribute_On_Return()
    {
        var source = @"
            using System.ComponentModel;
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    [return: DefaultValue(0)]
                    public int TheMethod(int arg0) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    [return: DefaultValue(0)]
                    public int TheMethod(int arg0);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Method_Attribute_On_Parameter()
    {
        var source = @"
            using System.ComponentModel;
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }

                    public int TheMethod([DefaultValue(0)] int arg0) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public int TheMethod([DefaultValue(0)] int arg0);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Constructor()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    public TheClass() { }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public TheClass();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Constructor_Static()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    static TheClass() { }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Finalizer()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    ~TheClass() { }
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    ~TheClass();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Operator_Implicit()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public static implicit operator TheClass(int value) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public static implicit operator TheClass(int value);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Operator_Explicit()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public static explicit operator TheClass(int value) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public static explicit operator TheClass(int value);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Operator_Unary()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public static float operator +(TheClass operand) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public static float operator +(TheClass operand);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Operator_Binary()
    {
        var source = @"
            namespace Test
            {
                public class TheClass
                {
                    private TheClass() { }
                    public static TheClass operator +(TheClass left, int right) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public class TheClass
                {
                    public static TheClass operator +(TheClass left, int right);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_Generic()
    {
        var source = @"
            using System.Collections.Generic;

            namespace Test
            {
                public static class TheClass
                {
                    public static IEnumerable<string> Test() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static IEnumerable<string> Test();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_Array()
    {
        var source = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static string[] Test() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static string[] Test();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_Array_MultiDimensional()
    {
        var source = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static string[,,] Test() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static string[,,] Test();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_Pointer()
    {
        var source = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static unsafe int* Test() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static int* Test();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_Nullable_ValueType()
    {
        var source = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static int? Test() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static int? Test();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_Nullable_ReferenceType_Nullable()
    {
        var source = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static string? Test() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static string? Test();
                }
            }
        ";

        Assert(source, expected, nullableEnabled: true);
    }

    [Fact]
    public void Signature_Nullable_ReferenceType_NonNullable()
    {
        var source = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static string Test() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static string! Test();
                }
            }
        ";

        Assert(source, expected, nullableEnabled: true);
    }

    [Fact]
    public void Signature_Nullable_ReferenceType_Oblivious()
    {
        var source = @"
            #nullable disable
            namespace Test
            {
                public static class TheClass
                {
                    public static string Test() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static string Test();
                }
            }
        ";

        Assert(source, expected, nullableEnabled: true);
    }

    [Fact]
    public void Signature_Tuple()
    {
        var source = @"
            #nullable disable
            namespace Test
            {
                public static class TheClass
                {
                    public static (string, int) Test() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static (string, int) Test();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_Tuple_Named()
    {
        var source = @"
            #nullable disable
            namespace Test
            {
                public static class TheClass
                {
                    public static (string Key, int Value) Test() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static (string Key, int Value) Test();
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_Tuple_Named_Partially()
    {
        var source = @"
            #nullable disable
            namespace Test
            {
                public static class TheClass
                {
                    public static (string, int Value, float) Test() => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static (string, int Value, float) Test();
                }
            }
        ";

        Assert(source, expected);
    }
    [Fact]
    public void Signature_FunctionPointer()
    {
        var source = @"
            #nullable disable
            namespace Test
            {
                public static class TheClass
                {
                    public unsafe static T M<T>(delegate*<T, int, void> combinator) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static T M<T>(delegate*<T, int, void> combinator);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_FunctionPointer_NoArgs()
    {
        var source = @"
            #nullable disable
            namespace Test
            {
                public static class TheClass
                {
                    public unsafe static T M<T>(delegate*<void> combinator) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static T M<T>(delegate*<void> combinator);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_FunctionPointer_Managed()
    {
        var source = @"
            #nullable disable
            namespace Test
            {
                public static class TheClass
                {
                    public unsafe static T M<T>(delegate* managed<void> combinator) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static T M<T>(delegate*<void> combinator);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_FunctionPointer_Unmanaged()
    {
        var source = @"
            #nullable disable
            namespace Test
            {
                public static class TheClass
                {
                    public unsafe static T M<T>(delegate* unmanaged<void> combinator) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static T M<T>(delegate* unmanaged<void> combinator);
                }
            }
        ";

        Assert(source, expected);
    }

    [Theory]
    [InlineData("Cdecl")]
    [InlineData("Stdcall")]
    [InlineData("Fastcall")]
    [InlineData("Thiscall")]
    public void Signature_FunctionPointer_Unmanaged_With_CallingConvention(string callingConvention)
    {
        var source = @"
            #nullable disable
            namespace Test
            {
                public static class TheClass
                {
                    public unsafe static T M<T>(delegate* unmanaged[%CC%]<void> combinator) => throw null;
                }
            }
        ".Replace("%CC%", callingConvention);

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static T M<T>(delegate* unmanaged[%CC%]<void> combinator);
                }
            }
        ".Replace("%CC%", callingConvention);

        Assert(source, expected);
    }

    [Fact]
    public void Signature_Dynamic()
    {
        var source = @"
            using System.Collections.Generic;
            namespace Test
            {
                public static class TheClass
                {
                    public static dynamic M(dynamic p1, IEnumerable<dynamic> p2) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static dynamic M(dynamic p1, IEnumerable<dynamic> p2);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_Parameter_Ref()
    {
        var source = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static void M(ref int p0) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static void M(ref int p0);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_Parameter_Out()
    {
        var source = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static void M(out int p0) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static void M(out int p0);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Signature_Parameter_In()
    {
        var source = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static void M(in int p0) => throw null;
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                public static class TheClass
                {
                    public static void M(in int p0);
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Attribute_Null()
    {
        var source = @"
            using System.ComponentModel;
            namespace Test
            {
                [DefaultValue((object)null)]
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultValue(null)]
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Attribute_String()
    {
        var source = @"
            using System.ComponentModel;
            namespace Test
            {
                [DefaultValue(""test"")]
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultValue(""test"")]
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Attribute_Boolean()
    {
        var source = @"
            using System.ComponentModel;
            namespace Test
            {
                [DefaultValue(false)]
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultValue(false)]
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Attribute_Int32()
    {
        var source = @"
            using System.ComponentModel;
            namespace Test
            {
                [DefaultValue(1)]
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultValue(1)]
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Attribute_Int16()
    {
        var source = @"
            using System.ComponentModel;
            namespace Test
            {
                [DefaultValue((short)1)]
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultValue(1)]
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Attribute_Single()
    {
        var source = @"
            using System.ComponentModel;
            namespace Test
            {
                [DefaultValue(1.5F)]
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultValue(1.5F)]
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Attribute_Double()
    {
        var source = @"
            using System.ComponentModel;
            namespace Test
            {
                [DefaultValue(1.5)]
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultValue(1.5)]
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Attribute_Array()
    {
        var source = @"
            using System.ComponentModel;
            namespace Test
            {
                [DefaultValue(new[] { 1, 2, 3 })]
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultValue(new[] { 1, 2, 3 })]
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Attribute_Enum()
    {
        var source = @"
            using System.ComponentModel;
            namespace Test
            {
                [DefaultValue(EditorBrowsableState.Never)]
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultValue(EditorBrowsableState.Never)]
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Attribute_Enum_Flags()
    {
        var source = @"
            using System;
            using System.ComponentModel;
            namespace Test
            {
                [DefaultValue(AttributeTargets.Method | AttributeTargets.Field)]
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultValue(AttributeTargets.Method | AttributeTargets.Field)]
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Attribute_Enum_Flags_All()
    {
        var source = @"
            using System;
            using System.ComponentModel;
            namespace Test
            {
                [DefaultValue(AttributeTargets.All)]
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultValue(AttributeTargets.All)]
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    [Fact]
    public void Attribute_Type()
    {
        var source = @"
            using System.ComponentModel;
            namespace Test
            {
                [DefaultValue(typeof(TheClass))]
                public static class TheClass
                {
                }
            }
        ";

        var expected = @"
            namespace Test
            {
                [DefaultValue(typeof(TheClass))]
                public static class TheClass
                {
                }
            }
        ";

        Assert(source, expected);
    }

    private static void Assert(string source, string expected, bool nullableEnabled = false)
    {
        var referencePaths = new[] {
            typeof(object).Assembly.Location,
            typeof(DynamicAttribute).Assembly.Location
        };

        var references = referencePaths.Select(p => MetadataReference.CreateFromFile(p)).ToArray();
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            allowUnsafe: true,
            nullableContextOptions: nullableEnabled ? NullableContextOptions.Enable : NullableContextOptions.Disable,
            optimizationLevel: OptimizationLevel.Release);
        var compilation = CSharpCompilation.Create("dummy",
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            options);

        var peStream = new MemoryStream();
        var result = compilation.Emit(peStream);
        if (!result.Success)
        {
            var diagnostics = string.Join(Environment.NewLine, result.Diagnostics);
            var message = $"Compilation has errors{Environment.NewLine}{diagnostics}";
            throw new Exception(message);
        }

        peStream.Position = 0;

        var reference = MetadataReference.CreateFromStream(peStream, filePath: "dummy.dll");
        var context = MetadataContext.Create(new[] { reference }, references);
        var entry = AssemblyEntry.Create(context.Assemblies.Single());
        using var stringWriter = new StringWriter();
        using var indentedWriter = new IndentedTextWriter(stringWriter, new string(' ', 4));
        foreach (var api in entry.Apis)
            Emit(indentedWriter, api);

        expected = expected.Unindent();
        var actual = stringWriter.ToString();

        var expectedLines = expected.SplitLines();
        var actualLines = actual.SplitLines();

        var maxLength = Math.Min(expectedLines.Length, actualLines.Length);
        for (var i = 0; i < maxLength; i++)
            Xunit.Assert.Equal(expectedLines[i], actualLines[i]);

        for (var i = maxLength; i < expectedLines.Length; i++)
            Xunit.Assert.Equal(expectedLines[i], string.Empty);

        for (var i = maxLength; i < actualLines.Length; i++)
            Xunit.Assert.Equal(string.Empty, actualLines[i]);

        static void Emit(IndentedTextWriter writer, ApiEntry api, bool addComma = false)
        {
            var markup = Markup.Parse(api.Syntax);

            var lines = markup.ToString().SplitLines();
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (addComma && i == lines.Length - 1)
                    writer.WriteLine(line + ",");
                else
                    writer.WriteLine(line);
            }

            var canHaveChildren = api.Kind == ApiKind.Namespace ||
                                  api.Kind.IsType() && api.Kind != ApiKind.Delegate;

            if (canHaveChildren)
            {
                writer.WriteLine("{");
                writer.Indent++;

                foreach (var child in api.Children)
                {
                    var needsComma = api.Kind == ApiKind.Enum && child != api.Children.Last();
                    Emit(writer, child, needsComma);
                }

                writer.Indent--;
                writer.WriteLine("}");
            }
        }
    }
}