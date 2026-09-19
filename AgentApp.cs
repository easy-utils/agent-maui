using Easy.Agent.Services;

namespace Easy.Agent;

/// <summary>App-wide helpers: build the API with the bundled dev-cluster CA.</summary>
public static class AgentApp
{
    private static string? _caPem;

    public static AgentApi CreateApi(string baseUrl, string token)
        => new(baseUrl.TrimEnd('/'), token, LoadCa());

    private static string? LoadCa()
    {
        if (_caPem != null) return _caPem;
        try
        {
            using var s = FileSystem.OpenAppPackageFileAsync("agent_ca.crt")
                .GetAwaiter().GetResult();
            using var r = new StreamReader(s);
            _caPem = r.ReadToEnd();
        }
        catch
        {
            _caPem = null;
        }
        return _caPem;
    }
}
