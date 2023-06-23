using CalamityMod.NPCs;
using CalamityMod.NPCs.Polterghast;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Polterghast
{
    public class PolterghastCloneBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PolterPhantom>();

        public override bool PreAI(NPC npc)
        {
            npc.scale = 0.7f;

            // Disappear if the main boss is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.ghostBoss))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            NPC polterghast = Main.npc[CalamityGlobalNPC.ghostBoss];
            Player target = Main.player[polterghast.target];

            // Explode into souls if the main boss is done performing the attack that involves clones.
            if (polterghast.ai[0] != (int)PolterghastBehaviorOverride.PolterghastAttackType.CloneSplit)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int j = 0; j < 18; j++)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(npc.Center).RotatedByRandom(0.4f) * Main.rand.NextFloat(15f, 20f);

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(soul => soul.timeLeft = 20);
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<NotSpecialSoul>(), 0, 0f);
                    }
                }
                npc.active = false;
            }

            // Manually set the number of souls released by Polter.
            // These will be brought back once these clones disappear.
            polterghast.ai[2] = 54f;
            polterghast.scale = 0.85f;

            // Inherit polterghast's hit sound.
            npc.HitSound = polterghast.HitSound;

            float attackTimer = polterghast.Infernum().ExtraAI[2];

            if (polterghast.Infernum().ExtraAI[3] == 1f)
            {
                npc.damage = npc.defDamage;
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * polterghast.velocity.Length();
                npc.rotation = npc.velocity.ToRotation() + PiOver2;
            }
            else
            {
                npc.damage = 0;
                npc.rotation = npc.AngleTo(target.Center) + PiOver2;
                if (attackTimer < 40f && !npc.WithinRange(target.Center, 300f))
                {
                    Vector2 destination = target.Center - Vector2.UnitY * 300f;
                    destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 240f;

                    npc.velocity = (npc.velocity * 15f + npc.SafeDirectionTo(destination) * 18f) / 16f;
                }
                else
                    npc.velocity *= 0.97f;
            }
            npc.Opacity = 1f;

            return false;
        }
    }
}
