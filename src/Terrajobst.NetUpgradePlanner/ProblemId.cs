namespace Terrajobst.NetUpgradePlanner;

public sealed record ProblemId(ProblemSeverity Severity, ProblemCategory Category, string Text, string? Url);
