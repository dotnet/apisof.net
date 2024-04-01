using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Terrajobst.ApiCatalog.MarkupTokenKind;

namespace Terrajobst.ApiCatalog;

internal static class CSharpDeclarationWriter
{
    public static void WriteDeclaration(ISymbol symbol, SyntaxWriter writer)
    {
        switch (symbol)
        {
            case INamespaceSymbol @namespace:
                WriteNamespaceDeclaration(@namespace, writer);
                break;
            case INamedTypeSymbol type:
                WriteTypeDeclaration(type, writer);
                break;
            case IFieldSymbol field:
                WriteFieldDeclaration(field, writer);
                break;
            case IMethodSymbol method:
                WriteMethodDeclaration(method, writer);
                break;
            case IPropertySymbol property:
                WritePropertyDeclaration(property, writer);
                break;
            case IEventSymbol @event:
                WriteEventDeclaration(@event, writer);
                break;
            default:
                throw new Exception($"Unexpected symbol {symbol.Kind}");
        }
    }

    private static void WriteNamespaceDeclaration(INamespaceSymbol @namespace, SyntaxWriter writer)
    {
        writer.Write(NamespaceKeyword);
        writer.WriteSpace();

        var parents = new Stack<INamespaceSymbol>();
        var c = @namespace;
        while (c is not null && !c.IsGlobalNamespace)
        {
            parents.Push(c);
            c = c.ContainingNamespace;
        }

        var isFirst = true;

        while (parents.Count > 0)
        {
            if (isFirst)
                isFirst = false;
            else
                writer.Write(DotToken);

            var n = parents.Pop();
            writer.WriteReference(n, n.Name);
        }
    }

    private static void WriteTypeDeclaration(INamedTypeSymbol type, SyntaxWriter writer)
    {
        switch (type.TypeKind)
        {
            case TypeKind.Class:
                WriteClassDeclaration(type, writer);
                break;
            case TypeKind.Struct:
                WriteStructDeclaration(type, writer);
                break;
            case TypeKind.Interface:
                WriteInterfaceDeclaration(type, writer);
                break;
            case TypeKind.Delegate:
                WriteDelegateDeclaration(type, writer);
                break;
            case TypeKind.Enum:
                WriteEnumDeclaration(type, writer);
                break;
            default:
                throw new Exception($"Unexpected type kind {type.TypeKind}");
        }
    }

    private static void WriteClassDeclaration(INamedTypeSymbol type, SyntaxWriter writer)
    {
        WriteAttributeList(type.GetAttributes(), writer);
        WriteAccessibility(type.DeclaredAccessibility, writer);

        writer.WriteSpace();

        if (type.IsStatic)
        {
            writer.Write(StaticKeyword);
            writer.WriteSpace();
        }
        else if (type.IsAbstract)
        {
            writer.Write(AbstractKeyword);
            writer.WriteSpace();
        }
        else if (type.IsSealed)
        {
            writer.Write(SealedKeyword);
            writer.WriteSpace();
        }

        if (type.IsReadOnly)
        {
            writer.Write(ReadonlyKeyword);
            writer.WriteSpace();
        }

        writer.Write(ClassKeyword);
        writer.WriteSpace();

        writer.WriteReference(type, type.Name);

        WriteTypeParameterList(type.TypeParameters, writer);

        var hasBaseType = type.BaseType is not null &&
                              type.BaseType.SpecialType != SpecialType.System_Object;

        var implementsInterfaces = type.Interfaces.Where(t => t.IsIncludedInCatalog()).Any();

        if (hasBaseType || implementsInterfaces)
        {
            writer.WriteSpace();
            writer.Write(ColonToken);

            if (hasBaseType)
            {
                writer.WriteSpace();
                WriteTypeReference(type.BaseType, writer);
            }

            if (implementsInterfaces)
            {
                var isFirst = !hasBaseType;

                foreach (var @interface in type.Interfaces.Where(t => t.IsIncludedInCatalog()).Ordered())
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        writer.Write(CommaToken);

                    writer.WriteSpace();
                    WriteTypeReference(@interface, writer);
                }
            }
        }

