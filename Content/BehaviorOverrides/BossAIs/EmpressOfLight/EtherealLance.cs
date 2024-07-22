using System.IO;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EtherealLance : ModProjectile, IScreenCullDrawer
    {
        public float FlySpeedFactor
        {
            get;
            set;
        } = 1f;

        public bool PlaySoundOnFiring
        {
            get;
            set;
        }

        public float SoundPitch
        {
            get;
            set;
        }

        public Color MyColor
        {
            get
            {
                Color color = Main.hslToRgb(Projectile.ai[1] % 1f, 1f, 0.5f) * Projectile.Opacity * 1.3f;
                if (EmpressOfLightBehaviorOverride.ShouldBeEnraged)
                    color = EmpressOfLightBehaviorOverride.GetDaytimeColor(Projectile.ai[1] % 1f) * Projectile.Opacity;

                color.A /= 8;
                return color;
            }
        }

        public ref float Time => ref Projectile.localAI[0];

        public static int FireDelay => 68;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.FireWhipProj}";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ethereal Lance");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 120;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = Projectile.MaxUpdates * 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(PlaySoundOnFiring);
            writer.Write(Time);
            writer.Write(FlySpeedFactor);
            writer.Write(Projectile.MaxUpdates);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            PlaySoundOnFiring = reader.ReadBoolean();
            Time = reader.ReadSingle();
            FlySpeedFactor = reader.ReadSingle();
            Projectile.MaxUpdates = reader.ReadInt32();
            Projectile.localAI[1] = reader.ReadSingle();
        }

        public override void AI()
        {
            if (Time >= FireDelay)
            {
                Projectile.velocity = Projectile.ai[0].ToRotationVector2() * FlySpeedFactor * 40f;
                if (Main.rand.NextBool(3))
                {
                    Dust rainbowMagic = Dust.NewDustPerfect(Projectile.Center, 267);
                    rainbowMagic.fadeIn = 1f;
                    rainbowMagic.noGravity = true;
                    rainbowMagic.alpha = 100;
                    rainbowMagic.color = Color.Lerp(MyColor, Color.White, Main.rand.NextFloat() * 0.4f);
                    rainbowMagic.noLight = true;
                    rainbowMagic.scale *= 1.5f;
                }

                if (PlaySoundOnFiring && Time == FireDelay)
                    SoundEngine.PlaySound(SoundID.Item163 with { Pitch = SoundPitch }, Main.LocalPlayer.Center);
            }
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 14, 0, 255);
            Projectile.rotation = Projectile.ai[0];
            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage() => Time >= FireDelay ? null : false;

        public void CullDraw(SpriteBatch spriteBatch)
        {
            Texture2D telegraphTex = InfernumTextureRegistry.Line.Value;
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;

            int telegraphSize = 3400;
            if (Projectile.localAI[1] > 0f)
                telegraphSize = (int)Projectile.localAI[1];

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 telegraphOrigin = telegraphTex.Size() * new Vector2(0f, 0.5f);
            Vector2 outerTelegraphScale = new(telegraphSize / (float)telegraphTex.Width, 6f);
            Vector2 innerTelegraphScale = new(telegraphSize / (float)telegraphTex.Width, 2f);

            Color lanceColor = MyColor;
            Color telegraphColor = MyColor;
            lanceColor.A = 0;
            telegraphColor.A /= 2;

            Color fadedLanceColor = lanceColor * Utils.GetLerpValue(FireDelay, FireDelay - 5f, Time, true) * Projectile.Opacity;
            Color outerLanceColor = Color.White * Utils.GetLerpValue(0f, 20f, Time, true);
            outerLanceColor.A /= 2;

            spriteBatch.Draw(telegraphTex, drawPos, null, fadedLanceColor * 0.65f, Projectile.rotation, telegraphOrigin, innerTelegraphScale, 0, 0f);
            spriteBatch.Draw(telegraphTex, drawPos, null, fadedLanceColor * 0.24f, Projectile.rotation, telegraphOrigin, outerTelegraphScale, 0, 0f);

            Vector2 origin = tex.Size() / 2f;
            float scale = Lerp(0.7f, 1f, Utils.GetLerpValue(FireDelay - 5f, FireDelay, Time, true));
            float telegraphInterpolant = Utils.GetLerpValue(10f, FireDelay, Time, false) * Projectile.Opacity;
            if (telegraphInterpolant > 0f)
            {
                for (float i = 1f; i > 0f; i -= 1f / 16f)
                {
                    Vector2 lineOffset = Projectile.rotation.ToRotationVector2() * Utils.GetLerpValue(0f, 1f, Projectile.velocity.Length(), true) * i * -120f;
                    spriteBatch.Draw(tex, drawPos + lineOffset, null, lanceColor * telegraphInterpolant * (1f - i), Projectile.rotation, origin, scale, 0, 0f);
                    spriteBatch.Draw(tex, drawPos + lineOffset, null, new Color(255, 255, 255, 0) * telegraphInterpolant * (1f - i) * 0.15f, Projectile.rotation, origin, scale * 0.85f, 0, 0f);
                }
                for (float i = 0f; i < 1f; i += 0.25f)
                {
                    Vector2 drawOffset = (TwoPi * i + Projectile.rotation).ToRotationVector2() * scale * 2f;
                    spriteBatch.Draw(tex, drawPos + drawOffset, null, telegraphColor * telegraphInterpolant, Projectile.rotation, origin, scale, 0, 0f);
                }
                spriteBatch.Draw(tex, drawPos, null, telegraphColor * telegraphInterpolant, Projectile.rotation, origin, scale * 1.1f, 0, 0f);
            }
            spriteBatch.Draw(tex, drawPos, null, outerLanceColor, Projectile.rotation, origin, scale, 0, 0f);
        }
    }
}
