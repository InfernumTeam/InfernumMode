using CalamityMod;
using CalamityMod.NPCs;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone.CalamitasCloneBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class DarkMagicFlame : ModProjectile, IPixelPrimitiveDrawer
    {
        public string HexType;

        public string HexType2;

        public PrimitiveTrailCopy TrailDrawer = null;

        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic Flame");
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 9;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.scale = 0.8f;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(HexType);
            writer.Write(HexType2);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            HexType = reader.ReadString();
            HexType2 = reader.ReadString();
        }

        public bool ImbuedWithHex(string hexName)
        {
            return HexType == hexName || HexType2 == hexName;
        }

        public override void AI()
        {
            // Initialize the hex type(s).
            if (string.IsNullOrEmpty(HexType) && Projectile.velocity.Length() < 42f && GetHexNames(out HexType, out HexType2))
                Projectile.netUpdate = true;

            float acceleration = 1f;
            float maxSpeed = 36f;
            if (CalamityGlobalNPC.calamitas != -1 && ImbuedWithHex("Zeal"))
            {
                // Start out slower if acceleration is expected.
                if (Projectile.ai[1] == 0f)
                {
                    Projectile.velocity *= 0.37f;
                    Projectile.ai[1] = 1f;
                    Projectile.netUpdate = true;
                }

                acceleration = 1.037f;
            }

            // Home in weakly if CalClone's target has the appropriate hex.
            if (CalamityGlobalNPC.calamitas != -1 && ImbuedWithHex("Accentuation"))
            {
                float idealDirection = Projectile.AngleTo(Main.player[Main.npc[CalamityGlobalNPC.calamitas].target].Center);
                Projectile.velocity = Projectile.velocity.RotateTowards(idealDirection, 0.012f);
                if (Projectile.velocity.Length() > 18.75f)
                    Projectile.velocity *= 0.98f;
            }

            if (acceleration > 1f && Projectile.velocity.Length() < maxSpeed)
                Projectile.velocity *= acceleration;

            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 8f, Time, true);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            Time++;
        }

        public float FlameTrailWidthFunction(float completionRatio)
        {
            return MathHelper.SmoothStep(24f, 5f, completionRatio) * Projectile.Opacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.75f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true) * 0.9f;
            Color startingColor = Color.Lerp(Color.White, Color.DarkRed, 0.25f);
            Color middleColor = Color.Lerp(Color.Pink, Color.Red, 0.4f);
            Color endColor = Color.Lerp(Color.Orange, Color.Black, 0.35f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
            color.A = (byte)(trailOpacity * 255);
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Color color = Projectile.GetAlpha(Color.Lerp(Color.Violet, new Color(1f, 1f, 1f, 1f), Projectile.identity / 5f * 0.6f));

            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            TrailDrawer ??= new PrimitiveTrailCopy(FlameTrailWidthFunction, FlameTrailColorFunction, null, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(InfernumTextureRegistry.StreakMagma);
            TrailDrawer.DrawPixelated(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 30);
        }
    }
}