        WriteConstraints(type.TypeParameters, writer);
    }

    private static void WriteStructDeclaration(INamedTypeSymbol type, SyntaxWriter writer)
    {
        WriteAttributeList(type.GetAttributes(), writer);
        WriteAccessibility(type.DeclaredAccessibility, writer);

        writer.WriteSpace();

        if (type.IsReadOnly)
        {
            writer.Write(ReadonlyKeyword);
            writer.WriteSpace();
        }

        writer.Write(StructKeyword);
        writer.WriteSpace();
        writer.WriteReference(type, type.Name);

        WriteTypeParameterList(type.TypeParameters, writer);

        if (type.Interfaces.Where(t => t.IsIncludedInCatalog()).Any())
        {
            writer.WriteSpace();
            writer.Write(ColonToken);

            var isFirst = true;

            foreach (var @interface in type.Interfaces.Where(t => t.IsIncludedInCatalog()).Ordered())
            {
                if (isFirst)
                    isFirst = false;
                else
                    writer.Write(CommaToken);

                writer.WriteSpace();
                WriteTypeReference(@interface, writer);
            }
        }

        WriteConstraints(type.TypeParameters, writer);
    }

    private static void WriteInterfaceDeclaration(INamedTypeSymbol type, SyntaxWriter writer)
    {
        WriteAttributeList(type.GetAttributes(), writer);
        WriteAccessibility(type.DeclaredAccessibility, writer);

        writer.WriteSpace();
        writer.Write(InterfaceKeyword);
        writer.WriteSpace();
        writer.WriteReference(type, type.Name);

        WriteTypeParameterList(type.TypeParameters, writer);

        if (type.Interfaces.Where(t => t.IsIncludedInCatalog()).Any())
        {
            writer.WriteSpace();
            writer.Write(ColonToken);

            var isFirst = true;

            foreach (var @interface in type.Interfaces.Where(t => t.IsIncludedInCatalog()).Ordered())
            {
                if (isFirst)
                    isFirst = false;
                else
                    writer.Write(CommaToken);

                writer.WriteSpace();
                WriteTypeReference(@interface, writer);
            }
        }

        WriteConstraints(type.TypeParameters, writer);
    }

    private static void WriteDelegateDeclaration(INamedTypeSymbol type, SyntaxWriter writer)
    {
        WriteAttributeList(type.GetAttributes(), writer);
        WriteAccessibility(type.DeclaredAccessibility, writer);

        writer.WriteSpace();
        writer.Write(DelegateKeyword);
        writer.WriteSpace();

        var invokeMethod = (IMethodSymbol)type.GetMembers("Invoke")
            .First();

        WriteTypeReference(invokeMethod.ReturnType, writer);
        writer.WriteSpace();
        writer.WriteReference(type, type.Name);
        WriteParameterList(invokeMethod.Parameters, writer);

        WriteConstraints(type.TypeParameters, writer);

        writer.Write(SemicolonToken);
    }

    private static void WriteEnumDeclaration(INamedTypeSymbol type, SyntaxWriter writer)
    {
        WriteAttributeList(type.GetAttributes(), writer);
        WriteAccessibility(type.DeclaredAccessibility, writer);

        writer.WriteSpace();
        writer.Write(EnumKeyword);
        writer.WriteSpace();

        writer.WriteReference(type, type.Name);

        if (type.EnumUnderlyingType is not null && type.EnumUnderlyingType.SpecialType != SpecialType.System_Int32)
        {
            writer.WriteSpace();
            writer.Write(ColonToken);
            writer.WriteSpace();
            WriteTypeReference(type.EnumUnderlyingType, writer);
        }
    }

    private static void WriteFieldDeclaration(IFieldSymbol field, SyntaxWriter writer)
    {
        if (field.ContainingType.TypeKind == TypeKind.Enum)
        {
            WriteEnumFieldDeclaration(field, writer);
            return;
        }

        WriteAttributeList(field.GetAttributes(), writer);
        WriteAccessibility(field.DeclaredAccessibility, writer);

        writer.WriteSpace();

        if (field.IsConst)
        {
            writer.Write(ConstKeyword);
            writer.WriteSpace();
        }
        else
        {
            if (field.IsStatic)
            {
                writer.Write(StaticKeyword);
                writer.WriteSpace();
            }
            if (field.IsReadOnly)
            {
                writer.Write(ReadonlyKeyword);
                writer.WriteSpace();
            }
        }

        if (field.IsVolatile)
        {
            writer.Write(VolatileKeyword);
            writer.WriteSpace();
        }

        WriteTypeReference(field.Type, writer);
        writer.WriteSpace();
        writer.WriteReference(field, field.Name);

        if (field.HasConstantValue)
        {
            writer.WriteSpace();
            writer.Write(EqualsToken);
            writer.WriteSpace();
            WriteConstant(field.Type, field.ConstantValue, writer);
        }

        writer.Write(SemicolonToken);
    }

    private static void WriteEnumFieldDeclaration(IFieldSymbol field, SyntaxWriter writer)
    {
        WriteAttributeList(field.GetAttributes(), writer);
        writer.WriteReference(field, field.Name);
        writer.WriteSpace();
        writer.Write(EqualsToken);
        writer.WriteSpace();
        WriteEnum(field.ContainingType, field.ConstantValue, isForEnumDeclaration: true, writer);
    }

    private static void WriteMethodDeclaration(IMethodSymbol method, SyntaxWriter writer)
    {
        WriteAttributeList(method.GetAttributes(), writer);
        WriteAttributeList(method.GetReturnTypeAttributes(), writer, ReturnKeyword);

        if (method.MethodKind != MethodKind.Destructor &&
            method.ContainingType.TypeKind != TypeKind.Interface)
        {
            WriteAccessibility(method.DeclaredAccessibility, writer);
            writer.WriteSpace();
        }

        if (method.IsStatic)
        {
            writer.Write(StaticKeyword);
            writer.WriteSpace();
        }

        if (method.MethodKind != MethodKind.Destructor)
        {
            if (method.IsAbstract)
            {
                if (method.ContainingType.TypeKind != TypeKind.Interface)
                {
                    writer.Write(AbstractKeyword);
                    writer.WriteSpace();
                }
            }
            else if (method.IsVirtual)
            {
                writer.Write(VirtualKeyword);
                writer.WriteSpace();
            }
            else if (method.IsOverride)
            {
                writer.Write(OverrideKeyword);
                writer.WriteSpace();
            }

            if (method.IsSealed)
            {
                writer.Write(SealedKeyword);
                writer.WriteSpace();
            }
        }

        if (method.MethodKind == MethodKind.Constructor)
        {
            writer.WriteReference(method, method.ContainingType.Name);
        }
        else if (method.MethodKind == MethodKind.Destructor)
        {
            writer.Write(TildeToken);
            writer.WriteReference(method, method.ContainingType.Name);
        }
        else if (method.MethodKind == MethodKind.Conversion)
        {
            if (method.Name == "op_Explicit")
            {
                writer.Write(ExplicitKeyword);
            }
            else if (method.Name == "op_Implicit")
            {
                writer.Write(ImplicitKeyword);
            }

            writer.WriteSpace();
            writer.Write(OperatorKeyword);
            writer.WriteSpace();
            WriteTypeReference(method.ReturnType, writer);
        }
        else if (method.MethodKind == MethodKind.UserDefinedOperator)
        {
            WriteTypeReference(method.ReturnType, writer);
            writer.WriteSpace();

            var operatorKind = SyntaxFacts.GetOperatorKind(method.MetadataName);
            var operatorText = SyntaxFacts.GetText(operatorKind);
            writer.Write(OperatorKeyword);
            writer.WriteSpace();

            var tokenKind = MarkupFacts.GetTokenKind(operatorText);
            writer.Write(tokenKind);
        }
        else
        {
            if (method.RefKind == RefKind.Ref)
            {
                writer.Write(RefKeyword);
                writer.WriteSpace();
            }

            WriteTypeReference(method.ReturnType, writer);
            writer.WriteSpace();
            writer.WriteReference(method, method.Name);
        }

        WriteTypeParameterList(method.TypeParameters, writer);
        WriteParameterList(method.Parameters, writer);
        WriteConstraints(method.TypeParameters, writer);
        writer.Write(SemicolonToken);
    }

    private static void WritePropertyDeclaration(IPropertySymbol property, SyntaxWriter writer)
    {
        WriteAttributeList(property.GetAttributes(), writer);

        if (property.ContainingType.TypeKind != TypeKind.Interface)
        {
            WriteAccessibility(property.DeclaredAccessibility, writer);
            writer.WriteSpace();
        }

        if (property.IsStatic)
        {
            writer.Write(StaticKeyword);
            writer.WriteSpace();
        }

        if (property.IsAbstract)
        {
            if (property.ContainingType.TypeKind != TypeKind.Interface)
            {
                writer.Write(AbstractKeyword);
                writer.WriteSpace();
            }
        }
        else if (property.IsVirtual)
        {
            writer.Write(VirtualKeyword);
            writer.WriteSpace();
        }
        else if (property.IsOverride)
        {
            writer.Write(OverrideKeyword);
            writer.WriteSpace();
        }

        if (property.IsSealed)
        {
            writer.Write(SealedKeyword);
            writer.WriteSpace();
        }

        WriteTypeReference(property.Type, writer);
        writer.WriteSpace();

        if (!property.IsIndexer)
        {
            writer.WriteReference(property, property.Name);
        }
        else
        {
            writer.WriteReference(property, "this");
            writer.Write(OpenBracketToken);
            WriteParameters(property.Parameters, writer);
            writer.Write(CloseBracketToken);
        }

        var getterAttributes = property.GetMethod.GetCatalogAttributes();
        var setterAttributes = property.SetMethod.GetCatalogAttributes();
        var multipleLines = getterAttributes.Any() || setterAttributes.Any();

        if (!multipleLines)
        {
            writer.WriteSpace();
            writer.Write(OpenBraceToken);

            if (property.GetMethod is not null)
            {
                writer.WriteSpace();
                writer.Write(GetKeyword);
                writer.Write(SemicolonToken);
            }

            if (property.SetMethod is not null)
            {
                writer.WriteSpace();
                writer.Write(SetKeyword);
                writer.Write(SemicolonToken);
            }

            writer.WriteSpace();
            writer.Write(CloseBraceToken);
        }
        else
        {
            writer.WriteLine();
            writer.Write(OpenBraceToken);
            writer.WriteLine();
            writer.Indent++;

            if (property.GetMethod is not null)
            {
                WriteAttributeList(getterAttributes, writer);
                writer.Write(GetKeyword);
                writer.Write(SemicolonToken);
                writer.WriteLine();
            }

            if (property.SetMethod is not null)
            {
                WriteAttributeList(setterAttributes, writer);
                writer.Write(SetKeyword);
                writer.Write(SemicolonToken);
                writer.WriteLine();
            }

            writer.Indent--;
            writer.Write(CloseBraceToken);
            writer.WriteLine();
        }
    }

    private static void WriteEventDeclaration(IEventSymbol @event, SyntaxWriter writer)
    {
        WriteAttributeList(@event.GetAttributes(), writer);

        if (@event.ContainingType.TypeKind != TypeKind.Interface)
        {
            WriteAccessibility(@event.DeclaredAccessibility, writer);
            writer.WriteSpace();
        }

        if (@event.IsStatic)
        {
            writer.Write(StaticKeyword);
            writer.WriteSpace();
        }

        if (@event.IsAbstract)
        {
            if (@event.ContainingType.TypeKind != TypeKind.Interface)
            {
                writer.Write(AbstractKeyword);
                writer.WriteSpace();
            }
        }
        else if (@event.IsVirtual)
        {
            writer.Write(VirtualKeyword);
            writer.WriteSpace();
        }
        else if (@event.IsOverride)
        {
            writer.Write(OverrideKeyword);
            writer.WriteSpace();
        }

        if (@event.IsSealed)
        {
            writer.Write(SealedKeyword);
            writer.WriteSpace();
        }

        writer.Write(EventKeyword);
        writer.WriteSpace();
        WriteTypeReference(@event.Type, writer);
        writer.WriteSpace();
        writer.WriteReference(@event, @event.Name);

        var adderAttributes = @event.AddMethod.GetCatalogAttributes();
        var removerAttributes = @event.RemoveMethod.GetCatalogAttributes();
        var multipleLines = adderAttributes.Any() || removerAttributes.Any();

        if (!multipleLines)
        {
            writer.Write(SemicolonToken);
        }
        else
        {
            writer.WriteLine();
            writer.Write(OpenBraceToken);
            writer.WriteLine();
            writer.Indent++;

            if (@event.AddMethod is not null)
            {
                WriteAttributeList(adderAttributes, writer);
                writer.Write(AddKeyword);
                writer.Write(SemicolonToken);
                writer.WriteLine();
            }

            if (@event.RemoveMethod is not null)
            {
                WriteAttributeList(removerAttributes, writer);
                writer.Write(RemoveKeyword);
                writer.Write(SemicolonToken);
                writer.WriteLine();
            }

            writer.Indent--;
            writer.Write(CloseBraceToken);
            writer.WriteLine();
        }
    }

    private static void WriteAttributeList(ImmutableArray<AttributeData> attributes, SyntaxWriter writer, MarkupTokenKind target = None, bool compact = false)
    {
        var attributesWritten = false;

        foreach (var attribute in attributes.Ordered())
        {
            if (!attribute.IsIncludedInCatalog())
                continue;

            if (!compact)
            {
                writer.Write(OpenBracketToken);
            }
            else
            {
                if (attributesWritten)
                {
                    writer.Write(CommaToken);
                    writer.WriteSpace();
                }
                else
                {
                    writer.Write(OpenBracketToken);
                    attributesWritten = true;
                }
            }

            if (target != None)
            {
                writer.Write(target);
                writer.Write(ColonToken);
                writer.WriteSpace();
            }

            const string AttributeSuffix = "Attribute";
            var typeName = attribute.AttributeClass.Name;
            if (typeName.EndsWith(AttributeSuffix))
                typeName = typeName.Substring(0, typeName.Length - AttributeSuffix.Length);

            writer.WriteReference(attribute.AttributeConstructor, typeName);

            var hasArguments = attribute.ConstructorArguments.Any() ||
                               attribute.NamedArguments.Any();

            if (hasArguments)
                writer.Write(OpenParenToken);

            var isFirst = true;

            if (attribute.ConstructorArguments.Any())
            {
                foreach (var arg in attribute.ConstructorArguments)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        writer.Write(CommaToken);
                        writer.WriteSpace();
                    }

                    WriteTypedConstant(arg, writer);
                }
            }

            if (attribute.NamedArguments.Any())
            {
                foreach (var arg in attribute.NamedArguments.Ordered())
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        writer.Write(CommaToken);
                        writer.WriteSpace();
                    }

                    var propertyOrField = attribute.AttributeClass
                        .GetMembers(arg.Key)
                        .FirstOrDefault();

                    writer.WriteReference(propertyOrField, arg.Key);
                    writer.WriteSpace();
                    writer.Write(EqualsToken);
                    writer.WriteSpace();

                    WriteTypedConstant(arg.Value, writer);
                }
            }

            if (hasArguments)
                writer.Write(CloseParenToken);

            if (!compact)
            {
                writer.Write(CloseBracketToken);
                writer.WriteLine();
            }
        }

        if (compact && attributesWritten)
        {
            writer.Write(CloseBracketToken);
            writer.WriteSpace();
        }
    }

    private static void WriteTypedConstant(TypedConstant constant, SyntaxWriter writer)
    {
        if (constant.IsNull)
        {
            if (constant.Type?.IsValueType == true)
                writer.Write(DefaultKeyword);
            else
                writer.Write(NullKeyword);
        }
        else
        {
            switch (constant.Kind)
            {
                case TypedConstantKind.Type:
                    writer.Write(TypeofKeyword);
                    writer.Write(OpenParenToken);
                    WriteTypeReference((ITypeSymbol)constant.Value, writer);
                    writer.Write(CloseParenToken);
                    break;
                case TypedConstantKind.Array:
                    writer.Write(NewKeyword);
                    writer.Write(OpenBracketToken);
                    writer.Write(CloseBracketToken);
                    writer.WriteSpace();
                    writer.Write(OpenBraceToken);
                    writer.WriteSpace();
                    var isFirst = true;
                    foreach (var value in constant.Values)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                        }
                        else
                        {
                            writer.Write(CommaToken);
                            writer.WriteSpace();
                        }

                        WriteTypedConstant(value, writer);
                    }
                    writer.WriteSpace();
                    writer.Write(CloseBraceToken);
                    break;
                case TypedConstantKind.Enum:
                case TypedConstantKind.Primitive:
                    WriteConstant(constant.Type, constant.Value, writer);
                    break;
                case TypedConstantKind.Error:
                default:
                    throw new Exception($"Unexpected constant kind {constant.Kind}");
            }
        }
    }

    private static void WriteConstant(ITypeSymbol type, object value, SyntaxWriter writer)
    {
        if (value is null)
        {
            if (type.IsValueType)
                writer.Write(DefaultKeyword);
            else
                writer.Write(NullKeyword);
        }
        else if (type.TypeKind == TypeKind.Enum)
        {
            WriteEnum((INamedTypeSymbol)type, value, isForEnumDeclaration: false, writer);
        }
        else if (value is bool valueBool)
        {
            if (valueBool)
                writer.Write(TrueKeyword);
            else
                writer.Write(FalseKeyword);
        }
        else if (value is string valueString)
        {
            var text = SyntaxFactory.Literal(valueString).ToString();
            writer.WriteLiteralString(text);
        }
        else if (value is char valueChar)
        {
            var text = SyntaxFactory.Literal(valueChar).ToString();
            writer.WriteLiteralString(text);
        }
        else if (value is float valueSingle)
        {
            var text = SyntaxFactory.Literal(valueSingle).ToString();
            writer.WriteLiteralNumber(text);
        }
        else if (value is double valueDouble)
        {
            var text = SyntaxFactory.Literal(valueDouble).ToString();
            writer.WriteLiteralNumber(text);
        }
        else if (value is decimal valueDecimal)
        {
            var text = SyntaxFactory.Literal(valueDecimal).ToString();
            writer.WriteLiteralNumber(text);
        }
        else if (value is byte valueByte)
        {
            var text = SyntaxFactory.Literal(valueByte).ToString();
            writer.WriteLiteralNumber(text);
        }
        else if (value is sbyte valueSByte)
        {
            var text = SyntaxFactory.Literal(valueSByte).ToString();
            writer.WriteLiteralNumber(text);
        }
        else if (value is short valueInt16)
        {
            var text = SyntaxFactory.Literal(valueInt16).ToString();
            writer.WriteLiteralNumber(text);
        }
        else if (value is ushort valueUInt16)
        {
            var text = SyntaxFactory.Literal(valueUInt16).ToString();
            writer.WriteLiteralNumber(text);
        }
        else if (value is int valueInt32)
        {
            var text = SyntaxFactory.Literal(valueInt32).ToString();
            writer.WriteLiteralNumber(text);
        }
        else if (value is uint valueUInt32)
        {
            var text = SyntaxFactory.Literal(valueUInt32).ToString();
            writer.WriteLiteralNumber(text);
        }
        else if (value is long valueInt64)
        {
            var text = SyntaxFactory.Literal(valueInt64).ToString();
            writer.WriteLiteralNumber(text);
        }
        else if (value is ulong valueUInt64)
        {
            var text = SyntaxFactory.Literal(valueUInt64).ToString();
            writer.WriteLiteralNumber(text);
        }
        else
        {
            throw new Exception($"Unexpected primitive type {type}");
        }
    }

    private static void WriteAccessibility(Accessibility accessibility, SyntaxWriter writer)
    {
        switch (accessibility)
        {
            case Accessibility.Private:
                writer.Write(PrivateKeyword);
                break;
            case Accessibility.ProtectedAndInternal:
                writer.Write(PrivateKeyword);
                writer.WriteSpace();
                writer.Write(ProtectedKeyword);
                break;
            case Accessibility.Protected:
                writer.Write(ProtectedKeyword);
                break;
            case Accessibility.Internal:
                writer.Write(InternalKeyword);
                break;
            case Accessibility.ProtectedOrInternal:
                writer.Write(ProtectedKeyword);
                writer.WriteSpace();
                writer.Write(InternalKeyword);
                break;
            case Accessibility.Public:
                writer.Write(PublicKeyword);
                break;
            default:
                throw new Exception($"Unexpected accessibility: {accessibility}");
        }
    }

    private static void WriteTypeParameterList(ImmutableArray<ITypeParameterSymbol> typeParameters, SyntaxWriter writer)
    {
        if (!typeParameters.Any())
            return;

        writer.Write(LessThanToken);

        var isFirst = true;

        foreach (var typeParameter in typeParameters)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                writer.Write(CommaToken);
                writer.WriteSpace();
            }

            if (typeParameter.Variance == VarianceKind.In)
            {
                writer.Write(InKeyword);
                writer.WriteSpace();
            }
            else if (typeParameter.Variance == VarianceKind.Out)
            {
                writer.Write(OutKeyword);
                writer.WriteSpace();
            }

            WriteTypeReference(typeParameter, writer);
        }

        writer.Write(GreaterThanToken);
    }

    private static void WriteConstraints(ImmutableArray<ITypeParameterSymbol> typeParameters, SyntaxWriter writer)
    {
        if (!typeParameters.Any())
            return;

        static void WriteConstructorConstraint(SyntaxWriter writer)
        {
            writer.Write(NewKeyword);
            writer.Write(OpenParenToken);
            writer.Write(CloseParenToken);
        }

        static void WriteNotNullConstraint(SyntaxWriter writer)
        {
            writer.Write(NotnullKeyword);
        }

        static void WriteReferenceTypeConstraint(SyntaxWriter writer)
        {
            writer.Write(ClassKeyword);
        }

        static void WriteUnmanagedTypeConstraint(SyntaxWriter writer)
        {
            writer.Write(UnmanagedKeyword);
        }

        static void WriteValueTypeConstraint(SyntaxWriter writer)
        {
            writer.Write(StructKeyword);
        }

        var constraintBuilders = new List<Action<SyntaxWriter>>(5);

        foreach (var typeParameter in typeParameters)
        {
            constraintBuilders.Clear();

            if (typeParameter.HasConstructorConstraint)
                constraintBuilders.Add(WriteConstructorConstraint);

            if (typeParameter.HasNotNullConstraint)
                constraintBuilders.Add(WriteNotNullConstraint);

            if (typeParameter.HasReferenceTypeConstraint)
                constraintBuilders.Add(WriteReferenceTypeConstraint);

            if (typeParameter.HasUnmanagedTypeConstraint)
                constraintBuilders.Add(WriteUnmanagedTypeConstraint);

            if (typeParameter.HasValueTypeConstraint)
                constraintBuilders.Add(WriteValueTypeConstraint);

            var hasAnyContraints =
                typeParameter.ConstraintTypes.Any() ||
                constraintBuilders.Any();

            if (!hasAnyContraints)
                continue;

            writer.WriteLine();
            writer.Indent++;
            writer.Write(WhereKeyword);
            writer.WriteSpace();
            writer.WriteReference(typeParameter, typeParameter.Name);
            writer.Write(CommaToken);
            writer.WriteSpace();

            for (var i = 0; i < constraintBuilders.Count; i++)
            {
                if (i > 0)
                {
                    writer.Write(CommaToken);
                    writer.WriteSpace();
                }

                constraintBuilders[i](writer);
            }

            if (typeParameter.ConstraintTypes.Any())
            {
                var needsComma = constraintBuilders.Any();

                foreach (var constraintType in typeParameter.ConstraintTypes.Ordered())
                {
                    if (needsComma)
                    {
                        writer.Write(CommaToken);
                        writer.WriteSpace();
                    }
                    else
                    {
                        needsComma = true;
                    }

                    WriteTypeReference(constraintType, writer);
                }
            }

            writer.Indent--;
        }
    }

    private static void WriteTypeReference(ITypeSymbol type, SyntaxWriter writer)
    {
        switch (type.TypeKind)
        {
            case TypeKind.Class:
            case TypeKind.Delegate:
            case TypeKind.Enum:
            case TypeKind.Interface:
            case TypeKind.Struct:
                var namedType = (INamedTypeSymbol)type;
                if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    WriteTypeReference(namedType.TypeArguments[0], writer);
                    writer.Write(QuestionToken);
                }
                else if (namedType.IsTupleType)
                {
                    writer.Write(OpenParenToken);
                    var isFirst = true;
                    foreach (var field in namedType.TupleElements)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                        }
                        else
                        {
                            writer.Write(CommaToken);
                            writer.WriteSpace();
                        }

                        WriteTypeReference(field.Type, writer);

                        if (field.IsExplicitlyNamedTupleElement)
                        {
                            writer.WriteSpace();
                            writer.WriteReference(field, field.Name);
                        }
                    }
                    writer.Write(CloseParenToken);
                }
                else
                {
                    switch (namedType.SpecialType)
                    {
                        case SpecialType.System_Object:
                            writer.Write(ObjectKeyword);
                            break;
                        case SpecialType.System_Void:
                            writer.Write(VoidKeyword);
                            break;
                        case SpecialType.System_Boolean:
                            writer.Write(BoolKeyword);
                            break;
                        case SpecialType.System_Char:
                            writer.Write(CharKeyword);
                            break;
                        case SpecialType.System_SByte:
                            writer.Write(SbyteKeyword);
                            break;
                        case SpecialType.System_Byte:
                            writer.Write(ByteKeyword);
                            break;
                        case SpecialType.System_Int16:
                            writer.Write(ShortKeyword);
                            break;
                        case SpecialType.System_UInt16:
                            writer.Write(UshortKeyword);
                            break;
                        case SpecialType.System_Int32:
                            writer.Write(IntKeyword);
                            break;
                        case SpecialType.System_UInt32:
                            writer.Write(UintKeyword);
                            break;
                        case SpecialType.System_Int64:
                            writer.Write(LongKeyword);
                            break;
                        case SpecialType.System_UInt64:
                            writer.Write(UlongKeyword);
                            break;
                        case SpecialType.System_Decimal:
                            writer.Write(DecimalKeyword);
                            break;
                        case SpecialType.System_Single:
                            writer.Write(FloatKeyword);
                            break;
                        case SpecialType.System_Double:
                            writer.Write(DoubleKeyword);
                            break;
                        case SpecialType.System_String:
                            writer.Write(StringKeyword);
                            break;
                        case SpecialType.None:
                        case SpecialType.System_ArgIterator:
                        case SpecialType.System_Array:
                        case SpecialType.System_AsyncCallback:
                        case SpecialType.System_Collections_Generic_ICollection_T:
                        case SpecialType.System_Collections_Generic_IEnumerable_T:
                        case SpecialType.System_Collections_Generic_IEnumerator_T:
                        case SpecialType.System_Collections_Generic_IList_T:
                        case SpecialType.System_Collections_Generic_IReadOnlyCollection_T:
                        case SpecialType.System_Collections_Generic_IReadOnlyList_T:
                        case SpecialType.System_Collections_IEnumerable:
                        case SpecialType.System_Collections_IEnumerator:
                        case SpecialType.System_DateTime:
                        case SpecialType.System_Delegate:
                        case SpecialType.System_Enum:
                        case SpecialType.System_IAsyncResult:
                        case SpecialType.System_IDisposable:
                        case SpecialType.System_IntPtr:
                        case SpecialType.System_MulticastDelegate:
                        case SpecialType.System_Nullable_T:
                        case SpecialType.System_Runtime_CompilerServices_IsVolatile:
                        case SpecialType.System_Runtime_CompilerServices_PreserveBaseOverridesAttribute:
                        case SpecialType.System_Runtime_CompilerServices_RuntimeFeature:
                        case SpecialType.System_RuntimeArgumentHandle:
                        case SpecialType.System_RuntimeFieldHandle:
                        case SpecialType.System_RuntimeMethodHandle:
                        case SpecialType.System_RuntimeTypeHandle:
                        case SpecialType.System_TypedReference:
                        case SpecialType.System_UIntPtr:
                        case SpecialType.System_ValueType:
                        default:
                            writer.WriteReference(namedType, namedType.Name);
                            WriteTypeParameterReferences(namedType.TypeArguments, writer);
                            break;
                    }
                }
                break;
            case TypeKind.TypeParameter:
                var typeParameter = (ITypeParameterSymbol)type;
                writer.WriteReference(typeParameter, typeParameter.Name);
                break;
            case TypeKind.Array:
                var array = (IArrayTypeSymbol)type;
                WriteTypeReference(array.ElementType, writer);
                writer.Write(OpenBracketToken);
                for (var i = 1; i < array.Rank; i++)
                    writer.Write(CommaToken);
                writer.Write(CloseBracketToken);
                break;
            case TypeKind.Pointer:
                var ptr = (IPointerTypeSymbol)type;
                WriteTypeReference(ptr.PointedAtType, writer);
                writer.Write(AsteriskToken);
                break;
            case TypeKind.Dynamic:
                writer.Write(DynamicKeyword);
                break;
            case TypeKind.FunctionPointer:
            {
                var fp = (IFunctionPointerTypeSymbol)type;
                writer.Write(DelegateKeyword);
                writer.Write(AsteriskToken);

                if (fp.Signature.CallingConvention != SignatureCallingConvention.Default)
                {
                    writer.WriteSpace();

                    switch (fp.Signature.CallingConvention)
                    {
                        case SignatureCallingConvention.Unmanaged:
                            writer.Write(UnmanagedKeyword);
                            break;
                        case SignatureCallingConvention.CDecl:
                            WriteCallingConvention(CdeclKeyword, writer);
                            break;
                        case SignatureCallingConvention.StdCall:
                            WriteCallingConvention(StdcallKeyword, writer);
                            break;
                        case SignatureCallingConvention.ThisCall:
                            WriteCallingConvention(ThiscallKeyword, writer);
                            break;
                        case SignatureCallingConvention.FastCall:
                            WriteCallingConvention(FastcallKeyword, writer);
                            break;
                        default:
                            throw new Exception($"Unexpected calling convention: {fp.Signature.CallingConvention}");
                    }

                    static void WriteCallingConvention(MarkupTokenKind convention, SyntaxWriter writer)
                    {
                        writer.Write(UnmanagedKeyword);
                        writer.Write(OpenBracketToken);
                        writer.Write(convention);
                        writer.Write(CloseBracketToken);
                    }
                }

                writer.Write(LessThanToken);

                var isFirst = true;
                foreach (var p in fp.Signature.Parameters)
                {
                    WriteSeparator(ref isFirst, writer);
                    WriteTypeReference(p.Type, writer);
                }

                WriteSeparator(ref isFirst, writer);
                WriteTypeReference(fp.Signature.ReturnType, writer);

                static void WriteSeparator(ref bool isFirst, SyntaxWriter writer)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        writer.Write(CommaToken);
                        writer.WriteSpace();
                    }
                }

                writer.Write(GreaterThanToken);
                break;
            }
            case TypeKind.Error:
                writer.Write(QuestionToken);
                break;
            case TypeKind.Unknown:
            case TypeKind.Module:
            case TypeKind.Submission:
            default:
                throw new Exception($"Unexpected type kind: {type.TypeKind}");
        }

        if (type.IsReferenceType)
        {
            if (type.NullableAnnotation == NullableAnnotation.Annotated)
            {
                writer.Write(QuestionToken);
            }
            else if (type.NullableAnnotation == NullableAnnotation.NotAnnotated)
            {
                writer.Write(ExclamationToken);
            }
        }
    }

    private static void WriteTypeParameterReferences(ImmutableArray<ITypeSymbol> typeParameters, SyntaxWriter writer)
    {
        if (!typeParameters.Any())
            return;

        writer.Write(LessThanToken);

        for (var i = 0; i < typeParameters.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(CommaToken);
                writer.WriteSpace();
            }

            WriteTypeReference(typeParameters[i], writer);
        }

        writer.Write(GreaterThanToken);
    }

    private static void WriteParameterList(ImmutableArray<IParameterSymbol> parameters, SyntaxWriter writer)
    {
        writer.Write(OpenParenToken);
        WriteParameters(parameters, writer);
        writer.Write(CloseParenToken);
    }

    private static void WriteParameters(ImmutableArray<IParameterSymbol> parameters, SyntaxWriter writer)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(CommaToken);
                writer.WriteSpace();
            }

            var parameter = parameters[i];
            var method = parameter.ContainingSymbol as IMethodSymbol;
            WriteAttributeList(parameter.GetAttributes(), writer, compact: true);

            if (parameter.IsParams)
            {
                writer.Write(ParamsKeyword);
                writer.WriteSpace();
            }

            if (i == 0 && method?.IsExtensionMethod == true)
            {
                writer.Write(ThisKeyword);
                writer.WriteSpace();
            }

            if (parameter.RefKind == RefKind.In)
            {
                writer.Write(InKeyword);
                writer.WriteSpace();
            }
            else if (parameter.RefKind == RefKind.Ref)
            {
                writer.Write(RefKeyword);
                writer.WriteSpace();
            }
            else if (parameter.RefKind == RefKind.RefReadOnly)
            {
                writer.Write(RefKeyword);
                writer.WriteSpace();
                writer.Write(ReadonlyKeyword);
                writer.WriteSpace();
            }
            else if (parameter.RefKind == RefKind.Out)
            {
                writer.Write(OutKeyword);
                writer.WriteSpace();
            }

            WriteTypeReference(parameter.Type, writer);
            writer.WriteSpace();
            writer.WriteReference(parameter, parameter.Name);

            if (parameter.HasExplicitDefaultValue)
            {
                writer.WriteSpace();
                writer.Write(EqualsToken);
                writer.WriteSpace();
                WriteConstant(parameter.Type, parameter.ExplicitDefaultValue, writer);
            }
        }
    }

    private static void WriteEnum(INamedTypeSymbol enumType, object constantValue, bool isForEnumDeclaration, SyntaxWriter writer)
    {
        if (IsFlagsEnum(enumType))
            WriteFlagsEnumConstantValue(enumType, constantValue, isForEnumDeclaration, writer);
        else if (isForEnumDeclaration)
            WriteConstant(enumType.EnumUnderlyingType, constantValue, writer);
        else
            WriteNonFlagsEnumConstantValue(enumType, constantValue, writer);
    }

    private static bool IsFlagsEnum(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.TypeKind != TypeKind.Enum)
            return false;

        foreach (var attribute in typeSymbol.GetAttributes())
        {
            if (attribute.AttributeClass is null || attribute.AttributeConstructor is null)
                continue;

            if (attribute.AttributeConstructor.Parameters.Any() || attribute.AttributeClass.Name != "FlagsAttribute")
                continue;

            var containingSymbol = attribute.AttributeClass.ContainingSymbol;

            if (containingSymbol.Kind == SymbolKind.Namespace &&
                containingSymbol.Name == "System" &&
                ((INamespaceSymbol)containingSymbol.ContainingSymbol).IsGlobalNamespace)
            {
                return true;
            }
        }

        return false;
    }

    private static void WriteFlagsEnumConstantValue(INamedTypeSymbol enumType,
                                                    object constantValue,
                                                    bool isForEnumDeclaration,
                                                    SyntaxWriter writer)
    {
        var allFieldsAndValues = new List<EnumField>();
        GetSortedEnumFields(enumType, allFieldsAndValues);

        var usedFieldsAndValues = new List<EnumField>();
        WriteFlagsEnumConstantValue(enumType, constantValue, isForEnumDeclaration, allFieldsAndValues, usedFieldsAndValues, writer);
    }

    private static void WriteFlagsEnumConstantValue(INamedTypeSymbol enumType,
                                                    object constantValue,
                                                    bool isForEnumDeclaration,
                                                    List<EnumField> allFieldsAndValues,
                                                    List<EnumField> usedFieldsAndValues,
                                                    SyntaxWriter writer)
    {
        var underlyingSpecialType = enumType.EnumUnderlyingType!.SpecialType;
        var constantValueULong = ConvertEnumUnderlyingTypeToUInt64(constantValue, underlyingSpecialType);

        var result = constantValueULong;

        if (result != 0)
        {
            foreach (var fieldAndValue in allFieldsAndValues)
            {
                var valueAtIndex = fieldAndValue.Value;

                if (isForEnumDeclaration && valueAtIndex == constantValueULong)
                    continue;

                if (valueAtIndex != 0 && (result & valueAtIndex) == valueAtIndex)
                {
                    usedFieldsAndValues.Add(fieldAndValue);
                    result -= valueAtIndex;
                    if (result == 0) break;
                }
            }
        }

        if (result == 0 && usedFieldsAndValues.Count > 0)
        {
            for (var i = usedFieldsAndValues.Count - 1; i >= 0; i--)
            {
                if (i != (usedFieldsAndValues.Count - 1))
                {
                    writer.WriteSpace();
                    writer.Write(BarToken);
                    writer.WriteSpace();
                }

                WriteEnumFieldReference((IFieldSymbol)usedFieldsAndValues[i].IdentityOpt, isForEnumDeclaration, writer);
            }
        }
        else
        {
            if (isForEnumDeclaration)
            {
                WriteConstant(enumType.EnumUnderlyingType, constantValue, writer);
                return;
            }

            var zeroField = constantValueULong == 0
                ? EnumField.FindValue(allFieldsAndValues, 0)
                : default;
            if (!zeroField.IsDefault)
                WriteEnumFieldReference((IFieldSymbol)zeroField.IdentityOpt, isForEnumDeclaration: false, writer);
            else
                WriteExplicitlyCastedLiteralValue(enumType, constantValue, writer);
        }
    }

    private static void WriteNonFlagsEnumConstantValue(INamedTypeSymbol enumType, object constantValue, SyntaxWriter writer)
    {
        var underlyingSpecialType = enumType.EnumUnderlyingType!.SpecialType;
        var constantValueULong = ConvertEnumUnderlyingTypeToUInt64(constantValue, underlyingSpecialType);

        var enumFields = new List<EnumField>();
        GetSortedEnumFields(enumType, enumFields);

        var match = EnumField.FindValue(enumFields, constantValueULong);

        if (!match.IsDefault)
            WriteEnumFieldReference((IFieldSymbol)match.IdentityOpt, isForEnumDeclaration: false, writer);
        else
            WriteExplicitlyCastedLiteralValue(enumType, constantValue, writer);
    }

    private static void WriteEnumFieldReference(IFieldSymbol symbol, bool isForEnumDeclaration, SyntaxWriter writer)
    {
        if (!isForEnumDeclaration)
        {
            writer.WriteReference(symbol.ContainingType, symbol.ContainingType.Name);
            writer.Write(DotToken);
        }

        writer.WriteReference(symbol, symbol.Name);
    }

    private static void WriteExplicitlyCastedLiteralValue(INamedTypeSymbol enumType,
                                                          object constantValue,
                                                          SyntaxWriter writer)
    {
        writer.Write(OpenParenToken);
        WriteTypeReference(enumType, writer);
        writer.Write(CloseParenToken);
        WriteConstant(enumType.EnumUnderlyingType!, constantValue, writer);
    }

    private static void GetSortedEnumFields(INamedTypeSymbol enumType, List<EnumField> enumFields)
    {
        var underlyingSpecialType = enumType.EnumUnderlyingType!.SpecialType;
        foreach (var member in enumType.GetMembers())
        {
            if (member.Kind == SymbolKind.Field)
            {
                var field = (IFieldSymbol)member;
                if (field.HasConstantValue)
                {
                    var enumField = new EnumField(field.Name, ConvertEnumUnderlyingTypeToUInt64(field.ConstantValue, underlyingSpecialType), field);
                    enumFields.Add(enumField);
                }
            }
        }

        enumFields.Sort(EnumField.Comparer);
    }

    private static ulong ConvertEnumUnderlyingTypeToUInt64(object value, SpecialType specialType)
    {
        unchecked
        {
            return specialType switch
            {
                SpecialType.System_SByte => (ulong)(sbyte)value,
                SpecialType.System_Int16 => (ulong)(short)value,
                SpecialType.System_Int32 => (ulong)(int)value,
                SpecialType.System_Int64 => (ulong)(long)value,
                SpecialType.System_Byte => (byte)value,
                SpecialType.System_UInt16 => (ushort)value,
                SpecialType.System_UInt32 => (uint)value,
                SpecialType.System_UInt64 => (ulong)value,
                _ => throw new InvalidOperationException($"{specialType} is not a valid underlying type for an enum"),
            };
        }
    }

    private readonly struct EnumField
    {
        public static readonly IComparer<EnumField> Comparer = new EnumFieldComparer();

        public readonly string Name;
        public readonly ulong Value;
        public readonly object IdentityOpt;

        public EnumField(string name, ulong value, object identityOpt = null)
        {
            this.Name = name;
            this.Value = value;
            this.IdentityOpt = identityOpt;
        }

        public bool IsDefault
        {
            get { return this.Name is null; }
        }

        public override string ToString()
        {
            return $"{this.Name} = {this.Value}";
        }

        public static EnumField FindValue(List<EnumField> sortedFields, ulong value)
        {
            int start = 0;
            int end = sortedFields.Count;

            while (start < end)
            {
                int mid = start + (end - start) / 2;

                long diff = unchecked((long)value - (long)sortedFields[mid].Value); // NOTE: Has to match the comparer below.

                if (diff == 0)
                {
                    while (mid >= start && sortedFields[mid].Value == value)
                    {
                        mid--;
                    }
                    return sortedFields[mid + 1];
                }
                else if (diff > 0)
                {
                    end = mid; // Exclude mid.
                }
                else
                {
                    start = mid + 1; // Exclude mid.
                }
            }

            return default(EnumField);
        }

        private sealed class EnumFieldComparer : IComparer<EnumField>
        {
            int IComparer<EnumField>.Compare(EnumField field1, EnumField field2)
            {
                // Sort order is descending value, then ascending name.
                int diff = unchecked(((long)field2.Value).CompareTo((long)field1.Value));
                return diff == 0
                    ? string.CompareOrdinal(field1.Name, field2.Name)
                    : diff;
            }
        }
    }
}