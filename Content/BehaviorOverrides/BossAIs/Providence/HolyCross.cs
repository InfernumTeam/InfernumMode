using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Provi = CalamityMod.NPCs.Providence.Providence;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class HolyCross : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public const int PhaseThroughTilesTime = 150;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Holy Symbol");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.velocity.Length() < 22f && Time >= 25f)
                Projectile.velocity *= 1.024f;

            Projectile.frameCounter++;
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;

            // Dissipate into ashes if inside of a wall.
            if (Time >= PhaseThroughTilesTime && Collision.SolidCollision(Projectile.TopLeft, Projectile.width, Projectile.height))
            {
                // Release ashes.
                int ashCount = (int)Lerp(8f, 2f, Projectile.Opacity);
                for (int i = 0; i < ashCount; i++)
                {
                    Color startingColor = Color.Lerp(Color.Orange, Color.Gray, Main.rand.NextFloat(0.5f, 0.8f));
                    MediumMistParticle ash = new(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextVector2Circular(2f, 2f), startingColor, Color.DarkGray, Projectile.Opacity * 0.4f, 255f, Main.rand.NextFloatDirection() * 0.014f);
                    GeneralParticleHandler.SpawnParticle(ash);
                }

                Projectile.Opacity = Clamp(Projectile.Opacity - 0.085f, 0f, 1f);
                if (Projectile.Opacity <= 0f)
                    Projectile.Kill();
            }

            Time++;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust holyFire = Dust.NewDustPerfect(Projectile.Center, (int)CalamityDusts.ProfanedFire);
                holyFire.velocity = Main.rand.NextVector2Circular(14f, 14f);
                holyFire.scale = 1.7f;
                holyFire.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.White, 0.4f);
            lightColor.A /= 2;

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            if (NPC.AnyNPCs(ModContent.NPCType<Provi>()) && ProvidenceBehaviorOverride.IsEnraged)
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/HolyCrossNight").Value;

            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1, texture);
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 128 }, Color.White, 3f, null, texture);
            return false;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.67f;
    }
}
