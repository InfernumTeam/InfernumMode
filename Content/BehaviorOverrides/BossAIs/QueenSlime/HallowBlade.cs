using System.IO;
using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class HallowBlade : ModProjectile, IScreenCullDrawer
    {
        public float TelegraphLength
        {
            get;
            set;
        }

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

        public float SoundPitchOffset
        {
            get;
            set;
        }

        public Color MyColor
        {
            get
            {
                Color color = Color.Lerp(Color.HotPink, Color.Cyan, Projectile.ai[1] % 1f * 0.7f);
                color.A /= 5;
                return color;
            }
        }

        public ref float Time => ref Projectile.localAI[0];

        public static int FireDelay => 24;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hallow Blade");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 3000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 30;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.MaxUpdates = 4;
            Projectile.timeLeft = Projectile.MaxUpdates * 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Time);
            writer.Write(PlaySoundOnFiring);
            writer.Write(FlySpeedFactor);
            writer.Write(Projectile.MaxUpdates);
            writer.Write(Projectile.timeLeft);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Time = reader.ReadSingle();
            PlaySoundOnFiring = reader.ReadBoolean();
            FlySpeedFactor = reader.ReadSingle();
            Projectile.MaxUpdates = reader.ReadInt32();
            Projectile.timeLeft = reader.ReadInt32();
            Projectile.localAI[1] = reader.ReadSingle();
        }

        public override void AI()
        {
            bool fadeOut = Projectile.timeLeft <= 25;
            if (Time >= FireDelay)
            {
                if (fadeOut)
                    Projectile.velocity *= 0.9f;
                else
                {
                    Projectile.velocity = Projectile.ai[0].ToRotationVector2() * FlySpeedFactor * 16f;
                    if (Main.rand.NextBool(3))
                    {
                        Dust magic = Dust.NewDustPerfect(Projectile.Center, 267);
                        magic.fadeIn = 1f;
                        magic.noGravity = true;
                        magic.alpha = 100;
                        magic.color = Color.Lerp(MyColor, Color.White, Main.rand.NextFloat() * 0.4f);
                        magic.noLight = true;
                        magic.scale *= 1.5f;
                    }
                }

                if (PlaySoundOnFiring && Time == FireDelay && Projectile.FinalExtraUpdate())
                {
                    SoundEngine.PlaySound(SoundID.Item163 with { Pitch = SoundPitchOffset + 0.3f }, Projectile.Center);
                    PlaySoundOnFiring = false;
                }
            }

            // Immediately fade away if intersecting the big laser.
            if (!fadeOut && Projectile.localAI[1] == 0f)
            {
                foreach (Projectile laser in Utilities.AllProjectilesByID(ModContent.ProjectileType<HallowBladeLaserbeam>()))
                {
                    if (laser.Colliding(laser.Hitbox, Projectile.Hitbox) && Time >= FireDelay)
                    {
                        Projectile.velocity *= 0.3f;
                        Projectile.timeLeft = 25;
                        Projectile.netUpdate = true;
                        fadeOut = true;
                    }

                    TelegraphLength = Distance(laser.Center.X, Projectile.Center.X);
                    if (TelegraphLength >= 4000f)
                        TelegraphLength = 0f;
                }
            }

            Projectile.alpha = Utils.Clamp(Projectile.alpha + fadeOut.ToDirectionInt() * 14, 0, 255);
            Projectile.rotation = Projectile.ai[0];

            if (Projectile.FinalExtraUpdate())
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

            // Why is this named Smolstar in the source code?
            Texture2D fullBladeTex = TextureAssets.Projectile[ProjectileID.Smolstar].Value;
            Rectangle fullBladeFrame = fullBladeTex.Frame(1, 2);

            int telegraphSize = (int)TelegraphLength;
            if (Projectile.localAI[1] > 0f)
                telegraphSize = (int)Projectile.localAI[1];

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 telegraphOrigin = telegraphTex.Size() * new Vector2(0f, 0.5f);
            Vector2 outerTelegraphScale = new(telegraphSize / (float)telegraphTex.Width, 9f);
            Vector2 innerTelegraphScale = new(telegraphSize / (float)telegraphTex.Width, 3f);

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
                for (float i = 1f; i > 0f; i -= 1f / 12f)
                {
                    Vector2 lineOffset = Projectile.rotation.ToRotationVector2() * Utils.GetLerpValue(0f, 1f, Projectile.velocity.Length(), true) * i * -110f;
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
            spriteBatch.Draw(fullBladeTex, drawPos, fullBladeFrame, Projectile.GetAlpha(Color.White), Projectile.rotation + PiOver2, fullBladeFrame.Size() * 0.5f, scale, 0, 0f);
        }
    }
}
