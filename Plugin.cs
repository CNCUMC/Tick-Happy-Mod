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
    public static ConfigEntry<bool> RequireThm;

    public static List<string> BanModsList =>
        BanMods?.Value
            ?.Split([',', ' ', ';', '|', '、', '，'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList() ?? [];

    private static bool IsMpActive =>
        Chainloader.PluginInfos.ContainsKey("KrokoshaCasualtiesMP") &&
        PlayerPrefs.GetInt("KrokoshaCasualtiesMP_FORCE_DISABLE_MP_MOD", 0) == 0;

    private const ushort NetMsgId = 33993;
    private static bool _registered;
    private static readonly Dictionary<ushort, float> _pendingReports = new();
    private const float ReportTimeout = 15f;

    public void Awake()
    {
        Logger = base.Logger;

        BanMods = Config.Bind(
            Name,
            "ban_mods",
            "",
            "The Guid list of mods to be banned. Supports delimiters: \",\" \";\" \"|\" \"、\" \"，\" space tab newline." +
            "\nFor example: com.gouxi.gouxisfunnyshit, org.explosivehydra.lazyshooting");

        RequireThm = Config.Bind(
            Name,
            "require_thm",
            false,
            "When true, the server will kick any player who does NOT have Tick Happy Mod installed.");

        if (IsMpActive)
        {
            Logger.LogInfo("Multiplayer mode detected.");

            foreach (var banMod in BanModsList.Where(m => Chainloader.PluginInfos.ContainsKey(m)))
            {
                Logger.LogWarning($"Banned mod '{banMod}' is loaded!");
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
        if (!IsMpActive || !Net.is_server) return;
        if (!_registered && Net.running)
            RegisterHandler();
        if (!_registered) return;
        if (!RequireThm.Value) return;

        var now = Time.unscaledTime;
        foreach (var kv in _pendingReports.ToList().Where(kv => !(now - kv.Value <= ReportTimeout)))
        {
            var msg = BuildKickMessage(required: true);
            Logger.LogInfo($"Player clientId={kv.Key} timed out (no THM). Kicking: {msg}");

            try
            {
                var writer = Net.CreateWriter(30006);
                writer.Put(msg);
                writer.Put(true);
                Net.Server_SendTo((DeliveryMethod)2, writer, new knetid(kv.Key));
            }
            catch
            {
                // ignored
            }

            Net.Server_Kick(new knetid(kv.Key), msg);
            _pendingReports.Remove(kv.Key);
        }
    }

    private static void RegisterHandler()
    {
        try
        {
            var field = AccessTools.Field(typeof(Net), "SERVER_MESSAGE_HANDLERS");
            if (field == null)
            {
                field = AccessTools.Field(typeof(Net), "_SERVER_MESSAGE_HANDLERS");
            }

            if (field == null)
            {
                Logger.LogWarning("SERVER_MESSAGE_HANDLERS field not found.");
                return;
            }

            if (field.GetValue(null) is not IDictionary dict)
            {
                Logger.LogWarning("SERVER_MESSAGE_HANDLERS is null.");
                return;
            }

            var handlerType = typeof(KrokoshaScavMultiplayer).GetNestedType("KrokoshaHandleNamedMessageDelegate",
                BindingFlags.Public | BindingFlags.NonPublic);
            if (handlerType == null)
            {
                Logger.LogWarning("KrokoshaHandleNamedMessageDelegate not found.");
                return;
            }

            var ourMethod = typeof(Plugin).GetMethod(nameof(OnModReport),
                BindingFlags.NonPublic | BindingFlags.Static);
            var handler = Delegate.CreateDelegate(handlerType, ourMethod);

            dict[NetMsgId] = handler;
            _registered = true;
            Logger.LogInfo("Handler registered in SERVER_MESSAGE_HANDLERS.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Register error: {ex.Message}");
        }
    }

    private static void OnPlayerJoined(NetPlayer player)
    {
        if (player.is_local && Net.is_client)
        {
            Logger.LogInfo("Reporting mod list to server...");
            var modList = string.Join(",", Chainloader.PluginInfos.Keys);
            var writer = Net.CreateWriter(NetMsgId);
            writer.Put(modList);
            Net.Client_Send((DeliveryMethod)2, writer);
            return;
        }

        if (!Net.is_server || player.is_host || player.is_local) return;

        Logger.LogInfo($"Player '{player.playername}' (clientId={player.clientId}) joined.");

        if (RequireThm.Value)
        {
            _pendingReports[player.clientId] = Time.unscaledTime;
        }
    }

    private static string BuildKickMessage(bool required = false, List<string> banned = null)
    {
        var parts = new List<string>();
        if (required)
            parts.Add("this server requires Tick Happy Mod");
        if (banned is { Count: > 0 })
            parts.Add($"banned mods: {string.Join(", ", banned)}");
        return "Kicked: " + string.Join(" | ", parts);
    }

    private static void OnModReport(knetid clientId, ref NetDataReader reader)
    {
        var ushortId = (ushort)clientId;
        _pendingReports.Remove(ushortId);

        var modListCsv = reader.GetString();
        var mods = modListCsv.Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(m => m.Trim()).Where(m => m.Length > 0).ToList();

        Logger.LogInfo($"Mod report from clientId={clientId}: {mods.Count} mod(s).");

        var banned = mods.Where(m => BanModsList.Contains(m)).ToList();
        if (banned.Count <= 0)
        {
            Logger.LogInfo($"Client (clientId={clientId}) mods clean.");
            return;
        }

        var msg = BuildKickMessage(banned: banned);
        Logger.LogInfo($"{msg}");

        try
        {
            var player = NetPlayer.GetNetPlayerFromClientId(clientId);
            if (player != null)
            {
                player.Server_DoAlertSingle(msg);
            }
        }
        catch
        {
            // ignored
        }

        Net.Server_Kick(clientId, msg);
    }
}