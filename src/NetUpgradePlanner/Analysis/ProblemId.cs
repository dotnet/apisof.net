namespace NetUpgradePlanner.Analysis;

internal sealed record ProblemId(ProblemSeverity Severity, ProblemCategory Category, string Text, string? Url);
