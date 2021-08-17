using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ApiCatalog
{
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
            WriteConstant(field.Type, field.ConstantValue, writer);
        }

        private static void WriteMethodDeclaration(IMethodSymbol method, SyntaxWriter writer)
        {
            WriteAttributeList(method.GetAttributes(), writer);
            WriteAttributeList(method.GetReturnTypeAttributes(), "return", writer);
            WriteAccessibility(method.DeclaredAccessibility, writer);
            writer.WriteSpace();

            if (method.IsStatic)
            {
                writer.WriteKeyword("static");
                writer.WriteSpace();
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
            WriteAccessibility(property.DeclaredAccessibility, writer);
            writer.WriteSpace();
            WriteTypeReference(property.Type, writer);
            writer.WriteSpace();

            if (property.IsStatic)
            {
                writer.WriteKeyword("static");
                writer.WriteSpace();
            }

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
            WriteAccessibility(@event.DeclaredAccessibility, writer);
            writer.WriteSpace();

            if (@event.IsStatic)
            {
                writer.WriteKeyword("static");
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
                    writer.WriteKeyword("get");
                    writer.WritePunctuation(";");
                    writer.WriteLine();
                }

                if (@event.RemoveMethod != null)
                {
                    WriteAttributeList(removerAttributes, writer);
                    writer.WriteKeyword("set");
                    writer.WritePunctuation(";");
                    writer.WriteLine();
                }

                writer.Indent--;
                writer.WritePunctuation("}");
                writer.WriteLine();
            }
        }

        private static void WriteAttributeList(ImmutableArray<AttributeData> attributes, SyntaxWriter writer)
        {
            WriteAttributeList(attributes, null, writer);
        }

        private static void WriteAttributeList(ImmutableArray<AttributeData> attributes, string target, SyntaxWriter writer)
        {
            foreach (var attribute in attributes.Ordered())
            {
                if (!attribute.IsIncludedInCatalog())
                    continue;

                writer.WritePunctuation("[");

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

                writer.WriteReference(attribute.AttributeClass, typeName);

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

                writer.WritePunctuation("]");
                writer.WriteLine();
            }
        }

        private static void WriteTypedConstant(TypedConstant constant, SyntaxWriter writer)
        {
            if (constant.IsNull)
            {
                if (constant.Type.IsValueType)
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
                        // HACK: Fix this
                        writer.WritePunctuation(constant.ToCSharpString());
                        break;
                    case TypedConstantKind.Enum:
                        // HACK: Fix this
                        writer.WritePunctuation(constant.ToCSharpString());
                        break;
                    case TypedConstantKind.Primitive:
                        WriteConstant(constant.Type, constant.Value, writer);
                        break;
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
                // HACK: Fix this
                writer.WritePunctuation("(");
                WriteTypeReference(type, writer);
                writer.WritePunctuation(")");
                var text = value is ulong
                    ? SyntaxFactory.Literal(Convert.ToUInt64(value)).ToString()
                    : SyntaxFactory.Literal(Convert.ToInt64(value)).ToString();
                writer.WriteLiteralNumber(text);
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
                        case SpecialType.System_Nullable_T:
                            WriteTypeReference(namedType.TypeArguments[0], writer);
                            writer.WritePunctuation("?");
                            break;
                        default:
                            writer.WriteReference(namedType, namedType.Name);
                            WriteTypeParameterReferences(namedType.TypeArguments, writer);
                            break;
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
                    writer.WritePunctuation("]");
                    break;
                case TypeKind.Pointer:
                    var ptr = (IPointerTypeSymbol)type;
                    WriteTypeReference(ptr.PointedAtType, writer);
                    writer.WritePunctuation("*");
                    break;
                case TypeKind.Dynamic:
                    writer.WriteKeyword("dyanmic");
                    break;
                case TypeKind.FunctionPointer:
                    Debugger.Break();
                    break;
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
                WriteAttributeList(parameter.GetAttributes(), writer);

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
    }
}
