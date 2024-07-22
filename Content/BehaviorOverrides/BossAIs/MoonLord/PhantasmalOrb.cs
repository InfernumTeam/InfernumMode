using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord
{
    public class PhantasmalOrb : ModProjectile
    {
        public bool CanSplit
        {
            get => Projectile.ai[1] == 0f;
            set => Projectile.ai[1] = 1 - value.ToInt();
        }

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Phantasmal Orb");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 70;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;
            
        }

        public override void AI()
        {
            Projectile.velocity *= 0.965f;
            Projectile.Opacity = Lerp(Projectile.Opacity, 1f, 0.56f);

            if (Projectile.timeLeft != 2)
                return;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float shootSpeed = 2.75f;
                Vector2 orthogonalVelocity = Projectile.velocity.RotatedBy(PiOver2).SafeNormalize(Vector2.UnitY) * shootSpeed;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, orthogonalVelocity, ProjectileID.PhantasmalBolt, Projectile.damage, 0f);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, -orthogonalVelocity, ProjectileID.PhantasmalBolt, Projectile.damage, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float lerpMult = (1f + 0.22f * Cos(Main.GlobalTimeWrappedHourly % 30f * TwoPi * 3f + Projectile.identity % 10f)) * 0.8f;

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Color baseColor = new(39, 255, 151, 192);
            baseColor *= Projectile.Opacity * 0.6f;
            baseColor.A = 0;
            Color colorA = baseColor;
            Color colorB = baseColor * 0.5f;
            colorA *= lerpMult;
            colorB *= lerpMult;
            Vector2 origin = texture.Size() / 2f;
            Vector2 scale = new(Projectile.scale * Projectile.Opacity * lerpMult);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Main.spriteBatch.Draw(texture, drawPos, null, colorA, PiOver2, origin, scale, spriteEffects, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, colorA, 0f, origin, scale, spriteEffects, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, colorB, PiOver2, origin, scale * 0.8f, spriteEffects, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, colorB, 0f, origin, scale * 0.8f, spriteEffects, 0);

            Main.spriteBatch.Draw(texture, drawPos, null, colorA, PiOver2 + Main.GlobalTimeWrappedHourly * 0.35f, origin, scale, spriteEffects, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, colorA, Main.GlobalTimeWrappedHourly * 0.35f, origin, scale, spriteEffects, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, colorB, PiOver2 + Main.GlobalTimeWrappedHourly * 0.625f, origin, scale * 0.8f, spriteEffects, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, colorB, Main.GlobalTimeWrappedHourly * 0.625f, origin, scale * 0.8f, spriteEffects, 0);

            Main.spriteBatch.Draw(texture, drawPos, null, colorA, PiOver4, origin, scale * 0.6f, spriteEffects, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, colorA, PiOver4 * 3f, origin, scale * 0.6f, spriteEffects, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, colorB, PiOver4, origin, scale * 0.4f, spriteEffects, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, colorB, PiOver4 * 3f, origin, scale * 0.4f, spriteEffects, 0);

            Main.spriteBatch.Draw(texture, drawPos, null, colorA, PiOver4 + Main.GlobalTimeWrappedHourly * 1.1f * 0.75f, origin, scale * 0.6f, spriteEffects, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, colorA, PiOver4 * 3f + Main.GlobalTimeWrappedHourly * 1.1f * 0.75f, origin, scale * 0.6f, spriteEffects, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, colorB, PiOver4 + Main.GlobalTimeWrappedHourly * 1.1f, origin, scale * 0.4f, spriteEffects, 0);
            Main.spriteBatch.Draw(texture, drawPos, null, colorB, PiOver4 * 3f + Main.GlobalTimeWrappedHourly * 1.1f, origin, scale * 0.4f, spriteEffects, 0);

            return false;
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {

        }
    }
}
