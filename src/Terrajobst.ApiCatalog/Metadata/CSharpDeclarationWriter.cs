using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
        writer.WriteKeyword("namespace");
        writer.WriteSpace();

        var parents = new Stack<INamespaceSymbol>();
        var c = @namespace;
        while (c != null && !c.IsGlobalNamespace)
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
                writer.WritePunctuation(".");

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
            writer.WriteKeyword("static");
            writer.WriteSpace();
        }
        else if (type.IsAbstract)
        {
            writer.WriteKeyword("abstract");
            writer.WriteSpace();
        }
        else if (type.IsSealed)
        {
            writer.WriteKeyword("sealed");
            writer.WriteSpace();
        }

        if (type.IsReadOnly)
        {
            writer.WriteKeyword("readonly");
            writer.WriteSpace();
        }

        writer.WriteKeyword("class");
        writer.WriteSpace();

        writer.WriteReference(type, type.Name);

        WriteTypeParameterList(type.TypeParameters, writer);

        var hasBaseType = type.BaseType != null &&
                          type.BaseType.SpecialType != SpecialType.System_Object;

        var implementsInterfaces = type.Interfaces.Where(t => t.IsIncludedInCatalog()).Any();

        if (hasBaseType || implementsInterfaces)
        {
            writer.WriteSpace();
            writer.WritePunctuation(":");

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
                        writer.WritePunctuation(",");

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
            writer.WriteKeyword("readonly");
            writer.WriteSpace();
        }

        writer.WriteKeyword("struct");
        writer.WriteSpace();
        writer.WriteReference(type, type.Name);

        WriteTypeParameterList(type.TypeParameters, writer);

        if (type.Interfaces.Where(t => t.IsIncludedInCatalog()).Any())
        {
            writer.WriteSpace();
            writer.WritePunctuation(":");

            var isFirst = true;

            foreach (var @interface in type.Interfaces.Where(t => t.IsIncludedInCatalog()).Ordered())
            {
                if (isFirst)
                    isFirst = false;
                else
                    writer.WritePunctuation(",");

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
        writer.WriteKeyword("interface");
        writer.WriteSpace();
        writer.WriteReference(type, type.Name);

        WriteTypeParameterList(type.TypeParameters, writer);

        if (type.Interfaces.Where(t => t.IsIncludedInCatalog()).Any())
        {
            writer.WriteSpace();
            writer.WritePunctuation(":");

            var isFirst = true;

            foreach (var @interface in type.Interfaces.Where(t => t.IsIncludedInCatalog()).Ordered())
            {
                if (isFirst)
                    isFirst = false;
                else
                    writer.WritePunctuation(",");

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
        writer.WriteKeyword("delegate");
        writer.WriteSpace();

        var invokeMethod = (IMethodSymbol)type.GetMembers("Invoke")
            .First();

        WriteTypeReference(invokeMethod.ReturnType, writer);
        writer.WriteSpace();
        writer.WriteReference(type, type.Name);
        WriteParameterList(invokeMethod.Parameters, writer);

        WriteConstraints(type.TypeParameters, writer);

        writer.WritePunctuation(";");
    }

    private static void WriteEnumDeclaration(INamedTypeSymbol type, SyntaxWriter writer)
    {
        WriteAttributeList(type.GetAttributes(), writer);
        WriteAccessibility(type.DeclaredAccessibility, writer);

        writer.WriteSpace();
        writer.WriteKeyword("enum");
        writer.WriteSpace();

        writer.WriteReference(type, type.Name);

        if (type.EnumUnderlyingType != null && type.EnumUnderlyingType.SpecialType != SpecialType.System_Int32)
        {
            writer.WriteSpace();
            writer.WritePunctuation(":");
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
            writer.WriteKeyword("const");
            writer.WriteSpace();
        }
        else
        {
            if (field.IsStatic)
            {
                writer.WriteKeyword("static");
                writer.WriteSpace();
            }
            if (field.IsReadOnly)
            {
                writer.WriteKeyword("readonly");
                writer.WriteSpace();
            }
        }

        if (field.IsVolatile)
        {
            writer.WriteKeyword("volatile");
            writer.WriteSpace();
        }

        WriteTypeReference(field.Type, writer);
        writer.WriteSpace();
        writer.WriteReference(field, field.Name);

        if (field.HasConstantValue)
        {
            writer.WriteSpace();
            writer.WritePunctuation("=");
            writer.WriteSpace();
            WriteConstant(field.Type, field.ConstantValue, writer);
        }

        writer.WritePunctuation(";");
    }

    private static void WriteEnumFieldDeclaration(IFieldSymbol field, SyntaxWriter writer)
    {
        WriteAttributeList(field.GetAttributes(), writer);
        writer.WriteReference(field, field.Name);
        writer.WriteSpace();
        writer.WritePunctuation("=");
        writer.WriteSpace();
        WriteEnum(field.ContainingType, field.ConstantValue, isForEnumDeclaration: true, writer);
    }

    private static void WriteMethodDeclaration(IMethodSymbol method, SyntaxWriter writer)
    {
        WriteAttributeList(method.GetAttributes(), writer);
        WriteAttributeList(method.GetReturnTypeAttributes(), writer, "return");

        if (method.MethodKind != MethodKind.Destructor &&
            method.ContainingType.TypeKind != TypeKind.Interface)
        {
            WriteAccessibility(method.DeclaredAccessibility, writer);
            writer.WriteSpace();
        }

        if (method.IsStatic)
        {
            writer.WriteKeyword("static");
            writer.WriteSpace();
        }

        if (method.MethodKind != MethodKind.Destructor)
        {
            if (method.IsAbstract)
            {
                if (method.ContainingType.TypeKind != TypeKind.Interface)
                {
                    writer.WriteKeyword("abstract");
                    writer.WriteSpace();
                }
            }
            else if (method.IsVirtual)
            {
                writer.WriteKeyword("virtual");
                writer.WriteSpace();
            }
            else if (method.IsOverride)
            {
                writer.WriteKeyword("override");
                writer.WriteSpace();
            }

            if (method.IsSealed)
            {
                writer.WriteKeyword("sealed");
                writer.WriteSpace();
            }
        }

        if (method.MethodKind == MethodKind.Constructor)
        {
            writer.WriteReference(method, method.ContainingType.Name);
        }
        else if (method.MethodKind == MethodKind.Destructor)
        {
            writer.WritePunctuation("~");
            writer.WriteReference(method, method.ContainingType.Name);
        }
        else if (method.MethodKind == MethodKind.Conversion)
        {
            if (method.Name == "op_Explicit")
            {
                writer.WriteKeyword("explicit");
            }
            else if (method.Name == "op_Implicit")
            {
                writer.WriteKeyword("implicit");
            }

            writer.WriteSpace();
            writer.WriteKeyword("operator");
            writer.WriteSpace();
            WriteTypeReference(method.ReturnType, writer);
        }
        else if (method.MethodKind == MethodKind.UserDefinedOperator)
        {
            WriteTypeReference(method.ReturnType, writer);
            writer.WriteSpace();

            var operatorKind = SyntaxFacts.GetOperatorKind(method.MetadataName);
            var isKeyword = SyntaxFacts.IsKeywordKind(operatorKind);
            var operatorText = SyntaxFacts.GetText(operatorKind);
            writer.WriteKeyword("operator");
            writer.WriteSpace();

            if (isKeyword)
                writer.WriteKeyword(operatorText);
            else
                writer.WritePunctuation(operatorText);
        }
        else
        {
            if (method.RefKind == RefKind.Ref)
            {
                writer.WriteKeyword("ref");
                writer.WriteSpace();
            }

            WriteTypeReference(method.ReturnType, writer);
            writer.WriteSpace();
            writer.WriteReference(method, method.Name);
        }

        WriteTypeParameterList(method.TypeParameters, writer);
        WriteParameterList(method.Parameters, writer);
        WriteConstraints(method.TypeParameters, writer);
        writer.WritePunctuation(";");
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
            writer.WriteKeyword("static");
            writer.WriteSpace();
        }

        if (property.IsAbstract)
        {
            if (property.ContainingType.TypeKind != TypeKind.Interface)
            {
                writer.WriteKeyword("abstract");
                writer.WriteSpace();
            }
        }
        else if (property.IsVirtual)
        {
            writer.WriteKeyword("virtual");
            writer.WriteSpace();
        }
        else if (property.IsOverride)
        {
            writer.WriteKeyword("override");
            writer.WriteSpace();
        }

        if (property.IsSealed)
        {
            writer.WriteKeyword("sealed");
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
            writer.WritePunctuation("[");
            WriteParameters(property.Parameters, writer);
            writer.WritePunctuation("]");
        }

        var getterAttributes = property.GetMethod.GetCatalogAttributes();
        var setterAttributes = property.SetMethod.GetCatalogAttributes();
        var multipleLines = getterAttributes.Any() || setterAttributes.Any();

        if (!multipleLines)
        {
            writer.WriteSpace();
            writer.WritePunctuation("{");

            if (property.GetMethod != null)
            {
                writer.WriteSpace();
                writer.WriteKeyword("get");
                writer.WritePunctuation(";");
            }

            if (property.SetMethod != null)
            {
                writer.WriteSpace();
                writer.WriteKeyword("set");
                writer.WritePunctuation(";");
            }

            writer.WriteSpace();
            writer.WritePunctuation("}");
        }
        else
        {
            writer.WriteLine();
            writer.WritePunctuation("{");
            writer.WriteLine();
            writer.Indent++;

            if (property.GetMethod != null)
            {
                WriteAttributeList(getterAttributes, writer);
                writer.WriteKeyword("get");
                writer.WritePunctuation(";");
                writer.WriteLine();
            }

            if (property.SetMethod != null)
            {
                WriteAttributeList(setterAttributes, writer);
                writer.WriteKeyword("set");
                writer.WritePunctuation(";");
                writer.WriteLine();
            }

            writer.Indent--;
            writer.WritePunctuation("}");
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
            writer.WriteKeyword("static");
            writer.WriteSpace();
        }

        if (@event.IsAbstract)
        {
            if (@event.ContainingType.TypeKind != TypeKind.Interface)
            {
                writer.WriteKeyword("abstract");
                writer.WriteSpace();
            }
        }
        else if (@event.IsVirtual)
        {
            writer.WriteKeyword("virtual");
            writer.WriteSpace();
        }
        else if (@event.IsOverride)
        {
            writer.WriteKeyword("override");
            writer.WriteSpace();
        }

        if (@event.IsSealed)
        {
            writer.WriteKeyword("sealed");
            writer.WriteSpace();
        }

        writer.WriteKeyword("event");
        writer.WriteSpace();
        WriteTypeReference(@event.Type, writer);
        writer.WriteSpace();
        writer.WriteReference(@event, @event.Name);

        var adderAttributes = @event.AddMethod.GetCatalogAttributes();
        var removerAttributes = @event.RemoveMethod.GetCatalogAttributes();
        var multipleLines = adderAttributes.Any() || removerAttributes.Any();

        if (!multipleLines)
        {
            writer.WritePunctuation(";");
        }
        else
        {
            writer.WriteLine();
            writer.WritePunctuation("{");
            writer.WriteLine();
            writer.Indent++;

            if (@event.AddMethod != null)
            {
                WriteAttributeList(adderAttributes, writer);
                writer.WriteKeyword("add");
                writer.WritePunctuation(";");
                writer.WriteLine();
            }

            if (@event.RemoveMethod != null)
            {
                WriteAttributeList(removerAttributes, writer);
                writer.WriteKeyword("remove");
                writer.WritePunctuation(";");
                writer.WriteLine();
            }

            writer.Indent--;
            writer.WritePunctuation("}");
            writer.WriteLine();
        }
    }

    private static void WriteAttributeList(ImmutableArray<AttributeData> attributes, SyntaxWriter writer, string target = null, bool compact = false)
    {
        var attributesWritten = false;

        foreach (var attribute in attributes.Ordered())
        {
            if (!attribute.IsIncludedInCatalog())
                continue;

            if (!compact)
            {
                writer.WritePunctuation("[");
            }
            else
            {
                if (attributesWritten)
                {
                    writer.WritePunctuation(",");
                    writer.WriteSpace();
                }
                else
                {
                    writer.WritePunctuation("[");
                    attributesWritten = true;
                }
            }

            if (target != null)
            {
                writer.WriteKeyword(target);
                writer.WritePunctuation(":");
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
                writer.WritePunctuation("(");

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
                        writer.WritePunctuation(",");
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
                        writer.WritePunctuation(",");
                        writer.WriteSpace();
                    }

                    var propertyOrField = attribute.AttributeClass
                        .GetMembers(arg.Key)
                        .FirstOrDefault();

                    writer.WriteReference(propertyOrField, arg.Key);
                    writer.WriteSpace();
                    writer.WritePunctuation("=");
                    writer.WriteSpace();

                    WriteTypedConstant(arg.Value, writer);
                }
            }

            if (hasArguments)
                writer.WritePunctuation(")");

            if (!compact)
            {
                writer.WritePunctuation("]");
                writer.WriteLine();
            }
        }

        if (compact && attributesWritten)
        {
            writer.WritePunctuation("]");
            writer.WriteSpace();
        }
    }

    private static void WriteTypedConstant(TypedConstant constant, SyntaxWriter writer)
    {
        if (constant.IsNull)
        {
            if (constant.Type?.IsValueType == true)
                writer.WriteKeyword("default");
            else
                writer.WriteKeyword("null");
        }
        else
        {
            switch (constant.Kind)
            {
                case TypedConstantKind.Type:
                    writer.WriteKeyword("typeof");
                    writer.WritePunctuation("(");
                    WriteTypeReference((ITypeSymbol)constant.Value, writer);
                    writer.WritePunctuation(")");
                    break;
                case TypedConstantKind.Array:
                    writer.WriteKeyword("new");
                    writer.WritePunctuation("[");
                    writer.WritePunctuation("]");
                    writer.WriteSpace();
                    writer.WritePunctuation("{");
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
                            writer.WritePunctuation(",");
                            writer.WriteSpace();
                        }

                        WriteTypedConstant(value, writer);
                    }
                    writer.WriteSpace();
                    writer.WritePunctuation("}");
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
        if (value == null)
        {
            if (type.IsValueType)
                writer.WriteKeyword("default");
            else
                writer.WriteKeyword("null");
        }
        else if (type.TypeKind == TypeKind.Enum)
        {
            WriteEnum((INamedTypeSymbol)type, value, isForEnumDeclaration: false, writer);
        }
        else if (value is bool valueBool)
        {
            if (valueBool)
                writer.WriteKeyword("true");
            else
                writer.WriteKeyword("false");
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
                writer.WriteKeyword("private");
                break;
            case Accessibility.ProtectedAndInternal:
                writer.WriteKeyword("private");
                writer.WriteSpace();
                writer.WriteKeyword("protected");
                break;
            case Accessibility.Protected:
                writer.WriteKeyword("protected");
                break;
            case Accessibility.Internal:
                writer.WriteKeyword("internal");
                break;
            case Accessibility.ProtectedOrInternal:
                writer.WriteKeyword("protected");
                writer.WriteSpace();
                writer.WriteKeyword("internal");
                break;
            case Accessibility.Public:
                writer.WriteKeyword("public");
                break;
            default:
                throw new Exception($"Unexpected accessibility: {accessibility}");
        }
    }

    private static void WriteTypeParameterList(ImmutableArray<ITypeParameterSymbol> typeParameters, SyntaxWriter writer)
    {
        if (!typeParameters.Any())
            return;

        writer.WritePunctuation("<");

        var isFirst = true;

        foreach (var typeParameter in typeParameters)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                writer.WritePunctuation(",");
                writer.WriteSpace();
            }

            if (typeParameter.Variance == VarianceKind.In)
            {
                writer.WriteKeyword("in");
                writer.WriteSpace();
            }
            else if (typeParameter.Variance == VarianceKind.Out)
            {
                writer.WriteKeyword("out");
                writer.WriteSpace();
            }

            WriteTypeReference(typeParameter, writer);
        }

        writer.WritePunctuation(">");
    }

    private static void WriteConstraints(ImmutableArray<ITypeParameterSymbol> typeParameters, SyntaxWriter writer)
    {
        if (!typeParameters.Any())
            return;

        static void WriteConstructorConstraint(SyntaxWriter writer)
        {
            writer.WriteKeyword("new");
            writer.WritePunctuation("(");
            writer.WritePunctuation(")");
        }

        static void WriteNotNullConstraint(SyntaxWriter writer)
        {
            writer.WriteKeyword("notnull");
        }

        static void WriteReferenceTypeConstraint(SyntaxWriter writer)
        {
            writer.WriteKeyword("class");
        }

        static void WriteUnmanagedTypeConstraint(SyntaxWriter writer)
        {
            writer.WriteKeyword("unmanaged");
        }

        static void WriteValueTypeConstraint(SyntaxWriter writer)
        {
            writer.WriteKeyword("struct");
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
            writer.WriteKeyword("where");
            writer.WriteSpace();
            writer.WriteReference(typeParameter, typeParameter.Name);
            writer.WritePunctuation(":");
            writer.WriteSpace();

            for (var i = 0; i < constraintBuilders.Count; i++)
            {
                if (i > 0)
                {
                    writer.WritePunctuation(",");
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
                        writer.WritePunctuation(",");
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
                    writer.WritePunctuation("?");
                }
                else if (namedType.IsTupleType)
                {
                    writer.WritePunctuation("(");
                    var isFirst = true;
                    foreach (var field in namedType.TupleElements)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                        }
                        else
                        {
                            writer.WritePunctuation(",");
                            writer.WriteSpace();
                        }

                        WriteTypeReference(field.Type, writer);

                        if (field.IsExplicitlyNamedTupleElement)
                        {
                            writer.WriteSpace();
                            writer.WriteReference(field, field.Name);
                        }
                    }
                    writer.WritePunctuation(")");
                }
                else
                {
                    switch (namedType.SpecialType)
                    {
                        case SpecialType.System_Object:
                            writer.WriteKeyword("object");
                            break;
                        case SpecialType.System_Void:
                            writer.WriteKeyword("void");
                            break;
                        case SpecialType.System_Boolean:
                            writer.WriteKeyword("bool");
                            break;
                        case SpecialType.System_Char:
                            writer.WriteKeyword("char");
                            break;
                        case SpecialType.System_SByte:
                            writer.WriteKeyword("sbyte");
                            break;
                        case SpecialType.System_Byte:
                            writer.WriteKeyword("byte");
                            break;
                        case SpecialType.System_Int16:
                            writer.WriteKeyword("short");
                            break;
                        case SpecialType.System_UInt16:
                            writer.WriteKeyword("ushort");
                            break;
                        case SpecialType.System_Int32:
                            writer.WriteKeyword("int");
                            break;
                        case SpecialType.System_UInt32:
                            writer.WriteKeyword("uint");
                            break;
                        case SpecialType.System_Int64:
                            writer.WriteKeyword("long");
                            break;
                        case SpecialType.System_UInt64:
                            writer.WriteKeyword("ulong");
                            break;
                        case SpecialType.System_Decimal:
                            writer.WriteKeyword("decimal");
                            break;
                        case SpecialType.System_Single:
                            writer.WriteKeyword("float");
                            break;
                        case SpecialType.System_Double:
                            writer.WriteKeyword("double");
                            break;
                        case SpecialType.System_String:
                            writer.WriteKeyword("string");
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
                writer.WritePunctuation("[");
                for (var i = 1; i < array.Rank; i++)
                    writer.WritePunctuation(",");
                writer.WritePunctuation("]");
                break;
            case TypeKind.Pointer:
                var ptr = (IPointerTypeSymbol)type;
                WriteTypeReference(ptr.PointedAtType, writer);
                writer.WritePunctuation("*");
                break;
            case TypeKind.Dynamic:
                writer.WriteKeyword("dynamic");
                break;
            case TypeKind.FunctionPointer:
            {
                var fp = (IFunctionPointerTypeSymbol)type;
                writer.WriteKeyword("delegate");
                writer.WritePunctuation("*");

                if (fp.Signature.CallingConvention != SignatureCallingConvention.Default)
                {
                    writer.WriteSpace();

                    switch (fp.Signature.CallingConvention)
                    {
                        case SignatureCallingConvention.Unmanaged:
                            writer.WriteKeyword("unmanaged");
                            break;
                        case SignatureCallingConvention.CDecl:
                            WriteCallingConvention("Cdecl", writer);
                            break;
                        case SignatureCallingConvention.StdCall:
                            WriteCallingConvention("Stdcall", writer);
                            break;
                        case SignatureCallingConvention.ThisCall:
                            WriteCallingConvention("Thiscall", writer);
                            break;
                        case SignatureCallingConvention.FastCall:
                            WriteCallingConvention("Fastcall", writer);
                            break;
                        default:
                            throw new Exception($"Unexpected calling convention: {fp.Signature.CallingConvention}");
                    }

                    static void WriteCallingConvention(string name, SyntaxWriter writer)
                    {
                        writer.WriteKeyword("unmanaged");
                        writer.WritePunctuation("[");
                        writer.WriteKeyword(name);
                        writer.WritePunctuation("]");
                    }
                }

                writer.WritePunctuation("<");

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
                        writer.WritePunctuation(",");
                        writer.WriteSpace();
                    }
                }

                writer.WritePunctuation(">");
                break;
            }
            case TypeKind.Error:
                writer.WritePunctuation("?");
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
                writer.WritePunctuation("?");
            }
            else if (type.NullableAnnotation == NullableAnnotation.NotAnnotated)
            {
                writer.WritePunctuation("!");
            }
        }
    }

    private static void WriteTypeParameterReferences(ImmutableArray<ITypeSymbol> typeParameters, SyntaxWriter writer)
    {
        if (!typeParameters.Any())
            return;

        writer.WritePunctuation("<");

        for (var i = 0; i < typeParameters.Length; i++)
        {
            if (i > 0)
            {
                writer.WritePunctuation(",");
                writer.WriteSpace();
            }

            WriteTypeReference(typeParameters[i], writer);
        }

        writer.WritePunctuation(">");
    }

    private static void WriteParameterList(ImmutableArray<IParameterSymbol> parameters, SyntaxWriter writer)
    {
        writer.WritePunctuation("(");
        WriteParameters(parameters, writer);
        writer.WritePunctuation(")");
    }

    private static void WriteParameters(ImmutableArray<IParameterSymbol> parameters, SyntaxWriter writer)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
            {
                writer.WritePunctuation(",");
                writer.WriteSpace();
            }

            var parameter = parameters[i];
            var method = parameter.ContainingSymbol as IMethodSymbol;
            WriteAttributeList(parameter.GetAttributes(), writer, compact: true);

            if (parameter.IsParams)
            {
                writer.WriteKeyword("params");
                writer.WriteSpace();
            }

            if (i == 0 && method?.IsExtensionMethod == true)
            {
                writer.WriteKeyword("this");
                writer.WriteSpace();
            }

            if (parameter.RefKind == RefKind.In)
            {
                writer.WriteKeyword("in");
                writer.WriteSpace();
            }
            else if (parameter.RefKind == RefKind.Ref)
            {
                writer.WriteKeyword("ref");
                writer.WriteSpace();
            }
            else if (parameter.RefKind == RefKind.RefReadOnly)
            {
                writer.WriteKeyword("ref");
                writer.WriteSpace();
                writer.WriteKeyword("readonly");
                writer.WriteSpace();
            }
            else if (parameter.RefKind == RefKind.Out)
            {
                writer.WriteKeyword("out");
                writer.WriteSpace();
            }

            WriteTypeReference(parameter.Type, writer);
            writer.WriteSpace();
            writer.WriteReference(parameter, parameter.Name);

            if (parameter.HasExplicitDefaultValue)
            {
                writer.WriteSpace();
                writer.WritePunctuation("=");
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
            if (attribute.AttributeClass is null || attribute.AttributeConstructor == null)
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
                    writer.WritePunctuation("|");
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
            writer.WritePunctuation(".");
        }

        writer.WriteReference(symbol, symbol.Name);
    }

    private static void WriteExplicitlyCastedLiteralValue(INamedTypeSymbol enumType,
                                                          object constantValue,
                                                          SyntaxWriter writer)
    {
        writer.WritePunctuation("(");
        WriteTypeReference(enumType, writer);
        writer.WritePunctuation(")");
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
            get { return this.Name == null; }
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