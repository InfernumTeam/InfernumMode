using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordExplosion : ModProjectile
    {
        public ref float Countdown => ref Projectile.ai[0];
        public Player Target => Main.player[Projectile.owner];
        public override string Texture => "CalamityMod/Skies/XerocLight";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Explosion");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 250;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 40;
            Projectile.scale = 1f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(Main.rand.NextBool() ? SoundID.DD2_KoboldExplosion : SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 cinderVelocity = (TwoPi * i / 10f).ToRotationVector2() * Main.rand.NextFloat(7f, 16f) + Main.rand.NextVector2Circular(4f, 4f);
                        Utilities.NewProjectileBetter(Projectile.Center + cinderVelocity * 2f, cinderVelocity, ModContent.ProjectileType<MoonLordExplosionCinder>(), 0, 0f);
                    }
                    Projectile.localAI[0] = 1f;
                }
            }
            Projectile.scale = Projectile.timeLeft / 40f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 scale = Projectile.Size / texture.Size();
            Color color = Color.Lerp(Color.Turquoise, Color.White, Projectile.scale) * Projectile.scale;

            Main.spriteBatch.Draw(texture, drawPosition, null, color, 0f, texture.Size() * 0.5f, scale * Pow(Projectile.scale, 1.5f), 0, 0f);
            for (int i = 0; i < 2; i++)
            {
                float rotation = Lerp(-PiOver4, PiOver4, i);
                Main.spriteBatch.Draw(texture, drawPosition, null, color, rotation, texture.Size() * 0.5f, scale * new Vector2(0.1f, 1f) * 1.45f, 0, 0f);
            }
            Main.spriteBatch.ResetBlendState();

            return false;
        }
    }
}
