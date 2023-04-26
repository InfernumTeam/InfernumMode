using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Perforator;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Perforators
{
    public class MediumPerforatorHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PerforatorHeadMedium>();

        public override bool PreAI(NPC npc)
        {
            int toothBallReleaseRate = 120;
            float toothBallShootSpeed = 9f;
            ref float attackTimer = ref npc.ai[0];

            // Create segments.
            if (npc.localAI[3] == 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    PerforatorHiveBehaviorOverride.CreateSegments(npc, 16, ModContent.NPCType<PerforatorBodyMedium>(), ModContent.NPCType<PerforatorTailMedium>());

                npc.localAI[3] = 1f;
            }

            // Fuck off if the hive is dead.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.perfHive) || !Main.npc[CalamityGlobalNPC.perfHive].active)
            {
                npc.active = false;
                return false;
            }

            npc.Calamity().SplittingWorm = false;
            npc.target = Main.npc[CalamityGlobalNPC.perfHive].target;
            Player target = Main.player[npc.target];

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);

            // Reset shit.
            npc.Calamity().DR = 0f;
            npc.dontTakeDamage = false;

            // Fly towards the target.
            float xDamp = Utils.Remap(Math.Abs(Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitX)), 0f, 1f, 0.2f, 1f);
            float yDamp = Utils.Remap(Math.Abs(Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitY)), 0f, 1f, 0.2f, 1f);
            Vector2 flyDestination = target.Center;

            float maxFlySpeed = BossRushEvent.BossRushActive ? 26f : 12.5f;
            if (npc.WithinRange(flyDestination, 400f) && npc.velocity.Length() > maxFlySpeed * 0.48f)
                npc.velocity *= 1.01f;
            else
            {
                Vector2 velocityStep = npc.SafeDirectionTo(flyDestination) * new Vector2(xDamp, yDamp) * 0.7f;
                npc.velocity = (npc.velocity + velocityStep).ClampMagnitude(0f, maxFlySpeed);
            }
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Periodically release spit balls at the player if not too close and in their line of sight.
            bool readyToFire = attackTimer % toothBallReleaseRate == toothBallReleaseRate - 1f;
            if (!npc.WithinRange(target.Center, 225f) && readyToFire)
            {
                SoundEngine.PlaySound(SoundID.NPCHit20, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 toothBallShootVelocity = npc.SafeDirectionTo(target.Center) * toothBallShootSpeed;
                    Utilities.NewProjectileBetter(npc.Center, toothBallShootVelocity, ModContent.ProjectileType<ToothBall>(), PerforatorHiveBehaviorOverride.ToothBallDamage, 0f);
                }
            }
            attackTimer++;

            return false;
        }
    }
}
