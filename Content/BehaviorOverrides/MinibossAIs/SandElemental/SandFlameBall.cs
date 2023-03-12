using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.SandElemental
{
    public class SandFlameBall : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sand Ball");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 100;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Play a wind sound.
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.DD2_BookStaffCast, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }

            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.075f, 0f, 1f);

            // Determine frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Utilities.CreateGenericDustExplosion(Projectile.Center, 32, 15, 8f, 1.2f);
            Utilities.CreateGenericDustExplosion(Projectile.Center, 65, 8, 9f, 1.35f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into desert flames.
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            for (int i = 0; i < 3; i++)
            {
                Vector2 shootVelocity = Projectile.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.65f, 0.65f, i / 2f)) * 8f;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, shootVelocity, ProjectileID.DesertDjinnCurse, Projectile.damage, 0f, Main.myPlayer, target.whoAmI);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 3f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, frame, Projectile.GetAlpha(new Color(0.84f, 0.19f, 0.87f, 0f)) * 0.65f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity > 0.9f;
    }
}
