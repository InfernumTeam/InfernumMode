using CalamityMod.NPCs.Providence;
using CalamityMod.World;
using InfernumMode.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using InfernumMode.Netcode;
using InfernumMode.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class PreEntityUpdateSystem : ModSystem
    {
        public override void PreUpdateEntities()
        {
            InfernumMode.BlackFade = MathHelper.Clamp(InfernumMode.BlackFade - 0.025f, 0f, 1f);
            PacketHandler.Update();
            TwinsAttackSynchronizer.DoUniversalUpdate();
            TwinsAttackSynchronizer.PostUpdateEffects();
            if (CalamityWorld.death)
                CalamityWorld.revenge = true;

            bool arenaShouldApply = Utilities.AnyProjectiles(ModContent.ProjectileType<ProvidenceSummonerProjectile>()) || NPC.AnyNPCs(ModContent.NPCType<Providence>());
            InfernumMode.ProvidenceArenaTimer = MathHelper.Clamp(InfernumMode.ProvidenceArenaTimer + arenaShouldApply.ToDirectionInt(), 0f, 120f);
            if (Main.netMode != NetmodeID.MultiplayerClient && InfernumMode.ProvidenceArenaTimer > 0 && !Utilities.AnyProjectiles(ModContent.ProjectileType<ProvidenceArenaBorder>()))
                Utilities.NewProjectileBetter(Vector2.One * 9999f, Vector2.Zero, ModContent.ProjectileType<ProvidenceArenaBorder>(), 0, 0f);
        }
    }
}