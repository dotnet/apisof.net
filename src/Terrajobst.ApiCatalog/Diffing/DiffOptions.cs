namespace Terrajobst.ApiCatalog;

[Flags]
public enum DiffOptions
{
    None,
    IncludeAdded = 0x01,
    IncludeRemoved = 0x02,
    IncludeChanged = 0x04,
    IncludeUnchanged = 0x08,
    Default = IncludeAdded | IncludeRemoved | IncludeChanged
}