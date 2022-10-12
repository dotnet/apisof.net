namespace NetUpgradePlanner.Services;

internal enum IconKind
{
    None,

    // General
    Error,
    Warning,

    // Metadata
    Assembly,
    Namespace,
    Class,
    Struct,
    Interface,
    Delegate,
    Enum,

    Constant,
    EnumItem,
    Event,
    Field,
    Method,
    Operator,
    Property
}
