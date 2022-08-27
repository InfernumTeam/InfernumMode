using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.StormWeaver
{
    public class StormWeaveCloud : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public ref float Variant => ref Projectile.ai[1];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Cloud");

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 420;
            Projectile.Opacity = 0f;
            Projectile.scale = 0.01f;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(300f, 285f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 35f, Projectile.timeLeft, true);
            Projectile.scale = MathHelper.Clamp(Projectile.Opacity + 0.065f, 0f, 1f);

            if (Variant == 0f)
            {
                Variant = Main.rand.Next(4) + 1f;
                switch ((int)Variant)
                {
                    case 1:
                        Projectile.Size = new Vector2(530f, 218f);
                        break;
                    case 2:
                        Projectile.Size = new Vector2(372f, 132f);
                        break;
                    case 3:
                        Projectile.Size = new Vector2(296f, 116f);
                        break;
                    case 4:
                        Projectile.Size = new Vector2(226f, 68f);
                        break;
                }

                Projectile.netUpdate = true;
            }

            Projectile.velocity = Projectile.velocity.MoveTowards(Vector2.Zero, 0.04f) * 0.985f;

            if (Time > 60f)
            {
                for (int i = 0; i < Projectile.width / 105f; i++)
                {
                    if (!Main.rand.NextBool(104))
                        continue;

                    SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap, Projectile.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 sparkVelocity = Vector2.UnitY * Main.rand.NextFloat(12f, 16f);
                        Vector2 sparkSpawnPosition = Projectile.Bottom + new Vector2(Main.rand.NextFloatDirection() * Projectile.width * 0.45f, Main.rand.NextFloat(-8f, 0f));
                        Utilities.NewProjectileBetter(sparkSpawnPosition, sparkVelocity, ModContent.ProjectileType<WeaverSpark2>(), 255, 0f);
                        Utilities.NewProjectileBetter(sparkSpawnPosition, -sparkVelocity, ModContent.ProjectileType<WeaverSpark2>(), 255, 0f);
                    }
                }
            }

            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.Lerp(lightColor, Color.Black, Utils.GetLerpValue(0f, 25f, Time, true) * 0.45f) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Variant is <= 0f or > 4f)
                return false;

            Texture2D texture = ModContent.Request<Texture2D>($"InfernumMode/BehaviorOverrides/BossAIs/StormWeaver/StormWeaveCloud{(int)Variant}").Value;
            Vector2 origin = texture.Size() * 0.5f;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color frontAfterimageColor = Projectile.GetAlpha(Color.Lerp(lightColor, Color.Cyan, 0.8f)) * 0.25f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2();
                drawOffset *= MathHelper.Lerp(-1f, 8f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.3f) * 0.5f + 0.5f);

                Vector2 afterimageDrawPosition = drawPosition + drawOffset;
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, null, frontAfterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
