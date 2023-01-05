using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace InfernumMode.BehaviorOverrides.BossAIs.HiveMind
{
    public class ShadeFireColumn : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];

        public const int BurstDelay = 67;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire");
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 250;
            Projectile.tileCollide = false;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Projectile.Opacity * 0.15f, 0f, Projectile.Opacity * 0.2f);

            if (Time > 7f)
            {
                if (Time >= BurstDelay)
                {
                    if (Projectile.extraUpdates != 4)
                        Projectile.extraUpdates = 4;

                    Projectile.velocity = Vector2.UnitY * -4.45f;
                }
                float scale = 1f;
                if (Time == 8f)
                    scale = 0.25f;
                else if (Time == 9f)
                    scale = 0.5f;
                else if (Time == 10f)
                    scale = 0.75f;

                if (Main.rand.NextBool())
                {
                    for (int i = 0; i < (Time >= BurstDelay ? 4 : 10); i++)
                    {
                        for (float yOffset = 0f; yOffset < (Time > 69f ? 100f : 1f); yOffset += 15f)
                        {
                            int dustType = i % 2 == 0 ? 157 : 14;
                            Dust fire = Dust.NewDustDirect(Projectile.position + Vector2.UnitY * yOffset, Projectile.width, Projectile.height, dustType, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1f);
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
                            fire.velocity += Projectile.velocity;
                            if (!fire.noGravity)
                                fire.velocity *= 0.5f;
                        }
                    }
                }
            }

            Time++;
        }

        public override bool? CanDamage() => Time >= BurstDelay;
    }
}
