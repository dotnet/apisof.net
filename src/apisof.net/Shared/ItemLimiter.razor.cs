using Microsoft.AspNetCore.Components;

namespace ApisOfDotNet.Shared;

[CascadingTypeParameter(nameof(TItem))]
public partial class ItemLimiter<TItem>
{
    private bool _showMore;
    private TItem[]? _items;

    [Parameter, EditorRequired]
    public required IEnumerable<TItem> ItemSource { get; set; }

    [Parameter]
    public int Limit { get; set; } = 20;

    [Parameter]
    public required RenderFragment<TItem> ChildContent { get; set; }

    private TItem[] Items
    {
        get
        {
            if (_items is null)
                _items = ItemSource.ToArray();

            return _items;
        }
    }
}