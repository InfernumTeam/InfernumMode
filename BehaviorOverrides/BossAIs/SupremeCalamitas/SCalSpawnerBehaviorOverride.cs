using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Skies;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using SCalBoss = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SCalSpawnerBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ModContent.ProjectileType<SCalRitualDrama>();

        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectileAI;

        public override bool PreAI(Projectile projectile)
        {
            ref float time = ref projectile.ai[0];

            // If needed, these effects may continue after the ritual timer, to ensure that there are no awkward
            // background changes between the time it takes for SCal to appear after this projectile is gone.
            // If SCal is already present, this does not happen.
            if (!NPC.AnyNPCs(ModContent.NPCType<SCalBoss>()))
            {
                SCalSky.OverridingIntensity = Utils.InverseLerp(90f, SCalRitualDrama.TotalRitualTime - 25f, time, true);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.InverseLerp(90f, SCalRitualDrama.TotalRitualTime - 25f, time, true);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower *= Utils.InverseLerp(3400f, 1560f, Main.LocalPlayer.Distance(projectile.Center), true) * 4f;
            }

            // Summon SCal right before the ritual effect ends.
            // The projectile lingers a little longer, however, to ensure that desync delays in MP do not interfere with the background transition.
            if (time == SCalRitualDrama.TotalRitualTime - 1f)
                SummonSCal(projectile);

            if (time >= SCalRitualDrama.TotalRitualTime)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && !NPC.AnyNPCs(ModContent.NPCType<SCalBoss>()))
                    projectile.Kill();
                return false;
            }

            int fireReleaseRate = time > 150f ? 2 : 1;
            for (int i = 0; i < fireReleaseRate; i++)
            {
                if (Main.rand.NextBool(4))
                {
                    Dust brimstone = Dust.NewDustPerfect(projectile.Center + new Vector2(Main.rand.NextFloat(-20f, 24f), Main.rand.NextFloat(10f, 18f)), 267);
                    brimstone.scale = Main.rand.NextFloat(0.7f, 1f);
                    brimstone.color = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat());
                    brimstone.fadeIn = 0.7f;
                    brimstone.velocity = -Vector2.UnitY * Main.rand.NextFloat(1.5f, 2.8f);
                    brimstone.noGravity = true;
                }
            }

            time++;
            return false;
        }

        public static void SummonSCal(Projectile projectile)
        {
            // Summon SCal serverside.
            // All the other acoustic and visual effects can happen client-side.
            if (Main.netMode != NetmodeID.MultiplayerClient)
                CalamityUtils.SpawnBossBetter(projectile.Center - new Vector2(60f), ModContent.NPCType<SCalBoss>());

            // Make a sudden screen shake.
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.InverseLerp(3400f, 1560f, Main.LocalPlayer.Distance(projectile.Center), true) * 16f;

            // Generate a dust explosion at the ritual's position.
            float burstDirectionVariance = 3;
            float burstSpeed = 14f;
            for (int j = 0; j < 16; j++)
            {
                burstDirectionVariance += j * 2;
                for (int k = 0; k < 40; k++)
                {
                    Dust burstDust = Dust.NewDustPerfect(projectile.Center, (int)CalamityDusts.Brimstone);
                    burstDust.scale = Main.rand.NextFloat(3.1f, 3.5f);
                    burstDust.position += Main.rand.NextVector2Circular(10f, 10f);
                    burstDust.velocity = Main.rand.NextVector2Square(-burstDirectionVariance, burstDirectionVariance).SafeNormalize(Vector2.UnitY) * burstSpeed;
                    burstDust.noGravity = true;
                }
                burstSpeed += 1.8f;
            }
        }
    }
}
