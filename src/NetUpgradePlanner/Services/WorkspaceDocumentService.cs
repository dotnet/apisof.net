using Terrajobst.NetUpgradePlanner;

namespace NetUpgradePlanner.Services;

internal sealed class WorkspaceDocumentService
{
    private readonly WorkspaceService _workspaceService;
    private Workspace _previousWorkspace;

    public WorkspaceDocumentService(WorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
        _workspaceService.Changed += WorkspaceService_Changed;
        _previousWorkspace = workspaceService.Current;
    }

    public bool IsDirty { get; private set; }

    public string? FileName { get; private set; }

    public void Clear()
    {
        _workspaceService.Clear();

        IsDirty = false;
        FileName = null;
        OnChanged();
    }

    public async Task LoadAsync(string fileName)
    {
        var workspace = await WorkspacePersistence.LoadAsync(fileName);
        _workspaceService.Current = workspace;

        FileName = fileName;
        IsDirty = false;
        OnChanged();

        await _workspaceService.AnalyzeAsync();
    }

    public async Task SaveAsync(string fileName)
    {
        await WorkspacePersistence.SaveAsync(_workspaceService.Current, fileName);

        FileName = fileName;
        IsDirty = false;
        OnChanged();
    }

    private void WorkspaceService_Changed(object? sender, EventArgs e)
    {
        if (_workspaceService.Current.InputChanged(_previousWorkspace))
        {
            IsDirty = true;
            OnChanged();
        }

        _previousWorkspace = _workspaceService.Current;
    }

    private void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Changed;
}
