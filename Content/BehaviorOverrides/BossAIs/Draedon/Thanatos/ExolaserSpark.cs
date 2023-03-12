using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class ExolaserSpark : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exolaser Spark");
            Main.projFrames[Projectile.type] = 8;

        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.scale = 1.2f;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 200;
            Projectile.Opacity = 0f;
            Projectile.hide = true;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.MaxUpdates);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.MaxUpdates = reader.ReadInt32();

        public override void AI()
        {
            if (Projectile.timeLeft < 30)
            {
                Projectile.Opacity = Projectile.timeLeft / 30f;
                Projectile.damage = 0;
                return;
            }

            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.velocity.Length() < 5f)
                Projectile.velocity *= 1.0225f;

            // Frames
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 8)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
                Projectile.frameCounter = 0;
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 32) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle sourceRectangle = texture.Frame(1, Main.projFrames[Projectile.type], frameY: Projectile.frame);

            Vector2 origin = sourceRectangle.Size() * 0.5f;
            Color frontAfterimageColor = Projectile.GetAlpha(lightColor) * 0.45f;
            frontAfterimageColor.A = 120;
            for (int i = 0; i < 7; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 7f + Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * Projectile.scale * 4f;
                Vector2 afterimageDrawPosition = Projectile.Center + drawOffset - Main.screenPosition;
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, sourceRectangle, frontAfterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 drawOffset = -Projectile.velocity.SafeNormalize(Vector2.Zero) * i * Projectile.scale * 4f;
                Vector2 afterimageDrawPosition = Projectile.Center + drawOffset - Main.screenPosition;
                Color backAfterimageColor = Projectile.GetAlpha(lightColor) * ((12f - i) / 12f);
                backAfterimageColor.A = 0;
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, sourceRectangle, backAfterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }
    }
}
