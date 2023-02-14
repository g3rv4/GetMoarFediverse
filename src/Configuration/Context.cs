using GetMoarFediverse.Configuration.Unsafe;

namespace GetMoarFediverse.Configuration;

public static class Context
{
    public static Config Configuration { get; private set; } = null!;
    
    public static void Load(string filename)
    {
        Configuration = UnsafeConfig.ToConfig(filename);
    }
}