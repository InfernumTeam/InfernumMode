using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class ElectricPulse : BaseMassiveExplosionProjectile
    {
        public override int Lifetime => 75;

        public override bool UsesScreenshake => false;

        public override Color GetCurrentExplosionColor(float pulseCompletionRatio) => Color.Lerp(Color.Cyan * 1.2f, Color.DeepSkyBlue, Clamp(pulseCompletionRatio * 1.2f, 0f, 1f));

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Electric Pulse");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            if (UsesScreenshake)
            {
                float screenShakePower = GetScreenshakePower(Projectile.timeLeft / (float)Lifetime) * Utils.GetLerpValue(1300f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
                if (Main.LocalPlayer.Calamity().GeneralScreenShakePower < screenShakePower)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = screenShakePower;
            }

            // Expand outward.
            CurrentRadius = Lerp(CurrentRadius, MaxRadius, 0.25f);
            Projectile.scale = Lerp(1.2f, 5f, Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true));

            // Adjust the hitbox.
            Projectile.ExpandHitboxBy((int)(CurrentRadius * Projectile.scale * 0.54f), (int)(CurrentRadius * Projectile.scale * 0.54f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

            float pulseCompletionRatio = Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true);
            Vector2 scale = new(1.5f, 1f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Projectile.Size * scale * 0.5f;
            Rectangle drawArea = new(0, 0, Projectile.width, Projectile.height);
            Color fadeoutColor = new(new Vector4(Fadeout(pulseCompletionRatio)) * Projectile.Opacity);
            DrawData drawData = new(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value, drawPosition, drawArea, fadeoutColor, Projectile.rotation, Projectile.Size, scale, SpriteEffects.None, 0);

            GameShaders.Misc["ForceField"].UseColor(GetCurrentExplosionColor(pulseCompletionRatio));
            GameShaders.Misc["ForceField"].Apply(drawData);
            drawData.Draw(Main.spriteBatch);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.85f;
    }
}
