using System.Collections.Generic;
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.Projectiles.Melee.PunctusRock;

namespace InfernumMode.Content.Projectiles.Melee
{
    public class PunctusProjectile : ModProjectile, IPixelPrimitiveDrawer
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
        public const int PullbackLength = 30;

        public const int FadeOutLength = 10;

        public const int TintLength = 10;

        public const int MinRocksForHoming = 3;

        public const int MaxCirclingRocks = 6;
        #endregion

        #region AI
        public override string Texture => "InfernumMode/Content/Items/Weapons/Melee/Punctus";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 90;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 240;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = -1;

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
                Projectile.timeLeft = 240;
            else if (CurrentMode is UseMode.NormalThrow && ShouldHome)
            {
                // Add to the custom old positions list, removing the last one if too many are present.
                if (OldPositions.Count >= ProjectileID.Sets.TrailCacheLength[Type])
                    OldPositions.RemoveAt(OldPositions.Count - 1);

                OldPositions.Insert(0, Projectile.Center);
            }

            Timer++;
        }

        public void DoBehavior_Aim()
        {
            // Aim the spear at the mouse.
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
                // Also, don't play it if neither mouse button is being held.
                //if (Timer == PullbackLength && (Owner.channel || Owner.Calamity().mouseRight))
                //    SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.1f }, Owner.Center);
            }

            // Calculate rotation, and set the player's arm to look as if its holding the spear.
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
            Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
            float frontArmRotation = Projectile.rotation - PiOver4 - PullbackCompletion * Owner.direction * 0.74f;
            if (Owner.direction == 1)
                frontArmRotation += Pi;

            // Stick to the player.
            Projectile.Center = Owner.Center + (frontArmRotation + PiOver2).ToRotationVector2() * Projectile.scale * 16f + Projectile.velocity * Projectile.scale * 40f;
            Owner.heldProj = Projectile.whoAmI;
            Owner.SetDummyItemTime(2);

            // Perform directioning.
            Projectile.spriteDirection = Owner.direction;
            if (Owner.direction == -1)
                Projectile.rotation += PiOver2;

            // Pause the global rock circling timer if this is a rock throw, as the rocks should not be moving.
            // This is important to prevent any new rocks currently spawning from moving a bit before they should be,
            // messing up the positions.
            if (CurrentMode is UseMode.RockThrow)
                Owner.GetModPlayer<PunctusPlayer>().PauseTimer = true;

            // Determine whether the spear should create more rocks on hit.
            int activeRocks = PunctusRock.ActiveRocks(Owner);
            if (CurrentMode is UseMode.RockThrow && activeRocks >= MaxCirclingRocks)
                ShouldCreateMoreRocks = true;

            // Update the player's arm directions to make it look as though they're holding the spear.
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);

            // Fire the spear if ready.
            if (CurrentMode is UseMode.NormalThrow ? !Owner.channel : !Owner.Calamity().mouseRight)
            {
                if (PullbackCompletion == 1f)
                {
                    // Ensure the timer resumes once the spear is fired.
                    Owner.GetModPlayer<PunctusPlayer>().PauseTimer = false;

                    // Restore the players arm.
                    Owner.SetCompositeArmFront(false, Player.CompositeArmStretchAmount.Full, 0f);

                    SoundEngine.PlaySound(InfernumSoundRegistry.PunctusThrowSound with { Volume = 0.5f, Pitch = 0.2f }, Owner.Center);
                    SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 1f, Pitch = -0.2f }, Owner.Center);

                    // Determine whether the spear should home. 3 rocks must be circling, and it must be a normal throw.
                    int circlingRocks = PassiveRocks(Owner);
                    if (circlingRocks >= MinRocksForHoming && CurrentMode is UseMode.NormalThrow)
                        ShouldHome = true;

                    // Swap the current state to firing, set the velocity, reset the timer and sync.
                    CurrentState = UseState.Firing;
                    Projectile.velocity *= Owner.ActiveItem().shootSpeed;
                    Timer = 0f;
                    Projectile.netUpdate = true;
                }
            }
        }

        public void DoBehavior_Fire()
        {
            // Ensure these remain correct.
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
            Projectile.spriteDirection = 1;

            // Check whether the timer can be reset, to handle it overflowing (VERY unlikely to happen ever regardless but yeah).
            if (Timer == 3f)
                Owner.GetModPlayer<PunctusPlayer>().CheckToResetTimer = true;

            Lighting.AddLight(Projectile.Center, WayfinderSymbol.Colors[1].ToVector3());

            // Release anime-like streak particle effects at the side of the spear to indicate motion.
            if (Timer % 3 == 2 || Main.rand.NextBool(3))
            {
                Vector2 energySpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(10f, 10f) + Projectile.velocity * 2f;
                Vector2 energyVelocity = -Projectile.velocity.SafeNormalize(Vector2.UnitX * Projectile.direction) * Main.rand.NextFloat(4f, 6.75f);
                Particle energyLeak = new SquishyLightParticle(energySpawnPosition, energyVelocity, Main.rand.NextFloat(0.35f, 0.5f), Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], Main.rand.NextFloat()), 30, 0.5f, 4.5f, 3f);
                GeneralParticleHandler.SpawnParticle(energyLeak);
            }

            // Only home if it should.
            if (!ShouldHome)
                return;

            // Locate the closest target, prioritising bosses, and lightly home towards them.
            NPC target = CalamityUtils.ClosestNPCAt(Projectile.Center, 250f, true, true);

            if (target == null)
                return;

            float newSpeed = Clamp(Projectile.velocity.Length() * 1.032f, 6f, 42f);

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * newSpeed, 0.24f).RotateTowards(Projectile.AngleTo(target.Center), 0.1f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * newSpeed;
        }

        // Fade out once it's hit something.
        public void DoBehavior_Hit()
        {
            Projectile.Opacity = Utils.GetLerpValue(FadeOutLength, 0f, Timer, true);

            if (Projectile.Opacity == 0f)
                Projectile.Kill();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0;

            // Custom collision code to check along the spear in a line.
            float distance = Projectile.Size.Length() * Projectile.scale;
            Vector2 start = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * distance * 0.5f;
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * distance * 0.5f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 15f * Projectile.scale, ref collisionPoint);
        }

        // Only hit stuff if it hasn't already lost a pierce from hitting.
        public override bool? CanHitNPC(NPC target) => Projectile.penetrate > 1 ? null : false;

        // Only hit stuff if it hasn't already lost a pierce from hitting.
        public override bool CanHitPlayer(Player target) => Projectile.penetrate > 1;

        // Only hit stuff if it hasn't already lost a pierce from hitting.
        public override bool CanHitPvp(Player target) => Projectile.penetrate > 1;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Swap the state to hit.
            Projectile.netUpdate = true;
            Timer = 0;
            CurrentState = UseState.Hit;

            // Get the tip of the spear.
            float spearLength = Projectile.Size.Length();
            Vector2 spearTip = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * spearLength * 0.5f;

            // Spawn the rock(s). These handle their own rock particle visuals.
            if (Main.myPlayer == Owner.whoAmI)
            {
                int numberOfExistingRocks = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (!Main.projectile[i].active || Main.projectile[i].owner != Owner.whoAmI || Main.projectile[i].type != ModContent.ProjectileType<PunctusRock>() || Main.projectile[i].ModProjectile is not PunctusRock rock)
                        continue;

                    if (rock.CurrentState is State.Circling or State.Aiming)
                    {
                        if (rock.HasDoneInitialGlow)
                        {
                            rock.Timer = rock.CurrentState is State.Circling ? 300 : CircleLength - 300f;
                            rock.Projectile.timeLeft = rock.CurrentState is State.Circling ? CircleLength : 300;
                        }
                        numberOfExistingRocks++;
                    }
                }

                bool anyRocksCreated = false;
                // If there isnt already 6 rocks, and is a left click throw, spawn a rock and update the amount of existing ones.
                if (numberOfExistingRocks < MaxCirclingRocks && CurrentMode is not UseMode.RockThrow)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), spearTip, Projectile.velocity, ModContent.ProjectileType<PunctusRock>(), Projectile.damage, 3f, Owner.whoAmI, Tau * numberOfExistingRocks / MaxCirclingRocks);
                    numberOfExistingRocks++;
                    anyRocksCreated = true;
                }

                // If this is a 6 rock throw, spawn 3 more rocks if able to, regardless of click mode.
                if (ShouldCreateMoreRocks)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        // Ensure more than 6 rocks aren't created.
                        if (numberOfExistingRocks == MaxCirclingRocks)
                            break;

                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), spearTip, Projectile.velocity, ModContent.ProjectileType<PunctusRock>(), Projectile.damage, 3f, Owner.whoAmI, Tau * numberOfExistingRocks / MaxCirclingRocks);
                        numberOfExistingRocks++;
                        anyRocksCreated = true;
                    }
                }

                if (!anyRocksCreated)
                {
                    // Crumble away into rocks on death.
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * Main.rand.NextFloat(1f, 8f);
                        Particle rock = new ProfanedRockParticle(spearTip, velocity,
                            Color.White, Main.rand.NextFloat(0.65f, 0.95f), 60, Main.rand.NextFloat(0f, 0.2f), false);
                        GeneralParticleHandler.SpawnParticle(rock);
                    }
                }
            }

            // Hit sounds. Not that bad but could do with custom sounds. Look into?
            SoundEngine.PlaySound(SoundID.DD2_LightningBugZap, spearTip);
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.95f, Volume = 0.9f }, spearTip);

            // Make the spear abruptly freeze in place with some screenshake to imply a heavy impact.
            Projectile.velocity = Vector2.Zero;
            Owner.Infernum_Camera().CurrentScreenShakePower = ShouldCreateMoreRocks ? 4f : 2f;

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
            }

            // Pretty bloom thing.
            var bloom = new StrongBloom(spearTip, Vector2.Zero, Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], Main.rand.NextFloat(0.3f, 0.7f)), 1f * scaleModifier, 40);
            GeneralParticleHandler.SpawnParticle(bloom);

            // Visual sparks as an indication of the high energy impact. Considered using the electric ones but they looked kinda bad.
            for (int i = 0; i < 8; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 5f);
                Color sparkColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], Main.rand.NextFloat(0.4f, 1f));
                GeneralParticleHandler.SpawnParticle(new SparkParticle(spearTip, sparkVelocity, false, 40, 2f * scaleModifier, sparkColor));
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
            // Only draw the trail if flying, and homing.
            if (CurrentMode is not UseMode.NormalThrow || CurrentState < UseState.Firing || !ShouldHome)
                return;
            
            AfterimageDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(InfernumTextureRegistry.StreakMagma);

            AfterimageDrawer.DrawPixelated(OldPositions, -Main.screenPosition - Vector2.One.RotatedBy(Projectile.rotation - PiOver2) * 10f, 30);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D spear = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // Account for the player moving up slopes if being held.
            if (CurrentState is UseState.Aiming)
                drawPosition.Y += Owner.gfxOffY;

            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Draw some backglow if the next shot can be homing, or will create extra rocks.
            int rockTotal = PassiveRocks(Owner);
            float glowDistance = 2f;
            if ((CurrentMode is UseMode.NormalThrow && rockTotal >= MinRocksForHoming && CurrentState is not UseState.Aiming) || (CurrentMode is UseMode.RockThrow && rockTotal == MaxCirclingRocks))
                glowDistance = 4f;

            float backglowAmount = 12f;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / backglowAmount).ToRotationVector2() * glowDistance * Utilities.Saturate(Timer / 10f);
                Color backglowColor = WayfinderSymbol.Colors[1];
                backglowColor.A = 0;
                Main.spriteBatch.Draw(spear, drawPosition + backglowOffset, null, backglowColor * Projectile.Opacity, Projectile.rotation, spear.Size() * 0.5f, Projectile.scale, direction, 0);
            }

            // Apply a tint to the spear to indicate it can be fired, along with the sound played.
            bool useTint = CurrentState is UseState.Aiming && PullbackCompletion >= 1f && Timer < PullbackLength + TintLength * 2f;
            if (useTint)
            {
                Main.spriteBatch.EnterShaderRegion();

                float shiftAmount = Utils.GetLerpValue(PullbackLength, PullbackLength + TintLength * 0.75f, Timer, true) * Utils.GetLerpValue(PullbackLength + TintLength * 2f, PullbackLength + TintLength * 1.25f, Timer, true);
                InfernumEffectsRegistry.BasicTintShader.UseSaturation(Lerp(0f, 0.55f, CalamityUtils.CircInEasing(shiftAmount, 1)));
                InfernumEffectsRegistry.BasicTintShader.UseOpacity(1f);
                InfernumEffectsRegistry.BasicTintShader.UseColor(WayfinderSymbol.Colors[0]);
                InfernumEffectsRegistry.BasicTintShader.Apply();
            }

            // Draw the spear.
            Main.spriteBatch.Draw(spear, drawPosition, null, Projectile.GetAlpha(lightColor) * Projectile.Opacity, Projectile.rotation, spear.Size() * 0.5f, Projectile.scale, direction, 0);

            // Restore the spritebatch if needed.
            if (useTint)
                Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        #endregion
    }
}
