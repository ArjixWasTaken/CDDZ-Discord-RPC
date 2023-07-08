namespace CDInDeeZ;

public static class RichPresenceLogger
{
    public static void LogInfo(string message)
    {
        Plugin.Log.LogInfo($"[RichPresence]: {message}");
    }

    public static void LogError(string message)
    {
        Plugin.Log.LogError($"[RichPresence]: {message}");
    }

    public static void LogWarning(string message)
    {
        Plugin.Log.LogWarning($"[RichPresence]: {message}");
    }

    public static void LogDebug(string message)
    {
        Plugin.Log.LogDebug($"[RichPresence]: {message}");
    }
}