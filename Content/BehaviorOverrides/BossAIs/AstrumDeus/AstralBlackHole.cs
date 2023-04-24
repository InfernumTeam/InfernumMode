using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class AstralBlackHole : ModProjectile
    {
        public const int LaserCount = 6;

        public ref float Timer => ref Projectile.ai[0];

        public ref float Owner => ref Projectile.ai[1];

        public Player Target => Main.player[Projectile.owner];

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/WhiteHole";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Astral Black Hole");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 160;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900000;
            Projectile.scale = 1f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Die if Deus is not present.
            if (!NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()))
            {
                Projectile.Kill();
                return;
            }

            Projectile.scale = Utilities.UltrasmoothStep(Timer / 60f) * 2.5f + Utilities.UltrasmoothStep(Timer / 32f) * 0.34f;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, 0f, Utils.GetLerpValue(30f, 0f, Projectile.timeLeft, true));
            Projectile.Opacity = MathHelper.Clamp(Projectile.scale * 0.87f, 0f, 1f);
            Timer++;

            // Prepare for death if the lasers are gone.
            if (Timer > 90f && !Utilities.AnyProjectiles(ModContent.ProjectileType<DarkGodLaser>()) && Projectile.timeLeft > 30)
            {
                Projectile.damage = 0;
                Projectile.timeLeft = 30;
            }

            // Create the lasers.
            if (Timer == 90f)
            {
                SoundEngine.PlaySound(TeslaCannon.FireSound, Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < LaserCount; i++)
                    {
                        Vector2 laserDirection = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / LaserCount);
                        Utilities.NewProjectileBetter(Projectile.Center, laserDirection, ModContent.ProjectileType<DarkGodLaser>(), AstrumDeusHeadBehaviorOverride.BlackHoleLaserDamage, 0f);
                    }
                }
            }

            // Idly release sparks.
            if (Timer >= 90f && Timer % 10f == 9f)
            {
                SoundEngine.PlaySound(SoundID.Item28, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                    Vector2 flyVelocity = Projectile.SafeDirectionTo(target.Center) * (BossRushEvent.BossRushActive ? 28f : 19.5f);
                    Utilities.NewProjectileBetter(Projectile.Center + flyVelocity * 10f, flyVelocity, ModContent.ProjectileType<DarkBoltLarge>(), AstrumDeusHeadBehaviorOverride.DarkBoltDamage, 0f);
                }
            }
        }

        public override bool? CanDamage() => Timer >= 96f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.scale * 80f, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D blackHoleTexture = TextureAssets.Projectile[Projectile.type].Value;
            Texture2D noiseTexture = InfernumTextureRegistry.VoronoiShapes.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = noiseTexture.Size() * 0.5f;

            if (Timer < 90f)
            {
                float width = Utils.GetLerpValue(0f, 8f, Timer, true) * Utils.GetLerpValue(45f, 38f, Timer, true) * 3f;
                for (int i = 0; i < LaserCount; i++)
                {
                    Vector2 lineDirection = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / LaserCount);
                    Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + lineDirection * 4500f, Color.Violet, width);
                }
            }

            // Draw a vortex blackglow effect if the reduced graphics config is not enabled.
            if (!InfernumConfig.Instance.ReducedGraphicsConfig)
            {
                Main.spriteBatch.EnterShaderRegion();

                Vector2 diskScale = Projectile.scale * new Vector2(0.925f, 0.85f);
                GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(Projectile.Opacity);
                GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Turquoise);
                GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.Turquoise);
                GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

                for (int i = 0; i < 2; i++)
                    Main.spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, 0f, origin, diskScale, SpriteEffects.None, 0f);
                Main.spriteBatch.ExitShaderRegion();
            }

            Vector2 blackHoleScale = Projectile.Size / blackHoleTexture.Size() * Projectile.scale;
            for (int i = 0; i < 2; i++)
                Main.spriteBatch.Draw(blackHoleTexture, drawPosition, null, Color.White, 0f, blackHoleTexture.Size() * 0.5f, blackHoleScale * 1.0024f, SpriteEffects.None, 0f);
            for (int i = 0; i < 3; i++)
                Main.spriteBatch.Draw(blackHoleTexture, drawPosition, null, Color.Black, 0f, blackHoleTexture.Size() * 0.5f, blackHoleScale, SpriteEffects.None, 0f);

            return false;
        }

        public override void Kill(int timeLeft)
        {
            Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<DarkStar>());

            SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound, Projectile.Center);
            Color[] explosionColors = new Color[]
            {
                new(250, 90, 74, 127),
                new(76, 255, 194, 127)
            };
            GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(Projectile.Center, Vector2.Zero, explosionColors, 3f, 180, 1.4f));
        }
    }
}
