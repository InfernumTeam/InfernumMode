using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas.SupremeCalamitasBehaviorOverride;
using SCalNPC = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SoulSeekerSupremeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SoulSeekerSupreme>();

        public override bool PreAI(NPC npc)
        {
            // Die if SCal is no longer present.
            if (CalamityGlobalNPC.SCal < 0 || !SCal.active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            npc.target = SCal.target;

            bool outerSeeker = npc.ai[2] == 1f;
            bool canFire = SCal.Infernum().ExtraAI[1] == 1f;
            Player target = Main.player[npc.target];
            Vector2 eyePosition = npc.Center + new Vector2(npc.spriteDirection == -1 ? 40f : -36f, 16f);
            ref float spinOffsetAngle = ref npc.ai[1];
            ref float attackTimer = ref npc.Infernum().ExtraAI[0];

            // Initialize the turn rotation.
            if (npc.localAI[0] == 0f)
            {
                for (int i = 0; i < 10; i++)
                    Dust.NewDust(npc.position, npc.width, npc.height, (int)CalamityDusts.Brimstone, 0f, 0f, 100, default, 2f);
                npc.ai[1] = npc.ai[0];
                npc.localAI[0] = 1f;
            }

            // Increase DR if the target leaves SCal's arena.
            npc.Calamity().DR = SoulSeekerSupreme.NormalDR;
            if (Enraged)
                npc.Calamity().DR = 0.99999f;

            // Pick a target if the current one is invalid.
            if (npc.target < 0 || npc.target == Main.maxPlayers || target.dead || !target.active)
                npc.TargetClosest();

            // Pick a target if the current one is too far away.
            if (!npc.WithinRange(target.Center, CalamityGlobalNPC.CatchUpDistance200Tiles))
                npc.TargetClosest();

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

            // Disable natural knockback resistence. Apparently this is something that Calamity never disabled?
            npc.knockBackResist = 0f;

            // Spin around SCal's arena.
            Vector2 arenaCenter = SCal.Infernum().Arena.Center.ToVector2();
            npc.Center = arenaCenter - MathHelper.ToRadians(spinOffsetAngle).ToRotationVector2() * (outerSeeker ? 1000f : 500f);

            // Begin to disappear if SCal isn't doing the seekers attack anymore.
            npc.dontTakeDamage = false;
            if (SCal.ai[0] != (int)SCalAttackType.SummonSeekers)
            {
                npc.Opacity -= 0.1f;
                if (npc.Opacity <= 0f)
                    npc.active = false;

                npc.dontTakeDamage = true;
                return false;
            }

            // Spin around.
            spinOffsetAngle += outerSeeker.ToDirectionInt() * 0.5f;

            // Release semi-inaccurate bombs towards the target.
            if (canFire)
                attackTimer++;
            var n = Main.projectile;
            if (attackTimer % 136f == 135f && !target.WithinRange(npc.Center, 400f))
            {
                SoundEngine.PlaySound(SCalNPC.BrimstoneBigShotSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int bombExplodeDelay = 90;
                    float bombExplosionRadius = 600f;
                    float bombShootSpeed = outerSeeker ? 8f : npc.Distance(target.Center) * 0.012f + 9f;
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(bomb =>
                    {
                        bomb.timeLeft = bombExplodeDelay;
                        bomb.ModProjectile<DemonicBomb>().ExplodeIntoDarts = outerSeeker;
                    });
                    Vector2 bombShootVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(0.3f) * bombShootSpeed;
                    Utilities.NewProjectileBetter(npc.Center, bombShootVelocity, ModContent.ProjectileType<DemonicBomb>(), 0, 0f, -1, bombExplosionRadius);
                }
            }
            return false;
        }
    }
}