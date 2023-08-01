using CalamityMod.Particles;
using InfernumMode.Content.Items.Weapons.Ranged;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Ranged
{
    public class GlassmakerFire : ModProjectile
    {
        public ref float Timer => ref Projectile.ai[0];

        public const int Lifetime = 150;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Glass Flames");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 3;
            Projectile.MaxUpdates = 7;
            Projectile.timeLeft = Lifetime;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 8;
        }

        public override void AI()
        {
            // Emit light.
            Lighting.AddLight(Projectile.Center, Projectile.Opacity * 0.3f, Projectile.Opacity * 0.3f, Projectile.Opacity * 0.03f);

            // Emit fire particles.
            float lifetimeInterpolant = Timer / Lifetime;
            float particleScale = Lerp(0.03f, 1.2f, Pow(lifetimeInterpolant, 0.53f));
            float opacity = Utils.GetLerpValue(0.96f, 0.7f, lifetimeInterpolant, true);
            float fadeToBlack = Utils.GetLerpValue(0.5f, 0.84f, lifetimeInterpolant, true);

            // Start with a random color between yellow and red. The variance from this leads to a pseudo-gradient look.
            Color fireColor = Color.Lerp(Color.Yellow, Color.Red, Main.rand.NextFloat(0.2f, 0.8f));

            // Have the fire color dissipate into smoke as it reaches death.
            fireColor = Color.Lerp(fireColor, Color.DarkGray, fadeToBlack);

            // Use a blue flame at the start of the flame's life, indicating extraordinary quantities of heat.
            fireColor = Color.Lerp(fireColor, Color.DeepSkyBlue, Utils.GetLerpValue(0.29f, 0f, lifetimeInterpolant, true));

            // Emit light.
            Lighting.AddLight(Projectile.Center, fireColor.ToVector3() * opacity);

            var particle = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.4f, 0.4f), fireColor, 30, particleScale, opacity, 0.05f, Main.rand.NextFloat() > Math.Pow(fadeToBlack, 0.2), 0f, true);
            GeneralParticleHandler.SpawnParticle(particle);

            // Randomly emit glass particles.
            if (Main.rand.NextBool(6) && lifetimeInterpolant < 0.33f)
            {
                Dust glass = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), 13);
                glass.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.6f) * 4f + Main.rand.NextVector2Circular(2f, 2f);
                glass.noGravity = true;
            }

            if (TheGlassmaker.TransformsSandIntoGlass)
            {
                for (int dx = -2; dx < 2; dx++)
                {
                    for (int dy = -2; dy < 2; dy++)
                    {
                        Point p = new((int)Projectile.Center.X / 16 + dx, (int)Projectile.Center.Y / 16 + dy);
                        int tileType = Main.tile[p].TileType;
                        if (TileID.Sets.isDesertBiomeSand[tileType] || tileType == TileID.Pearlsand)
                            Main.tile[p].TileType = TileID.Glass;
                    }
                }
            }

            Timer++;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire, 160);

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (info.PvP)
                target.AddBuff(BuffID.OnFire, 160);
        }
    }
}
