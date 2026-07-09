using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KrokoshaCasualtiesMP;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace TickHappyMod;

[BepInPlugin(Guid, Name, Version)]
[BepInDependency("KrokoshaCasualtiesMP", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    public const string Guid = "org.cncumc.tickhappymod";
    public const string Name = "Tick Happy Mod";
    public const string Version = "1.0.0";
    internal new static ManualLogSource Logger;
    private readonly Harmony _harmony = new(Guid);

    public static ConfigEntry<string> BanMods;

    public static List<string> BanModsList =>
        BanMods?.Value
            ?.Split([',', ' ', ';', '|', '、', '，', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList() ?? [];

    private static bool IsMpActive =>
        Chainloader.PluginInfos.ContainsKey("KrokoshaCasualtiesMP") &&
        PlayerPrefs.GetInt("KrokoshaCasualtiesMP_FORCE_DISABLE_MP_MOD", 0) == 0;

    private const ushort NetMsgId = 33993;
    private static bool _registered;

    public void Awake()
    {
        Logger = base.Logger;

        BanMods = Config.Bind(
            Name,
            "ban_mods",
            "",
            "The Guid list of mods to be banned. Supports delimiters: \",\" \";\" \"|\" \"、\" \"，\" space tab newline." +
            "\nFor example: com.gouxi.gouxisfunnyshit, org.explosivehydra.lazyshooting");

        if (IsMpActive)
        {
            Logger.LogInfo("Tick Happy Mod: Multiplayer mode detected.");

            foreach (var banMod in BanModsList.Where(m => Chainloader.PluginInfos.ContainsKey(m)))
            {
                Logger.LogWarning($"Tick Happy Mod: Banned mod '{banMod}' is loaded!");
            }

            NetPlayer.OnPlayerJoined += OnPlayerJoined;
        }
        else
        {
            var bannedMods = BanModsList.Where(m => Chainloader.PluginInfos.ContainsKey(m)).ToList();
            foreach (var banMod in bannedMods)
            {
                Logger.LogInfo($"{banMod} has been banned!");
            }
            if (bannedMods.Count > 0)
            {
                Application.Quit();
            }
        }

        _harmony.PatchAll();
    }

    private void Update()
    {
        if (_registered || !IsMpActive || !Net.is_server || !Net.running) return;
        RegisterHandler();
    }

    private static void RegisterHandler()
    {
        try
        {
            // Access the internal SERVER_MESSAGE_HANDLERS dictionary
            var field = AccessTools.Field(typeof(Net), "SERVER_MESSAGE_HANDLERS");
            if (field == null)
            {
                // Try alternative field names
                field = AccessTools.Field(typeof(Net), "_SERVER_MESSAGE_HANDLERS");
            }
            if (field == null)
            {
                Logger.LogWarning("Tick Happy Mod: SERVER_MESSAGE_HANDLERS field not found.");
                return;
            }

            var dict = field.GetValue(null) as IDictionary;
            if (dict == null)
            {
                Logger.LogWarning("Tick Happy Mod: SERVER_MESSAGE_HANDLERS is null.");
                return;
            }

            // Create delegate matching KrokoshaScavMultiplayer.KrokoshaHandleNamedMessageDelegate
            var handlerType = typeof(KrokoshaScavMultiplayer).GetNestedType("KrokoshaHandleNamedMessageDelegate",
                BindingFlags.Public | BindingFlags.NonPublic);
            if (handlerType == null)
            {
                Logger.LogWarning("Tick Happy Mod: KrokoshaHandleNamedMessageDelegate not found.");
                return;
            }

            var ourMethod = typeof(Plugin).GetMethod(nameof(OnModReport),
                BindingFlags.NonPublic | BindingFlags.Static);
            var handler = Delegate.CreateDelegate(handlerType, ourMethod);

            dict[NetMsgId] = handler;
            _registered = true;
            Logger.LogInfo("Tick Happy Mod: Handler registered in SERVER_MESSAGE_HANDLERS.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Tick Happy Mod: Register error: {ex.Message}");
        }
    }

    private static void OnPlayerJoined(NetPlayer player)
    {
        if (player.is_local && Net.is_client)
        {
            Logger.LogInfo("Tick Happy Mod: Reporting mod list to server...");
            var modList = string.Join(",", Chainloader.PluginInfos.Keys);
            var writer = Net.CreateWriter(NetMsgId);
            writer.Put(modList);
            Net.Client_Send((DeliveryMethod)2, writer);
            return;
        }

        if (!Net.is_server || player.is_host || player.is_local) return;

        Logger.LogInfo($"Tick Happy Mod: Player '{player.playername}' (clientId={player.clientId}) joined.");
    }

    /// <summary>
    /// Called on the server when a client sends a mod report.
    /// Signature matches KrokoshaHandleNamedMessageDelegate (knetid, ref NetDataReader).
    /// </summary>
    private static void OnModReport(knetid clientId, ref NetDataReader reader)
    {
        var modListCsv = reader.GetString();
        var mods = modListCsv.Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(m => m.Trim()).Where(m => m.Length > 0).ToList();

        Logger.LogInfo($"Tick Happy Mod: Mod report from clientId={clientId}: {mods.Count} mod(s).");

        var banned = mods.Where(m => BanModsList.Contains(m)).ToList();
        if (banned.Count <= 0)
        {
            Logger.LogInfo($"Tick Happy Mod: Client (clientId={clientId}) mods clean.");
            return;
        }

        var reason = $"Kicked: banned mod(s): {string.Join(", ", banned)}";
        Logger.LogInfo($"Tick Happy Mod: {reason}");
        Net.Server_Kick(clientId, reason);
    }
}
