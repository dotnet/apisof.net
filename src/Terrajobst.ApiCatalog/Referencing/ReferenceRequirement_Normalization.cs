using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Terrajobst.ApiCatalog;

partial class ReferenceRequirement
{
    public static ReferenceRequirement? Normalize(ReferenceRequirement requirement)
    {
        ThrowIfNull(requirement);

        requirement = Flatten(requirement);

        if (requirement is AndReferenceRequirement and)
        {
            var requirements = NormalizeCollection(and.Requirements);
            var result = requirements is null ? and : new AndReferenceRequirement(requirements);

            if (result.Requirements.Count == 0)
                return null;
            else if (result.Requirements.Count == 1)
                return result.Requirements[0];
            else
                return result;
        }

        if (requirement is OrReferenceRequirement or)
        {
            var requirements = NormalizeCollection(or.Requirements);
            var result = requirements is null ? or : new OrReferenceRequirement(requirements);

            if (result.Requirements.Count == 0)
                return null;
            else if (result.Requirements.Count == 1)
                return result.Requirements[0];
            else
                return result;
        }

        return requirement;

        static ReferenceRequirement[]? NormalizeCollection(IReadOnlyList<ReferenceRequirement> requirements)
        {
            List<ReferenceRequirement>? builder = null;
            for (var i = 0; i < requirements.Count; i++)
            {
                var r = requirements[i];
                var normalizedR = Normalize(r);

                if (!ReferenceEquals(r, normalizedR) && builder is null)
                {
                    builder = new List<ReferenceRequirement>(requirements.Count);
                    for (var j = 0; j < i; j++)
                        builder.Add(requirements[j]);
                }

                if (builder is not null && normalizedR is not null)
                    builder.Add(normalizedR);
            }

            return builder?.ToArray();
        }
    }

    private static ReferenceRequirement Flatten(ReferenceRequirement requirement)
    {
        if (requirement is AndReferenceRequirement and)
        {
            var flattened = FlattenFor(and, and.Requirements);
            if (flattened is null)
                return and;

            return new AndReferenceRequirement(flattened);
        }

        if (requirement is OrReferenceRequirement or)
        {
            var flattened = FlattenFor(or, or.Requirements);
            if (flattened is null)
                return or;

            return new OrReferenceRequirement(flattened);
        }

        return requirement;
    }

    static ReferenceRequirement[]? FlattenFor(ReferenceRequirement parent, IReadOnlyList<ReferenceRequirement> children)
    {
        List<ReferenceRequirement>? builder = null;
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var flattenedChild = Flatten(child);

            var needsBuilder = !ReferenceEquals(child, flattenedChild) ||
                               flattenedChild.GetType() == parent.GetType();

            if (needsBuilder && builder is null)
            {
                builder = new List<ReferenceRequirement>(children.Count);
                for (var j = 0; j < i; j++)
                    builder.Add(children[j]);
            }

            if (flattenedChild is AndReferenceRequirement nestedAnd && parent is AndReferenceRequirement)
            {
                Debug.Assert(builder is not null);
                builder.AddRange(nestedAnd.Requirements);
            }
            else if (flattenedChild is OrReferenceRequirement nestedOr && parent is OrReferenceRequirement)
            {
                Debug.Assert(builder is not null);
                builder.AddRange(nestedOr.Requirements);
            }
            else if (builder is not null && flattenedChild is not null)
            {
                builder.Add(flattenedChild);
            }
        }

        return builder?.ToArray();
    }
}
