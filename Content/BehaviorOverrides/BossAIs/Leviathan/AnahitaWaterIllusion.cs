using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Leviathan
{
    public class AnahitaWaterIllusion : ModProjectile
    {
        public static NPC Anahita => Main.npc[CalamityGlobalNPC.siren];

        public ref float Time => ref Projectile.ai[0];

        public ref float OffsetAngle => ref Projectile.ai[1];

        public const int Lifetime = 270;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Anahita Illusion");
            Main.projFrames[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 86;
            Projectile.height = 190;
            Projectile.timeLeft = Lifetime;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Die if Anahita is not around.
            if (CalamityGlobalNPC.siren == -1 || LeviathanComboAttackManager.FightState == LeviAnahitaFightState.LeviathanAlone)
            {
                Projectile.Kill();
                return;
            }

            OffsetAngle += ToRadians(0.84f);
            Projectile.Center = Anahita.Center + OffsetAngle.ToRotationVector2() * 125f;
            int frameHeight = Anahita.frame.Height;
            if (frameHeight == 0)
                frameHeight = 190;
            Projectile.frame = Anahita.frame.Y / frameHeight;
            Projectile.spriteDirection = Anahita.spriteDirection;
            Time++;
            Projectile.Opacity = Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true) * Anahita.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 baseDrawPosition = Projectile.Center - Main.screenPosition;
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color baseDrawColor = Projectile.GetAlpha(lightColor) * 0.6f;

            // Create back afterimages.
            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = -Vector2.UnitY.RotatedBy(TwoPi * i / 4f) * 4f;
                Color drawColor = Projectile.GetAlpha(Main.hslToRgb((Projectile.identity * 0.2875f + i / 4f) % 1f, 1f, 0.75f));
                drawColor.A = (byte)(Projectile.Opacity * 64f);
                drawColor *= 0.6f;
                Main.spriteBatch.Draw(texture, baseDrawPosition + drawOffset, frame, drawColor, Projectile.rotation, origin, Projectile.scale, direction, 0f);
            }

            Main.spriteBatch.Draw(texture, baseDrawPosition, frame, baseDrawColor, Projectile.rotation, origin, Projectile.scale, direction, 0f);
            return false;
        }
    }
}
