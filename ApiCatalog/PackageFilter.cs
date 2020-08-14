using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ApiCatalog
{
    public sealed class PackageFilterExpression
    {
        private PackageFilterExpression(string text, bool isPrefix, bool isSuffix)
        {
            Text = text;
            IsPrefix = isPrefix;
            IsSuffix = isSuffix;
        }

        public string Text { get; }
        public bool IsExact => !IsPrefix && !IsSuffix;
        public bool IsPrefix { get; }
        public bool IsSuffix { get; }

        public static PackageFilterExpression Parse(string text)
        {
            var firstAsterisk = text.IndexOf('*');
            var lastAsterisk = text.LastIndexOf('*');
            if (firstAsterisk != lastAsterisk)
                throw new FormatException();

            var asterisk = firstAsterisk;
            if (asterisk > 0 && asterisk < text.Length - 1)
                throw new FormatException();

            var isPrefix = asterisk == text.Length - 1;
            var isSuffix = asterisk == 0;

            if (isPrefix)
            {
                text = text.Substring(0, text.Length - 1);
                if (text.EndsWith('.'))
                    text = text.Substring(0, text.Length - 1);
            }
            else if (isSuffix)
            {
                text = text.Substring(1);
            }

            return new PackageFilterExpression(text, isPrefix, isSuffix);
        }

        public bool IsMatch(string packageId)
        {
            if (IsPrefix)
            {
                return packageId.Equals(Text, StringComparison.OrdinalIgnoreCase) ||
                       packageId.StartsWith(Text + ".", StringComparison.OrdinalIgnoreCase);
            }
            else if (IsSuffix)
            {
                return packageId.EndsWith(Text, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return packageId.Equals(Text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public sealed class PackageFilter
    {
        public PackageFilter(IEnumerable<PackageFilterExpression> includes, IEnumerable<PackageFilterExpression> excludes)
        {
            Includes = includes.ToImmutableArray();
            Excludes = excludes.ToImmutableArray();
        }

        public ImmutableArray<PackageFilterExpression> Includes { get; }
        public ImmutableArray<PackageFilterExpression> Excludes { get; }

        public bool IsMatch(string packageId)
        {
            return Includes.Any(e => e.IsMatch(packageId)) &&
                   !Excludes.Any(e => e.IsMatch(packageId));
        }

        public static PackageFilter Default = new PackageFilter(
            includes: new[]
            {
                PackageFilterExpression.Parse("EntityFramework.*"),
                PackageFilterExpression.Parse("Microsoft.AspNet.*"),
                PackageFilterExpression.Parse("Microsoft.AspNetCore.*"),
                PackageFilterExpression.Parse("Microsoft.Bcl.*"),
                PackageFilterExpression.Parse("Microsoft.Build.*"),
                PackageFilterExpression.Parse("Microsoft.CodeAnalysis.*"),
                PackageFilterExpression.Parse("Microsoft.CompilerServices.AsyncTargetingPack"),
                PackageFilterExpression.Parse("Microsoft.CSharp.*"),
                PackageFilterExpression.Parse("Microsoft.Data.*"),
                PackageFilterExpression.Parse("Microsoft.Diagnostics.*"),
                PackageFilterExpression.Parse("Microsoft.EntityFrameworkCore.*"),
                PackageFilterExpression.Parse("Microsoft.Extensions.*"),
                PackageFilterExpression.Parse("Microsoft.JSInterop.*"),
                PackageFilterExpression.Parse("Microsoft.ML.*"),
                PackageFilterExpression.Parse("Microsoft.Net.Http.*"),
                PackageFilterExpression.Parse("Microsoft.ReverseProxy.*"),
                PackageFilterExpression.Parse("Microsoft.Spark.*"),
                PackageFilterExpression.Parse("Microsoft.VisualBasic.*"),
                PackageFilterExpression.Parse("Microsoft.Win32.*"),
                PackageFilterExpression.Parse("System.*"),
                PackageFilterExpression.Parse("Iot.*")
            },
            excludes: new[]
            {
                PackageFilterExpression.Parse("*.cs"),
                PackageFilterExpression.Parse("*.de"),
                PackageFilterExpression.Parse("*.es"),
                PackageFilterExpression.Parse("*.fr"),
                PackageFilterExpression.Parse("*.it"),
                PackageFilterExpression.Parse("*.ja"),
                PackageFilterExpression.Parse("*.ko"),
                PackageFilterExpression.Parse("*.pl"),
                PackageFilterExpression.Parse("*.pt-br"),
                PackageFilterExpression.Parse("*.ru"),
                PackageFilterExpression.Parse("*.tr"),
                PackageFilterExpression.Parse("*.zh-Hans"),
                PackageFilterExpression.Parse("*.zh-Hant"),
            }
        );
    }
}
