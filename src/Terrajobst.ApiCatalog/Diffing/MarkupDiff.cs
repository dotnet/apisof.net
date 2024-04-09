namespace Terrajobst.ApiCatalog;

public enum MarkupTokenDiffKind
{
    None,
    Added,
    Removed
}

public record struct MarkupTokenWithDiff(MarkupToken Token, MarkupTokenDiffKind Diff);

public static class MarkupDiff
{
    public static IEnumerable<MarkupTokenWithDiff> Diff(Markup left, Markup right)
    {
        ThrowIfNull(left);
        ThrowIfNull(right);

        var startOfLine = true;

        foreach (var t in MarkupDiffer.Default.CalculateDiff(left, right))
        {
            var token = t;

            switch (token.Token.Kind)
            {
                case MarkupTokenKind.LineBreak:
                    startOfLine = true;
                    break;
                case MarkupTokenKind.Space:
                {
                    if (startOfLine)
                    {
                        token = token with {
                            Diff = MarkupTokenDiffKind.None
                        };
                    }

                    break;
                }
                default:
                    startOfLine = false;
                    break;
            }

            yield return token;
        }
    }

    private sealed class MarkupDiffer : LongestCommonSubsequence<Markup>
    {
        public static readonly MarkupDiffer Default = new();

        protected override bool ItemsEqual(Markup sequenceA, int indexA, Markup sequenceB, int indexB)
        {
            return sequenceA.Tokens[indexA] == sequenceB.Tokens[indexB];
        }

        public IEnumerable<MarkupTokenWithDiff> CalculateDiff(Markup sequenceA, Markup sequenceB)
        {
            foreach (var edit in GetEdits(sequenceA, sequenceA.Tokens.Length, sequenceB, sequenceB.Tokens.Length).Reverse())
            {
                switch (edit.Kind)
                {
                    case EditKind.None:
                        yield return new MarkupTokenWithDiff(sequenceA.Tokens[edit.IndexA], MarkupTokenDiffKind.None);
                        break;

                    case EditKind.Delete:
                        yield return new MarkupTokenWithDiff(sequenceA.Tokens[edit.IndexA], MarkupTokenDiffKind.Removed);
                        break;

                    case EditKind.Insert:
                        yield return new MarkupTokenWithDiff(sequenceB.Tokens[edit.IndexB], MarkupTokenDiffKind.Added);
                        break;

                    case EditKind.Update:
                        yield return new MarkupTokenWithDiff(sequenceA.Tokens[edit.IndexA], MarkupTokenDiffKind.Removed);
                        yield return new MarkupTokenWithDiff(sequenceB.Tokens[edit.IndexB], MarkupTokenDiffKind.Added);
                        break;
                }
            }
        }
    }

    private enum EditKind
    {
        None = 0,
        Update = 1,
        Insert = 2,
        Delete = 3,
    }

    private abstract class LongestCommonSubsequence<TSequence>
    {
        protected readonly struct Edit
        {
            public readonly EditKind Kind;
            public readonly int IndexA;
            public readonly int IndexB;

            internal Edit(EditKind kind, int indexA, int indexB)
            {
                this.Kind = kind;
                this.IndexA = indexA;
                this.IndexB = indexB;
            }
        }

        private const int DeleteCost = 1;
        private const int InsertCost = 1;
        private const int UpdateCost = 2;

        protected abstract bool ItemsEqual(TSequence sequenceA, int indexA, TSequence sequenceB, int indexB);

        protected IEnumerable<Edit> GetEdits(TSequence sequenceA, int lengthA, TSequence sequenceB, int lengthB)
        {
            var d = ComputeCostMatrix(sequenceA, lengthA, sequenceB, lengthB);
            var i = lengthA;
            var j = lengthB;

            while (i != 0 && j != 0)
            {
                if (d[i, j] == d[i - 1, j - 1] + UpdateCost)
                {
                    i--;
                    j--;
                    yield return new Edit(EditKind.Update, i, j);
                }
                else if (d[i, j] == d[i - 1, j] + DeleteCost)
                {
                    i--;
                    yield return new Edit(EditKind.Delete, i, -1);
                }
                else if (d[i, j] == d[i, j - 1] + InsertCost)
                {
                    j--;
                    yield return new Edit(EditKind.Insert, -1, j);
                }
                else
                {
                    i--;
                    j--;
                    yield return new Edit(EditKind.None, i, j);
                }
            }

            while (i > 0)
            {
                i--;
                yield return new Edit(EditKind.Delete, i, -1);
            }

            while (j > 0)
            {
                j--;
                yield return new Edit(EditKind.Insert, -1, j);
            }
        }

        private int[,] ComputeCostMatrix(TSequence sequenceA, int lengthA, TSequence sequenceB, int lengthB)
        {
            var la = lengthA + 1;
            var lb = lengthB + 1;

            var d = new int[la, lb];

            d[0, 0] = 0;
            for (var i = 1; i <= lengthA; i++)
            {
                d[i, 0] = d[i - 1, 0] + DeleteCost;
            }

            for (var j = 1; j <= lengthB; j++)
            {
                d[0, j] = d[0, j - 1] + InsertCost;
            }

            for (var i = 1; i <= lengthA; i++)
            {
                for (var j = 1; j <= lengthB; j++)
                {
                    var m1 = d[i - 1, j - 1] + (ItemsEqual(sequenceA, i - 1, sequenceB, j - 1) ? 0 : UpdateCost);
                    var m2 = d[i - 1, j] + DeleteCost;
                    var m3 = d[i, j - 1] + InsertCost;
                    d[i, j] = Math.Min(Math.Min(m1, m2), m3);
                }
            }

            return d;
        }
    }
}