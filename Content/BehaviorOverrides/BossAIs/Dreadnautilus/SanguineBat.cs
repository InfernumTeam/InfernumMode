using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Dreadnautilus
{
    public class SanguineBat : ModProjectile
    {
        public Player Target => Main.player[Projectile.owner];
        public ref float Time => ref Projectile.ai[0];
        public ref float LocalLifetime => ref Projectile.ai[1];

        public const int Lifetime = 420;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sanguine Bat");
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.timeLeft = 3600;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (!Projectile.WithinRange(Target.Center, 50f))
            {
                float flySpeed = Utils.GetLerpValue(LocalLifetime, LocalLifetime - 70f, Time, true) * Utils.GetLerpValue(0f, 120f, Time, true) * 17f;
                Vector2 destinationOffset = (MathHelper.TwoPi * Projectile.identity / 13f).ToRotationVector2() * 18f;
                Projectile.velocity = (Projectile.velocity * 35f + Projectile.SafeDirectionTo(Target.Center + destinationOffset) * flySpeed) / 36f;
            }
            else if (Time >= LocalLifetime - 75f)
                Projectile.velocity *= 0.96f;

            // Prevent bats from bundling together.
            Projectile.MinionAntiClump(0.7f);

            // Determine rotation.
            Projectile.rotation = Math.Abs(Projectile.velocity.X * 0.04f);
            Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            // Emit light.
            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3() * 0.4f);

            Time++;
            if (Time >= LocalLifetime && LocalLifetime > 0f)
                Projectile.Kill();
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && !Projectile.WithinRange(Target.Center, 150f) && Projectile.identity % 3 == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 boltShootVelocity = Projectile.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.TwoPi * i / 3f) * 2.25f;
                    Utilities.NewProjectileBetter(Projectile.Center, boltShootVelocity, ModContent.ProjectileType<BloodBolt>(), 120, 0f);
                }
            }

            for (int i = 0; i < 12; i++)
            {
                Dust blood = Dust.NewDustPerfect(Projectile.Center, 267);
                blood.velocity = Main.rand.NextVector2Circular(4f, 4f);
                blood.scale = Main.rand.NextFloat(1f, 1.5f);
                blood.color = Color.Red;
                blood.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Color afterimageColor = Color.Crimson;
            afterimageColor.A = 120;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            SpriteEffects direction = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
                direction = SpriteEffects.FlipHorizontally;

            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (Projectile.rotation + MathHelper.TwoPi * i / 4f).ToRotationVector2() * 2f;
                Main.EntitySpriteDraw(texture, drawPosition + drawOffset, frame, afterimageColor, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, direction, 0);
            }
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type]);

            return false;
        }
    }
}
