using Microsoft.Extensions.DependencyInjection;

internal static class CommandExtensions
{
    public static void AddCommands<T>(this IServiceCollection services)
    {
        var commandType = typeof(Command);
        var assembly = typeof(T).Assembly;
        var derivedTypes = assembly.GetTypes()
                                   .Where(t => !t.IsAbstract && t.IsAssignableTo(commandType));
        foreach (var derivedType in derivedTypes)
            services.AddSingleton(commandType, derivedType);
    }
}
