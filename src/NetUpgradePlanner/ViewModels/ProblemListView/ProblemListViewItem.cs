using System.Diagnostics;
using System.Windows.Media;

using Terrajobst.ApiCatalog;
using Terrajobst.NetUpgradePlanner;

namespace NetUpgradePlanner.ViewModels.MainWindow;

internal sealed class ProblemListViewItem : IComparable<ProblemListViewItem>
{
    public ProblemListViewItem(object? data,
                               ImageSource? icon,
                               string text)
        : this(data, icon, text, Array.Empty<ProblemListViewItem>())
    {
    }

    public ProblemListViewItem(object? data,
                               ImageSource? icon,
                               string text,
                               IEnumerable<ProblemListViewItem> children)
    {
        Data = data;
        Icon = icon;
        Text = text;
        Children = children.ToArray();
    }

    public object? Data { get; }

    public ImageSource? Icon { get; }

    public string Text { get; }

    public IReadOnlyList<ProblemListViewItem> Children { get; }

    public int CompareTo(ProblemListViewItem? other)
    {
        if (other is null)
            return -1;

        if (Data is Problem myProblem && other.Data is Problem otherProblem)
        {
            // Unresolved

            var myIsUnresolved = myProblem.UnresolvedReference is not null && myProblem.ResolvedReference is null;
            var otherIsUnresolved = otherProblem.UnresolvedReference is not null && otherProblem.ResolvedReference is null;

            if (myIsUnresolved != otherIsUnresolved)
                return myIsUnresolved.CompareTo(otherIsUnresolved);
            else if (myIsUnresolved && otherIsUnresolved)
                return CompareUnresolved(myProblem.UnresolvedReference!, otherProblem.UnresolvedReference!);

            // Resolved

            var myIsResolved = myProblem.ResolvedReference is not null;
            var otherIsResolved = otherProblem.ResolvedReference is not null;

            if (myIsResolved != otherIsResolved)
                return myIsResolved.CompareTo(otherIsResolved);
            else if (myIsResolved && otherIsResolved)
                return CompareResolved(myProblem.ResolvedReference!, otherProblem.ResolvedReference!);

            // API

            Debug.Assert(myProblem.Api is not null && otherProblem.Api is not null);
            return CompareApi(myProblem.Api.Value, otherProblem.Api.Value);
        }

        return Text.CompareTo(other.Text);

        static int CompareUnresolved(string x, string y)
        {
            return x.CompareTo(y);
        }

        static int CompareResolved(AssemblySetEntry x, AssemblySetEntry y)
        {
            return x.Name.CompareTo(y.Name);
        }

        static int CompareApi(ApiModel x, ApiModel y)
        {
            var result = x.GetNamespaceName().CompareTo(y.GetNamespaceName());
            if (result != 0)
                return result;

            result = x.GetTypeName().CompareTo(y.GetTypeName());
            if (result != 0)
                return result;

            return x.Name.CompareTo(y.Name);
        }
    }
}
