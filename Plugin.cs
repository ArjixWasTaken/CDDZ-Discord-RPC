using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Discord;
using HarmonyLib;
using PlayFabMatchmaking20;
using Server.Emulator.Tools;
using UdpKit;
using UnityEngine;

namespace CDInDeeZ;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
class Plugin : BaseUnityPlugin
{
    public static Plugin Instance;
    public static ManualLogSource Log => Instance.Logger;

    readonly Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);
    
    static readonly Dictionary<KeyCode, Action> _keybindsUp = new();
    static readonly Dictionary<KeyCode, Action> _keybindsDown = new();
    
    static long OldTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    static long NewTime = OldTime;
    
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

    class Message
        {
            public long Time;
            public string Content;

            public Message(string msg)
            {
                Time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                Content = msg;
            }
        }

    void SendMessageInConsole(string message)
    {
        scrollPosition.y = _messages.Count * 20;
        _messages.Add(new Message(message));
    }

    static FirstPersonMover Player;

    void Update()
    {
        NewTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        try
        {
            Player = (FirstPersonMover)CharacterTracker.Instance.GetPlayer();
        }
        catch (Exception) { }

        for (var i = 0; i < _messages.Count; i++)
        {
            if (_messages[i].Time + 5000 >= NewTime) continue;
            _messages.RemoveAt(i);
            i--;
        }

        if (!Data.HasSetEventListeners)
        {
            try
            {
                if (GlobalEventManager.Instance != null)
                {
                    // set the event listeners
                    GlobalEventManager.Instance.AddEventListener("LevelDefeated", () => {});
                    Data.HasSetEventListeners = true;
                }
            }
            catch (Exception) { }
        }

        if (!Input.GetKey(KeyCode.RightControl) && !Input.GetKey(KeyCode.LeftControl)) return;
        foreach (var keybind in _keybindsUp)
        {
            if (Input.GetKeyUp(keybind.Key))
            {
                keybind.Value();
            }
        }
        foreach (var keybind in _keybindsDown)
        {
            if (Input.GetKey(keybind.Key))
            {
                keybind.Value();
            }
        }
    }

    static GUIStyle Style = new()
    {
        fontSize = 18,
        normal = new()
        {
            textColor = Color.white
        }
    };
    static readonly Rect _windowRect = new(25, 25, 300, 800);
    static readonly List<Message> _messages = new();
    public Vector2 scrollPosition = Vector2.zero;
    
    void OnGUI()
    {
        if (Player == null) return;
        
        const int height = 280;
        var width = Screen.width * 0.18f;
        GUILayout.BeginHorizontal();

        var rectBox = new Rect(0, (Screen.height * 0.75f) - height, width, height);
        var viewRect = new Rect(rectBox.x, rectBox.y, rectBox.width, _messages.Count * 20f);

        GUI.Box(rectBox, GUIContent.none);

        scrollPosition = GUI.BeginScrollView(rectBox, scrollPosition, viewRect, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

        const int viewCount = 15;
        var maxCharacterLength = ((int)width / 20) * 2;
        var firstIndex = (int)scrollPosition.y / 20;

        var contentPos = new Rect(rectBox.x, viewRect.y + (firstIndex * 20f), rectBox.width, 20f);

        for (var i = firstIndex; i < Mathf.Min(_messages.Count, firstIndex + viewCount); i++)
        {
            var text = _messages[i].Content;
            if (text.Length > maxCharacterLength)
            {
                text = text.Substring(0, maxCharacterLength - 3) + "...";
            }
            GUI.Label(contentPos, text, Style);
            contentPos.y += 20f;
        }

        GUI.EndScrollView();
        GUILayout.EndHorizontal();
    }

    static class Data
    {
        public static bool HasSetEventListeners = false;
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
        [HarmonyPatch(typeof(MultiplayerInviteCodeUI), "ShowWithCode")]
        static void ShowWithCode(string inviteCode, bool showSettings)
        {
            RichPresence.Instance.InviteCode = inviteCode;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerMatchmakingManager), nameof(MultiplayerMatchmakingManager.BoltShutdownBegin))]
        static void BoltShutdownBegin()
        {
            RichPresence.Instance.InviteCode = null;
        }
    }
}
