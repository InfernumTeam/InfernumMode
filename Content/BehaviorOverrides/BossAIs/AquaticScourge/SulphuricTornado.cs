using InfernumMode.Assets.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class SulphuricTornado : ModProjectile
    {
        public SlotId WindSlot
        { 
            get;
            set;
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float FlyDirection => ref Projectile.ai[1];

        public static int Lifetime => 720;

        public override string Texture => "CalamityMod/Projectiles/Boss/OldDukeVortex";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sulphuric Typhoon");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.scale = 0.004f;
            Projectile.hostile = true;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.rotation -= Projectile.Opacity * 0.15f;
            Projectile.width = (int)(Projectile.scale * 208f);
            Projectile.height = (int)(Projectile.scale * 1000f);

            // Fade in and grow to the appropriate size.
            Projectile.Opacity = Utils.GetLerpValue(0f, 120f, Time, true) * Utils.GetLerpValue(0f, 45f, Projectile.timeLeft, true);
            Projectile.scale = Utils.GetLerpValue(0f, 60f, Time, true);

            // Move upward.
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Projectile.velocity.X = (float)Math.Cos(MathHelper.TwoPi * Time / 235f) * FlyDirection * Projectile.Opacity * 25f;
            Projectile.velocity.Y = -4.5f;
            Projectile.position.X += Projectile.SafeDirectionTo(target.Center).X * Projectile.Opacity * 13f;

            // Release a spray of falling acid.
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.Opacity >= 0.6f && Projectile.height >= 750f && Time % 4f == 3f)
            {
                Vector2 acidSpawnPosition = Projectile.Top + new Vector2(Main.rand.NextFloatDirection() * 100f, Main.rand.NextFloat(250f));
                Vector2 acidSpawnVelocity = -Vector2.UnitY.RotatedByRandom(1.1f) * Main.rand.NextFloat(10f, 25f);
                Utilities.NewProjectileBetter(acidSpawnPosition, acidSpawnVelocity, ModContent.ProjectileType<FallingAcid>(), 140, 0f);
            }

            // Handle sound stuff.
            if (SoundEngine.TryGetActiveSound(WindSlot, out var s))
            {
                if (s.Position != Projectile.Center && s.IsPlaying)
                    s.Position = Projectile.Center;
            }
            else
                WindSlot = SoundEngine.PlaySound(InfernumSoundRegistry.CloudElementalWindSound with { Volume = 1.5f }, Projectile.Center);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            for (float dy = 0; dy < Projectile.height; dy += 30f)
            {
                float rotation = Projectile.rotation + MathHelper.Pi * dy / Projectile.height;
                float opacity = MathHelper.Lerp(1f, 0.6f, dy / Projectile.height) * Projectile.Opacity * 0.3f;
                Vector2 drawPosition = Projectile.Bottom - Main.screenPosition - Vector2.UnitY * dy;
                Color tornadoColor = Color.White * opacity;
                tornadoColor.A /= 2;
                
                Main.EntitySpriteDraw(texture, drawPosition, null, tornadoColor, rotation, texture.Size() * 0.5f, TornadoPieceScale(dy), 0, 0);
            }
            return false;
        }

        public float TornadoPieceScale(float dy) => MathHelper.Lerp(0.3f, 1.4f, dy / Projectile.height) * Projectile.scale;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (float dy = 0; dy < Projectile.height; dy += 30f)
            {
                if (Utilities.CircularCollision(Projectile.Bottom - Vector2.UnitY * dy, targetHitbox, TornadoPieceScale(dy) * 196f))
                    return true;
            }
            return false;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.7f;
    }
}
