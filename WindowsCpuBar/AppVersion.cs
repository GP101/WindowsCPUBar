namespace WindowsCpuBar;

internal static class AppVersion
{
    public static string Current { get; } =
        typeof(AppVersion).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), inherit: false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .SingleOrDefault()
            ?.InformationalVersion
        ?? typeof(AppVersion).Assembly.GetName().Version?.ToString(2)
        ?? "1.2";
}
