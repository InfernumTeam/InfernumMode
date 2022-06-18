using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordExplosion : ModProjectile
    {
        public ref float Countdown => ref projectile.ai[0];
        public Player Target => Main.player[projectile.owner];
        public override string Texture => "CalamityMod/ExtraTextures/XerocLight";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Explosion");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 250;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 40;
            projectile.scale = 1f;
        }

        public override void AI()
        {
            if (projectile.localAI[0] == 0f)
            {
                Main.PlaySound(Main.rand.NextBool() ? SoundID.DD2_KoboldExplosion : SoundID.DD2_ExplosiveTrapExplode, projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 cinderVelocity = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * Main.rand.NextFloat(7f, 16f) + Main.rand.NextVector2Circular(4f, 4f);
                        Utilities.NewProjectileBetter(projectile.Center + cinderVelocity * 2f, cinderVelocity, ModContent.ProjectileType<MoonLordExplosionCinder>(), 0, 0f);
                    }
                    projectile.localAI[0] = 1f;
                }
            }
            projectile.scale = projectile.timeLeft / 40f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 scale = projectile.Size / texture.Size();
            Color color = Color.Lerp(Color.Turquoise, Color.White, projectile.scale) * projectile.scale;

            spriteBatch.Draw(texture, drawPosition, null, color, 0f, texture.Size() * 0.5f, scale * (float)Math.Pow(projectile.scale, 1.5), 0, 0f);
            for (int i = 0; i < 2; i++)
            {
                float rotation = MathHelper.Lerp(-MathHelper.PiOver4, MathHelper.PiOver4, i);
                spriteBatch.Draw(texture, drawPosition, null, color, rotation, texture.Size() * 0.5f, scale * new Vector2(0.1f, 1f) * 1.45f, 0, 0f);
            }
            spriteBatch.ResetBlendState();

            return false;
        }
    }
}
