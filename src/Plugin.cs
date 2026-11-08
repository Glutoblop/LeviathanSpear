using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace LeviathanSpear
{
    // Client-only. Vanilla spear throw (assembly_valheim):
    //   Attack (secondary, projectile) -> Projectile.Setup(): m_respawnItemOnHit => m_spawnItem = the thrown spear
    //   Projectile.SpawnOnHit()        -> ItemDrop.DropItem(m_spawnItem, 1, pos, rot) when it lands / hits / times out
    // The projectile is simulated by its owner (the thrower), so SpawnOnHit runs on our client. We wrap it and
    // grab the ItemDrop that DropItem returns, then:
    //   - draw the spear's icon over it, with a radial countdown ring, while we're within Radius metres;
    //   - once the ring completes, starting a dodge roll within Radius pulls it into an empty hotbar slot
    //     (same ownership + inventory steps as Humanoid.Pickup).
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "jordan.valheim.leviathanspear";
        public const string Name = "Leviathan Spear";
        public const string Version = "0.1.1";

        internal static Plugin Instance;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> Radius;
        internal static ConfigEntry<float> RecallDelay;
        internal static ConfigEntry<bool> RecallEnabled;
        internal static ConfigEntry<float> IconSize;
        internal static ConfigEntry<float> IconHeight;
        internal static ConfigEntry<Color> RingColor;
        internal static ConfigEntry<Color> RingReadyColor;
        internal static ConfigEntry<Color> RingBackgroundColor;

        private SpearTracker _tracker;
        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;

            Enabled = Config.Bind("General", "Enabled", true,
                "Show an icon over spears you've thrown, and let you recall them with a dodge roll.");
            Radius = Config.Bind("General", "Radius", 20f,
                new ConfigDescription("How close (metres) you must be to a thrown spear to see its icon and to recall it.",
                    new AcceptableValueRange<float>(1f, 100f)));
            RecallDelay = Config.Bind("Recall", "Seconds", 10f,
                new ConfigDescription("How long after a spear lands before it can be recalled (the ring's countdown).",
                    new AcceptableValueRange<float>(0f, 120f)));
            RecallEnabled = Config.Bind("Recall", "Enabled", true,
                "Dodge roll within Radius of a ready spear to return it to an empty hotbar slot.");

            IconSize = Config.Bind("Look", "IconSize", 48f,
                new ConfigDescription("Icon size in pixels.", new AcceptableValueRange<float>(16f, 256f)));
            IconHeight = Config.Bind("Look", "HeightAboveSpear", 0.6f,
                new ConfigDescription("How far above the spear the icon floats, in metres.", new AcceptableValueRange<float>(0f, 5f)));
            RingColor = Config.Bind("Look", "RingColor", new Color(1f, 0.8f, 0.35f, 0.95f),
                "Colour of the countdown ring while it fills.");
            RingReadyColor = Config.Bind("Look", "RingReadyColor", new Color(0.45f, 1f, 0.45f, 1f),
                "Colour of the ring once the spear can be recalled.");
            RingBackgroundColor = Config.Bind("Look", "RingBackgroundColor", new Color(0f, 0f, 0f, 0.55f),
                "Colour of the ring's track behind the countdown.");

            _tracker = gameObject.AddComponent<SpearTracker>();

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            if (_tracker) Destroy(_tracker);
        }

        internal static void Log(string msg) => Instance?.Logger.LogInfo(msg);
        internal static void Warn(string msg) => Instance?.Logger.LogWarning(msg);
    }
}
