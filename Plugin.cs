using BepInEx;
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

    public void Awake()
    {
        Logger = base.Logger;
        _harmony.PatchAll();

        Logger.LogInfo("Tick Happy Mod loaded!");
    }
}