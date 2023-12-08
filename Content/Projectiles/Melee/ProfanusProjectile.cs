using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Melee
{
    public class ProfanusProjectile : ModProjectile, IPixelPrimitiveDrawer
    {
        #region Enumerations
        public enum UseMode
        {
            NormalThrow,
            RockThrow
        }

        public enum UseState
        {
            Aiming,
            Firing,
            Hit
        }
        #endregion

        #region Properties
        public UseMode CurrentMode
        {
            get => (UseMode)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public UseState CurrentState
        {
            get => (UseState)Projectile.ai[1];
            set => Projectile.ai[1] = (int)value;
        }

        public ref float Timer => ref Projectile.ai[2];

        public Player Owner => Main.player[Projectile.owner];

        public PrimitiveTrailCopy AfterimageDrawer
        {
            get;
            private set;
        }

        public float PullbackCompletion => Utilities.Saturate(Timer / PullbackLength);

        public List<Vector2> OldPositions;

        public bool ShouldCreateMoreRocks;

        public bool ShouldHome;

        public Vector2 OffsetToTargetCenter;
        #endregion

        #region Constants
        public static int PullbackLength => 30;

        public static int FadeOutLength => 10;

        public static int TintLength => 10;

        public const int MaxRocks = 6;
        #endregion

        #region AI
        public override string Texture => "InfernumMode/Content/Items/Weapons/Melee/Profanus";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 90;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 240;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = -1;
            Projectile.Opacity = 1f;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;

            OldPositions = new();
        }

        public override void AI()
        {
            switch (CurrentState)
            {
                case UseState.Aiming:
                    DoBehavior_Aim();
                    break;

                case UseState.Firing:
                    DoBehavior_Fire();
                    break;

                case UseState.Hit:
                    DoBehavior_Hit();
                    break;
            }

            // Ensure the projectile doesn't die before being fired.
            if (CurrentState is UseState.Aiming)
            {
                //Projectile.Opacity = Utilities.Saturate(Projectile.Opacity += 0.05f);
                Projectile.timeLeft = 240;
            }
            else
            {
                if (OldPositions.Count >= ProjectileID.Sets.TrailCacheLength[Type])
                    OldPositions.RemoveAt(OldPositions.Count - 1);

                OldPositions.Insert(0, Projectile.Center);
            }

            Timer++;
        }

        public void DoBehavior_Aim()
        {
            // Aim the spear.
            if (Main.myPlayer == Projectile.owner)
            {
                float aimInterpolant = Utils.GetLerpValue(5f, 25f, Owner.Distance(Main.MouseWorld), true);
                Vector2 oldVelocity = Projectile.velocity;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Owner.SafeDirectionTo(Main.MouseWorld), aimInterpolant);
                if (Projectile.velocity != oldVelocity)
                {
                    Projectile.netSpam = 0;
                    Projectile.netUpdate = true;
                }

                // Don't bother playing this for other players, it's only useful for the owner.
                if (Timer == PullbackLength && (Owner.channel || Owner.Calamity().mouseRight))
                    SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.1f }, Owner.Center);
            }

            // Stick to the player.
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
            Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
            float frontArmRotation = Projectile.rotation - PiOver4 - PullbackCompletion * Owner.direction * 0.74f;
            if (Owner.direction == 1)
                frontArmRotation += Pi;

            Projectile.Center = Owner.Center + (frontArmRotation + PiOver2).ToRotationVector2() * Projectile.scale * 16f + Projectile.velocity * Projectile.scale * 40f;
            Owner.heldProj = Projectile.whoAmI;
            Owner.SetDummyItemTime(2);

            // Perform directioning.
            Projectile.spriteDirection = Owner.direction;
            if (Owner.direction == -1)
                Projectile.rotation += PiOver2;

            if (CurrentMode is UseMode.RockThrow)
                Owner.GetModPlayer<ProfanusPlayer>().PauseTimer = true;

            int activeRocks = ProfanusRocks.ActiveRocks(Owner);
            if (CurrentMode is UseMode.RockThrow && activeRocks >= 6)
                ShouldCreateMoreRocks = true;

            // Update the player's arm directions to make it look as though they're holding the spear.
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);

            if (CurrentMode is UseMode.NormalThrow ? !Owner.channel : !Owner.Calamity().mouseRight)
            {
                if (PullbackCompletion == 1f)
                {
                    Owner.GetModPlayer<ProfanusPlayer>().PauseTimer = false;

                    Owner.SetCompositeArmFront(false, Player.CompositeArmStretchAmount.Full, 0f);
                    
                    int circlingRocks = ProfanusRocks.PassiveRocks(Owner);
                    if (circlingRocks >= 3)
                        ShouldHome = true;

                    CurrentState = UseState.Firing;
                    Projectile.velocity *= Owner.ActiveItem().shootSpeed;
                    Timer = 0f;
                    Projectile.netUpdate = true;
                }
            }
        }

        public void DoBehavior_Fire()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
            Projectile.spriteDirection = 1;

            if (Timer == 3f)
                Owner.GetModPlayer<ProfanusPlayer>().CheckToResetTimer = true;

            if (!ShouldHome)
                return;

            NPC target = CalamityUtils.ClosestNPCAt(Projectile.Center, 250f, true, true);

            if (target == null)
                return;

            Vector2 directionToTarget = Projectile.SafeDirectionTo(target.Center);

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, directionToTarget * 45f, 0.05f);
        }

        public void DoBehavior_Hit() => Projectile.Opacity = Utils.GetLerpValue(FadeOutLength, 0f, Timer, true);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0;

            float distance = Projectile.Size.Length() * Projectile.scale;
            Vector2 start = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * distance * 0.5f;
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * distance * 0.5f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 15f * Projectile.scale, ref collisionPoint);
        }

        public override bool? CanHitNPC(NPC target) => Projectile.penetrate > 1;

        public override bool CanHitPlayer(Player target) => Projectile.penetrate > 1;

        public override bool CanHitPvp(Player target) => Projectile.penetrate > 1;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.netUpdate = true;
            Timer = 0;
            CurrentState = UseState.Hit;

            float spearLength = Projectile.Size.Length();
            Vector2 spearTip = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * spearLength * 0.5f;

            // Spawn the rock.
            int numberOfExistingRocks = ProfanusRocks.PassiveRocks(Owner);

            if (Main.myPlayer == Owner.whoAmI)
            {
                if (numberOfExistingRocks < MaxRocks && CurrentMode is not UseMode.RockThrow)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), spearTip, Projectile.velocity, ModContent.ProjectileType<ProfanusRocks>(), Projectile.damage, 3f, Owner.whoAmI, Tau * numberOfExistingRocks / MaxRocks);
                    numberOfExistingRocks++;
                }

                if (ShouldCreateMoreRocks)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (numberOfExistingRocks == 6)
                            break;

                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), spearTip, Projectile.velocity, ModContent.ProjectileType<ProfanusRocks>(), Projectile.damage, 3f, Owner.whoAmI, Tau * numberOfExistingRocks / MaxRocks);
                        numberOfExistingRocks++;
                    }
                }
            }

            Projectile.velocity = Vector2.Zero;

            Owner.Infernum_Camera().CurrentScreenShakePower = 2f;

            SoundEngine.PlaySound(SoundID.DD2_LightningBugZap, spearTip);
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.95f, Volume = 0.9f }, spearTip);

            float scaleModifier = 0.8f;
            // Spawn a bunch of light particles.
            for (int i = 0; i < 10; i++)
            {
                Vector2 position = spearTip + Main.rand.NextVector2Circular(20f, 20f);
                Particle light = new GlowyLightParticle(position, spearTip.DirectionTo(position) * Main.rand.NextFloat(0.5f, 1f),
                    Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : Color.OrangeRed, 60, Main.rand.NextFloat(0.85f, 1.15f) * scaleModifier, Main.rand.NextFloat(0.95f, 1.05f), false);
                GeneralParticleHandler.SpawnParticle(light);
            }

            // Create a fire explosion.
            for (int i = 0; i < 10; i++)
            {
                MediumMistParticle fireExplosion = new(spearTip + Main.rand.NextVector2Circular(30f, 30f), Vector2.Zero,
                    Main.rand.NextBool() ? WayfinderSymbol.Colors[0] : WayfinderSymbol.Colors[1],
                    Color.Gray, Main.rand.NextFloat(0.85f, 1.15f) * scaleModifier, Main.rand.NextFloat(220f, 250f));
                GeneralParticleHandler.SpawnParticle(fireExplosion);

                //Vector2 velocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.6f, 0.6f)) * Main.rand.NextFloat(2f, 6f);
                //Color color = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], Main.rand.NextFloat(0.4f, 1f));
                //var sparkle = new GenericSparkle(spearTip, velocity, color, Color.White, Main.rand.NextFloat(0.5f, 0.75f), 75, Main.rand.NextFloat(0.05f), 2f);
                //GeneralParticleHandler.SpawnParticle(sparkle);
            }

            var bloom = new StrongBloom(spearTip, Vector2.Zero, Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], Main.rand.NextFloat(0.3f, 0.7f)), 1f * scaleModifier, 40);
            GeneralParticleHandler.SpawnParticle(bloom);

            for (int i = 0; i < 8; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 5f);
                Color sparkColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], Main.rand.NextFloat(0.4f, 1f));
                GeneralParticleHandler.SpawnParticle(new SparkParticle(spearTip, sparkVelocity, false, 60, 2f * scaleModifier, sparkColor));

            //    sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f,3f);
            //    Color arcColor = Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], Main.rand.NextFloat(0.3f, 1f));
            //    GeneralParticleHandler.SpawnParticle(new ElectricArc(spearTip, sparkVelocity, arcColor, 0.84f, 30));
            }
        }

        // Don't do damage while aiming, to prevent "poking" strats where massive damage is acquired from just sitting on top of enemies with the spear.
        public override bool? CanDamage() => CurrentState != UseState.Aiming;
        #endregion

        #region Drawing
        public float WidthFunction(float completionRatio) => SmoothStep(21f, 5f, completionRatio) * Projectile.Opacity;

        public Color ColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.75f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true) * 0.9f;
            Color startingColor = new(255, 191, 73);
            Color middleColor = new(89, 43, 49);
            Color endColor = new(25, 8, 8);
            Color color = CalamityUtils.MulticolorLerp(Utilities.Saturate(completionRatio - 0.1f), startingColor, middleColor, endColor) * trailOpacity;
            color.A = (byte)(trailOpacity * 255);
            return color * Projectile.Opacity;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            if (CurrentMode is not UseMode.NormalThrow || CurrentState < UseState.Firing)
                return;
            
            AfterimageDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(InfernumTextureRegistry.StreakMagma);

            List<Vector2> positions = OldPositions;

            AfterimageDrawer.DrawPixelated(positions, -Main.screenPosition - Vector2.One.RotatedBy(Projectile.rotation - PiOver2) * 10f, 30);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D spear = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            if (CurrentState is UseState.Aiming)
                drawPosition.Y += Owner.gfxOffY;

            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            float backglowAmount = 12f;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / backglowAmount).ToRotationVector2() * 2f;
                Color backglowColor = WayfinderSymbol.Colors[1];
                backglowColor.A = 0;
                Main.spriteBatch.Draw(spear, drawPosition + backglowOffset, null, backglowColor * Projectile.Opacity , Projectile.rotation, spear.Size() * 0.5f, Projectile.scale, direction, 0);
            }

            bool useTint = CurrentState is UseState.Aiming && PullbackCompletion >= 1f;
            if (useTint)
            {
                Main.spriteBatch.EnterShaderRegion();

                float shiftAmount = Utils.GetLerpValue(PullbackLength, PullbackLength + TintLength * 0.75f, Timer, true) * Utils.GetLerpValue(PullbackLength + TintLength * 2f, PullbackLength + TintLength * 1.25f, Timer, true);
                InfernumEffectsRegistry.BasicTintShader.UseSaturation(shiftAmount);
                InfernumEffectsRegistry.BasicTintShader.UseOpacity(lightColor.ToGreyscale());
                // Set the color of the shader.
                InfernumEffectsRegistry.BasicTintShader.UseColor(WayfinderSymbol.Colors[0]);
                // Apply the shader.
                InfernumEffectsRegistry.BasicTintShader.Apply();
            }

            Main.spriteBatch.Draw(spear, drawPosition, null, Projectile.GetAlpha(lightColor) * Projectile.Opacity, Projectile.rotation, spear.Size() * 0.5f, Projectile.scale, direction, 0);

            if (useTint)
                Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        #endregion
    }
}
