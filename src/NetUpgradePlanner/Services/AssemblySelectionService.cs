using System;

using NetUpgradePlanner.Analysis;

namespace NetUpgradePlanner.Services;

internal sealed class AssemblySelectionService
{
    private AssemblySetEntry? _selection;

    public AssemblySetEntry? Selection
    {
        get => _selection;
        set
        {
            if (_selection != value)
            {
                _selection = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? Changed;
}