using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace InfernumMode.BehaviorOverrides.BossAIs.HiveMind
{
    public class ShadeFireColumn : ModProjectile
    {
        public ref float Time => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire");
        }

        public override void SetDefaults()
        {
            projectile.width = 6;
            projectile.height = 6;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 250;
            projectile.tileCollide = false;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Lighting.AddLight(projectile.Center, projectile.Opacity * 0.15f, 0f, projectile.Opacity * 0.2f);

            if (Time > 7f)
            {
                if (Time >= 67f)
                {
                    if (projectile.extraUpdates != 4)
                        projectile.extraUpdates = 4;

                    projectile.velocity = Vector2.UnitY * -4.45f;
                }
                float scale = 1f;
                if (Time == 8f)
                {
                    scale = 0.25f;
                }
                else if (Time == 9f)
                {
                    scale = 0.5f;
                }
                else if (Time == 10f)
                {
                    scale = 0.75f;
                }
                if (Main.rand.NextBool(2))
                {
                    for (int i = 0; i < (Time >= 67f ? 4 : 10); i++)
                    {
                        for (float yOffset = 0f; yOffset < (Time > 69f ? 100f : 1f); yOffset += 15f)
                        {
                            int dustType = i % 2 == 0 ? 157 : 14;
                            Dust fire = Dust.NewDustDirect(projectile.position + Vector2.UnitY * yOffset, projectile.width, projectile.height, dustType, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default, 1f);
                            if (Main.rand.NextBool(3))
                            {
                                fire.noGravity = true;
                                fire.scale *= 1.75f;
                                fire.velocity *= 2f;
                            }
                            else
                                fire.scale *= 0.5f;

                            fire.velocity *= 1.2f;
                            fire.scale *= scale;
                            fire.velocity += projectile.velocity;
                            if (!fire.noGravity)
                                fire.velocity *= 0.5f;
                        }
                    }
                }
            }

            Time++;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.AddBuff(BuffID.CursedInferno, 240);
            target.AddBuff(ModContent.BuffType<Shadowflame>(), 140);
            target.Calamity().lastProjectileHit = projectile;
        }

        public override bool CanDamage() => Time >= 67f;
    }
}
