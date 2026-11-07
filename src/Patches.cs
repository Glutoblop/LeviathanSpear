using System;
using HarmonyLib;

namespace LeviathanSpear
{
    // Projectile.SpawnOnHit drops m_spawnItem via ItemDrop.DropItem. We flag the call while it's our own thrown
    // spear, and pick up the ItemDrop that DropItem returns inside it.
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.SpawnOnHit))]
    internal static class Projectile_SpawnOnHit
    {
        internal static bool Capturing;

        private static void Prefix(Projectile __instance)
        {
            Capturing = false;
            if (!Plugin.Enabled.Value) return;
            Player local = Player.m_localPlayer;
            ItemDrop.ItemData item = __instance.m_spawnItem;
            Capturing = local != null && __instance.m_owner == local && item != null
                        && item.m_shared.m_skillType == Skills.SkillType.Spears;
        }

        private static Exception Finalizer(Exception __exception)
        {
            Capturing = false;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.DropItem))]
    internal static class ItemDrop_DropItem
    {
        private static void Postfix(ItemDrop __result)
        {
            if (Projectile_SpawnOnHit.Capturing && __result != null)
                SpearTracker.Register(__result);
        }
    }
}
