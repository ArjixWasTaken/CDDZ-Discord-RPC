using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Plugin.Tools;

namespace CDInDeeZ;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
class Plugin : BaseUnityPlugin
{
    public static Plugin Instance;
    public static ManualLogSource Log => Instance.Logger;

    readonly Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);
    void Awake()
    {
        Instance = this;
        _harmony.PatchAll();

        RichPresence.Instance.Init();
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    void onInit()
    {
        Log.LogInfo("Game initialized!!!");

        Run.Every(1f, 1f, () =>
        {
            RichPresence.Instance.InviteCode =
                BoltGlobalEventListenerSingleton<MultiplayerMatchmakingManager>.Instance._lastInviteCode;
        });
    }

    [HarmonyPatch]
    class GameFlowPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameFlowManager), "Start")]
        static void OnGameInitialized()
        {
            Instance.onInit();
        }
    }

    [HarmonyPatch]
    class MultiplayerMatchmakingPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerMatchmakingManager), nameof(MultiplayerMatchmakingManager.BoltShutdownBegin))]
        static void BoltShutdownBegin()
        {
            RichPresence.Instance.InviteCode = null;
        }
    }
}
