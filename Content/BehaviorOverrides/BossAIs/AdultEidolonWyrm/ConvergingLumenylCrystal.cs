using CalamityMod.Particles;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class ConvergingLumenylCrystal : ModProjectile
    {
        public Vector2 ConvergenceCenter
        {
            get;
            set;
        }

        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Divine Lumenyl");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 360;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WritePackedVector2(ConvergenceCenter);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ConvergenceCenter = reader.ReadPackedVector2();
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Time, true) * Utils.GetLerpValue(46f, 448f, Projectile.Distance(ConvergenceCenter), true);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            // Release magic particles if close to invisible.
            if (Projectile.Opacity < 0.85f && Main.rand.NextFloat() < 1f - Projectile.Opacity)
            {
                Color magicColor = Color.Lerp(Color.Blue, Color.Yellow, Main.rand.NextFloat(0.8f));
                Vector2 magicVelocity = Main.rand.NextVector2Circular(2f, 2f) * (1f - Projectile.Opacity);
                if (Main.rand.NextBool())
                {
                    SquishyLightParticle magic = new(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), magicVelocity, 0.4f, magicColor, 36);
                    GeneralParticleHandler.SpawnParticle(magic);
                }
                else
                {
                    Dust magic = Dust.NewDustPerfect(Projectile.Center, 267, magicVelocity, 0, magicColor, Main.rand.NextFloat(1f, 1.4f));
                    magic.noGravity = true;
                }
            }

            if (Projectile.WithinRange(ConvergenceCenter, 45f))
                Projectile.Kill();

            Time++;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.9f;

        public override Color? GetAlpha(Color lightColor) => new Color(255, 108, 50, 0) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, drawPosition, frame, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, 0, 0));
            
            return false;
        }
    }
}
