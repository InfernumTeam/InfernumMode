using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class HolyMagicLaserbeam : BaseLaserbeamProjectile, IPixelPrimitiveDrawer, ISpecializedDrawRegion
    {
        public PrimitiveTrailCopy LaserDrawer
        {
            get;
            set;
        }

        public int LaserTelegraphTime
        {
            get;
            set;
        } = 45;

        public int LaserShootTime
        {
            get;
            set;
        } = 35;

        public bool FromGuardians
        {
            get;
            set;
        }

        public float SetAngleToMoveTo
        {
            get;
            set;
        }

        public Vector2 InitialVelocity
        {
            get;
            set;
        } = Vector2.Zero;

        public override float Lifetime => LaserTelegraphTime + LaserShootTime;

        public override Color LaserOverlayColor => Color.White;

        public override Color LightCastColor => Color.Wheat;

        public override Texture2D LaserBeginTexture => TextureAssets.Projectile[Projectile.type].Value;

        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Lasers/OrangeLaserbeamMid", AssetRequestMode.ImmediateLoad).Value;

        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Lasers/OrangeLaserbeamEnd", AssetRequestMode.ImmediateLoad).Value;

        public override float MaxLaserLength => 5000f;

        public override float MaxScale => 1f;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Holy Disintegration Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            if (ProvidenceBehaviorOverride.IsEnraged)
                Projectile.Size *= 1.5f;

            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
            writer.Write(LaserTelegraphTime);
            writer.Write(LaserShootTime);
            writer.Write(FromGuardians);
            writer.Write(SetAngleToMoveTo);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
            LaserTelegraphTime = reader.ReadInt32();
            LaserShootTime = reader.ReadInt32();
            FromGuardians = reader.ReadBoolean();
            SetAngleToMoveTo = reader.ReadSingle();
        }

        public override void AttachToSomething()
        {
            // Disappear if providence and the commander is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.holyBoss) && !Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss))
            {
                Projectile.Kill();
                return;
            }

            Vector2 center;
            if (FromGuardians)
            {
                Projectile fireball = null;

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].type == ModContent.ProjectileType<HolyDogmaFireball>())
                    {
                        fireball = Main.projectile[i];
                        break;
                    }
                }
                if (fireball is null)
                {
                    Projectile.Kill();
                    return;
                }
                else
                    center = fireball.Center;
            }
            else
                center = Main.npc[CalamityGlobalNPC.holyBoss].Center;

            Projectile.Center = center + Vector2.UnitY * 24f;
            Projectile.Opacity = 1f;

            if (Time == 0)
                InitialVelocity = Projectile.velocity;

            // Rotate during the telegraph.
            float telegraphCompletion = Utils.GetLerpValue(0f, LaserTelegraphTime, Time, true);
            if (telegraphCompletion < 1f)
            {
                if (SetAngleToMoveTo == 0)
                    Projectile.velocity = Projectile.velocity.RotatedBy(RotationalSpeed * Pow(CalamityUtils.Convert01To010(telegraphCompletion), 15f));
                else
                    Projectile.velocity = InitialVelocity.ToRotation().AngleLerp(SetAngleToMoveTo, telegraphCompletion).ToRotationVector2();
            }
            Projectile.velocity = Projectile.velocity.RotatedBy(-RotationalSpeed);

            // Play a sound if the telegraph is over.
            if (Time == LaserTelegraphTime)
                SoundEngine.PlaySound(new SoundStyle("InfernumMode/Assets/Sounds/Custom/Providence/DogmaLasersFire") with { PitchVariance = 0.5f }, Projectile.Center);
        }

        public override void DetermineScale()
        {
            float lifetimeCompletion = Utils.GetLerpValue(LaserTelegraphTime, Lifetime, Time, true);
            Projectile.scale = CalamityUtils.Convert01To010(lifetimeCompletion) * MaxScale * 3f;
            if (Projectile.scale > MaxScale)
                Projectile.scale = MaxScale;
        }

        public override float DetermineLaserLength()
        {
            // Make the laser over time fire outward instead of instantly being full length, for the sake of impact.
            return Utils.Remap(Time, LaserTelegraphTime, LaserTelegraphTime + 10f, 20f, MaxLaserLength);
        }

        public override bool? CanDamage() => Time >= LaserTelegraphTime ? null : false;

        public float LaserWidthFunction(float _) => Projectile.scale * Projectile.width * 2;

        public Color LaserColorFunction(float completionRatio)
        {
            float colorInterpolant = Sin(Main.GlobalTimeWrappedHourly * -3.2f + completionRatio * 23f) * 0.5f + 0.5f;
            Color c = Color.Lerp(Color.Orange, Color.Pink, colorInterpolant * 0.67f);
            if (ProvidenceBehaviorOverride.IsEnraged && !FromGuardians)
                c = Color.Lerp(c, Color.SkyBlue, 0.55f);

            return c;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            // Don't draw until the telegraphs are done being drawn.
            if (Time < LaserTelegraphTime)
                return;

            // This should never happen, but just in case.
            if (Projectile.velocity == Vector2.Zero)
                return;

            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader);
            Vector2 laserEnd = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;
            Vector2[] baseDrawPoints = new Vector2[20];
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(Projectile.Center, laserEnd, i / (float)(baseDrawPoints.Length - 1f));

            // Select textures to pass to the shader, along with the electricity color.
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.Wheat);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakMagma);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");

            LaserDrawer.DrawPixelated(baseDrawPoints, -Main.screenPosition, 54);
        }

        // Draw the telegraphs before the lasers go outward.
        // This technically also draws when the lasers are shot, since the bloom looks super cool.
        public void SpecialDraw(SpriteBatch spriteBatch)
        {
            float opacity = Pow(Time / LaserTelegraphTime, 0.4f);
            Texture2D invisible = InfernumTextureRegistry.Invisible.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;
            float width = (0.002f + Pow(opacity, 4f) * (Sin(Main.GlobalTimeWrappedHourly * 3.5f) * 0.001f + 0.001f)) * Projectile.width / 36f;
            if (width >= 0.0072f)
                width = 0.0072f;

            laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
            laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);
            laserScopeEffect.Parameters["mainOpacity"].SetValue(Sqrt(opacity));
            laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(1500f));
            laserScopeEffect.Parameters["laserAngle"].SetValue(-Projectile.velocity.ToRotation());
            laserScopeEffect.Parameters["laserWidth"].SetValue(width);
            laserScopeEffect.Parameters["laserLightStrenght"].SetValue(5f);
            laserScopeEffect.Parameters["color"].SetValue(Color.Lerp(Color.Pink, ProvidenceBehaviorOverride.IsEnraged && !FromGuardians ? Color.Cyan : Color.Yellow, Projectile.identity / 7f % 1f * 0.84f).ToVector3());
            laserScopeEffect.Parameters["darkerColor"].SetValue(Color.Lerp(Color.Orange, Color.Red, 0.24f).ToVector3());
            laserScopeEffect.Parameters["bloomSize"].SetValue(0.28f + (1f - opacity) * 0.18f);
            laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.4f);
            laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(3f);
            laserScopeEffect.CurrentTechnique.Passes[0].Apply();
            spriteBatch.Draw(invisible, drawPosition, null, Color.White, 0f, invisible.Size() * 0.5f, opacity * MaxLaserLength * 1.3f, SpriteEffects.None, 0f);
        }

        public void PrepareSpriteBatch(SpriteBatch spriteBatch)
        {
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
        }
    }
}
