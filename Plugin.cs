using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace TickHappyMod;

[BepInPlugin(Guid, Name, Version)]
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
            ?.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList() ?? [];

    public void Awake()
    {
        Logger = base.Logger;

        BanMods = Config.Bind(
            Name,
            "ban_mods",
            "",
            "The Guid list of mods to be banned can be divided in three ways: \",\" \", \" \"\nFor example: com.gouxi.gouxisfunnyshit, org.explosivehydra.lazyshooting");
        
        _harmony.PatchAll();
    }
}
