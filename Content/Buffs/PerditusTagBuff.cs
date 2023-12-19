using InfernumMode.Core.GlobalInstances;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class PerditusTagBuff : ModBuff
    {
        public const int TagDamage = 23;

        public const int CritChance = 10;

        public override void SetStaticDefaults() => BuffID.Sets.IsATagBuff[Type] = true;

        public override void Load()
        {
            GlobalNPCOverrides.ModifyHitByProjectileEvent += WhipTagStuff;
        }

        private void WhipTagStuff(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.npcProj || projectile.trap || !projectile.IsMinionOrSentryRelated)
                return;

            // SummonTagDamageMultiplier scales down tag damage for some specific minion and sentry projectiles for balance purposes.
            var projTagMultiplier = ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
            if (!npc.HasBuff<PerditusTagBuff>())
                return;

            modifiers.FlatBonusDamage += TagDamage * projTagMultiplier;

            if (Main.rand.Next(0, 101) < CritChance)
                modifiers.SetCrit();
        }
    }
}
