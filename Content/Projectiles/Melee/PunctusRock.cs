using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians;
using InfernumMode.Content.Items.Weapons.Melee;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Melee
{
    public class PunctusRock : ModProjectile
    {
        #region Classes
        // These only function with these rocks, hence being private.
        private class ProfanusRockParticle : Particle
        {
            public Vector2 StartingPosition;

            public Color OriginalColor;

            public float RotationSpeed;

            public float Opacity;

            public static int BaseDriftTime => 25;

            public static int StopAndGlowLength = 30;

            public override int FrameVariants => 6;

            public override string Texture => "InfernumMode/Common/Graphics/Particles/ProfanedRockParticle";

            /// <summary>
            /// Only designed for use by profanus rocks.
            /// </summary>
            /// <param name="position"></param>
            /// <param name="velocity"></param>
            /// <param name="color"></param>
            /// <param name="scale"></param>
            /// <param name="lifeTime"></param>
            /// <param name="rotationSpeed"></param>
            /// <param name="gravity"></param>
            /// <param name="fadeIn"></param>
            public ProfanusRockParticle(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime)
            {
                Position = position;
                Velocity = velocity;
                Color = OriginalColor = color;
                Scale = scale;
                Lifetime = lifeTime;
                RotationSpeed = Main.rand.NextFloat(-0.02f, 0.02f);
                Rotation = Main.rand.NextFloat(TwoPi);
                Opacity = 1f;
                Variant = Main.rand.Next(6);
            }

            // For some fucking reason, manually updating the timer in the loop this is called in causes an index error.
            public override void Update() => Time++;
        }
        #endregion

        #region Enumerations
        public enum State
        {
            Circling,
            Aiming,
            Firing
        }
        #endregion

        #region Fields/Properties
        public string CurrentVariant = ProfanedRock.Textures[0];

        private List<ProfanusRockParticle> FormingRocks;

        public Vector2[] RockOffsets;

        public int IndexWeAre;

        public Vector2 AimingOffsetPosition;

        public bool RockIsBuffed;

        public bool HasDoneInitialGlow;

        public ref float RotationOffset => ref Projectile.ai[0];

        public ref float Timer => ref Projectile.ai[1];

        public State CurrentState
        {
            get => (State)Projectile.ai[2];
            set => Projectile.ai[2] = (int)value;
        }

        public Player Owner => Main.player[Projectile.owner];
        #endregion

        #region Constants
        public const int CircleLength = 720;

        public const int CrumbleWarningLength = 180;

        public const int CanBeFiredLength = 180;

        public const int MaxCreationParticles = 15;

        public const int CreationParticleLifetime = 90;

        public const int GlowStartTime = CreationParticleLifetime - 20;

        public const int GlowEndTime = CreationParticleLifetime + 20;

        public const float MinDamageMultiplier = 0.5f;

        public const float MaxDamageMultiplier = 0.8f;

        public const float MinShootSpeed = 30f;

        public const float MaxShootSpeed = 50f;

        public const float BuffedShootSpeed = 40f;

        public const float BuffedDamageMultiplier = 0.8f;
        #endregion

        #region AI
        public override string Texture => "InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/Rocks/" + CurrentVariant;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
        }

        public override void SetDefaults()
        {
            // These get changed later, but are this by default.
            Projectile.width = 42;
            Projectile.height = 36;

            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.Opacity = 0;
            Projectile.timeLeft = CircleLength;

            FormingRocks = new();
        }

        public override void AI()
        {
            // Die if the owner is dead.
            if (Owner.dead || !Owner.active)
            {
                Projectile.Kill();
                return;
            }

            // Initialization.
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;

                // The center is the spear tip for this first frame, so it can be used to spawn the particles from the spear visually.
                // As these are purely visual and dont interact with anything, they can be desynced between clients just fine.
                // The rock being ready to fire is done based on time, so no mechanical desync will occure.
                int particleAmount = Main.rand.Next(MaxCreationParticles - 5, MaxCreationParticles);
                for (int i = 0; i < particleAmount; i++)
                {
                    Vector2 position = Projectile.Center;
                    // The initial velocity is that of the spear, which allows the rocks to look like they're moving in the same direction.
                    Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * Main.rand.NextFloat(1f, 8f);
                    var rock = new ProfanusRockParticle(position, velocity, Color.White, Main.rand.NextFloat(0.8f, 1f), CreationParticleLifetime);
                    FormingRocks.Add(rock);
                }

                // These are also purely visual, and do not need to be synced.
                RockOffsets = new Vector2[MaxCreationParticles];
                for (int i = 0; i < MaxCreationParticles; i++)
                    RockOffsets[i] = Main.rand.NextVector2Circular(10f, 10f);

                // After being read, clear the spear velocity. It is no longer needed, and the rock should not move yet.
                Projectile.velocity = Vector2.Zero;

                // Only the owner should choose this. Pick the visual variant to be.
                if (Main.myPlayer == Owner.whoAmI)
                {
                    int varient = Main.rand.Next(ProfanedRock.Textures.Length);
                    switch (varient)
                    {
                        case 0:
                            CurrentVariant = ProfanedRock.Textures[varient];
                            break;
                        case 1:
                            CurrentVariant = ProfanedRock.Textures[varient];
                            Projectile.width = 34;
                            Projectile.height = 38;
                            break;
                        case 2:
                            CurrentVariant = ProfanedRock.Textures[varient];
                            Projectile.width = 36;
                            Projectile.height = 46;
                            break;
                        case 3:
                            CurrentVariant = ProfanedRock.Textures[varient];
                            Projectile.width = 28;
                            Projectile.height = 36;
                            break;
                    }

                    // Ensure it is synced.
                    Projectile.netUpdate = true;
                }

                // Get the first available rock index and set us to it.
                var rockPlayer = Owner.GetModPlayer<PunctusPlayer>();

                for (int i = 0; i < rockPlayer.RockSlots.Length; i++)
                {
                    if (rockPlayer.RockSlots[i] == -1)
                    {
                        rockPlayer.RockSlots[i] = Projectile.whoAmI;
                        IndexWeAre = i;
                        break;
                    }
                }
            }

            // Do behavior.
            switch (CurrentState)
            {
                case State.Circling:
                    DoBehavior_Circling();
                    break;

                case State.Aiming:
                    DoBehavior_Aiming();
                    break;

                case State.Firing:
                    DoBehavior_Firing();
                    break;
            }

            // Update any particles.
            if (FormingRocks.Any())
                UpdateFormingRocks();

            if (Timer == GlowEndTime + 1f)
                HasDoneInitialGlow = true;

            // Shudder around if close to running out of time, and it has not been fired.
            // TODO: Add sfx for this.
            if (Main.myPlayer == Owner.whoAmI && CurrentState is not State.Firing && Timer >= CircleLength - CrumbleWarningLength)
            {
                float shudderStrength = Utils.GetLerpValue(CircleLength - CrumbleWarningLength, CircleLength, Timer, true);
                Projectile.Center += Main.rand.NextVector2Circular(10f, 10f) * shudderStrength;

                // Dunno if either are needed but I'd rather attempt to make it work in mp.
                Projectile.netSpam = 0;
                Projectile.netUpdate = true;
            }

            Timer++;
        }

        public void UpdateFormingRocks()
        {
            // Update each of the rock particles.
            for (int i = 0; i < FormingRocks.Count; i++)
            {
                var rock = FormingRocks[i];

                // Fade out at the very end.
                rock.Opacity = Utils.GetLerpValue(rock.Lifetime, rock.Lifetime - 10f, rock.Time, true);

                // Rotate a bit.
                rock.Rotation += rock.RotationSpeed * (rock.Velocity.X > 0 ? 1f : -1f);

                int[] driftDelays = new int[6]
                {
                    1,
                    3,
                    6,
                    2,
                    0,
                    4
                };

                // Drift outwards from the impact for a specified amount of time, adding an offset to add visual variation.
                int driftTime = ProfanusRockParticle.BaseDriftTime + driftDelays[i % (driftDelays.Length - 1)] * 3;
                if (rock.Time <= driftTime)
                {
                    rock.Position += rock.Velocity;
                    rock.Velocity *= 0.99f;
                }
                else
                {
                    // Rapidly fly towards the chosen point, as if they're forming the rocks. For balancing reasons this happens very fast, else the rocks take too long to be created
                    // and the weapon feels a bit clunky.
                    Vector2 targetPosition = Projectile.Center + RockOffsets[i % (RockOffsets.Length - 1)];
                    float interpolant = CalamityUtils.SineInEasing(Utils.GetLerpValue(driftTime, rock.Lifetime - ProfanusRockParticle.StopAndGlowLength, rock.Time, true), 1);
                    rock.Position = Vector2.Lerp(rock.StartingPosition, targetPosition, interpolant);
                }

                // When The rock has finished drifting, mark its current location as the starting position for the forming movement.
                if (rock.Time == driftTime)
                    rock.StartingPosition = rock.Position;

                // Idk man.
                rock.Update();
            }

            // Remove every rock that should die.
            FormingRocks.RemoveAll(rocks => rocks.Time >= rocks.Lifetime);
        }

        public void DoBehavior_Circling()
        {
            // Circle around the owner. This uses a global timer, which if paused, will prevent the rock from moving.
            Projectile.Center = Owner.Center - (Owner.GetModPlayer<PunctusPlayer>().RockTimer / 55f + (Tau * IndexWeAre / PunctusProjectile.MaxCirclingRocks)).ToRotationVector2() * 100f;

            // Start off invisible, and fade in glowing when the rocks are moving inwards.
            if (Timer == CreationParticleLifetime - 10)
                Projectile.Opacity = 1f;

            // Leave if not opaque enough.
            if (Projectile.Opacity < 0.9f)
                return;

            // Attempt to get the currently held spear.
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || Main.projectile[i].type != ModContent.ProjectileType<PunctusProjectile>() || Main.projectile[i].owner != Owner.whoAmI)
                    continue;

                // Get the spear, and if it is performing a rock throw and aiming, begin aiming ourselves.
                PunctusProjectile profanus = Main.projectile[i].ModProjectile as PunctusProjectile;
                if (profanus.CurrentMode is PunctusProjectile.UseMode.RockThrow && profanus.CurrentState is PunctusProjectile.UseState.Aiming)
                {

                    SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound with { Pitch = 0.9f, Volume = 0.9f }, Projectile.Center);

                    // Mark our current position.
                    AimingOffsetPosition = Projectile.Center - Owner.Center;
                    CurrentState = State.Aiming;
                    Timer = CircleLength - 300f;
                    Projectile.timeLeft = 300;
                    Projectile.netUpdate = true;
                    break;
                }
            }
        }

        public void DoBehavior_Aiming()
        {
            // Aim backwards as if preparing to launch forwards.
            float aimBackStrength = Utilities.Saturate((Timer - (CircleLength - 300f)) / 20f);
            Vector2 aimBackOffset = Projectile.SafeDirectionTo(Owner.Calamity().mouseWorld) * -30f * aimBackStrength;
            Projectile.Center = Owner.Center + AimingOffsetPosition + aimBackOffset;

            // Begin rotating, getting faster over a short period of time to indiciate winding up.
            Projectile.rotation += 0.2f * Utils.GetLerpValue(0f, 40f, Timer, true);

            // Rotate the rock slightly to show wind up energy.
            Projectile.Center += Main.rand.NextVector2Circular(1f, 1f) * aimBackStrength;

            // Dunno if either are needed but I'd rather attempt to make it work in mp.
            Projectile.netSpam = 0;
            Projectile.netUpdate = true;

            // If the owner is holding punctus, and is no longer holding right click, and the rocks are fully recoiled, launch them.
            if (Owner.ActiveItem() == null || Owner.ActiveItem().type != ModContent.ItemType<Punctus>() || !Owner.Calamity().mouseRight && aimBackStrength >= 1f)
            {
                // Get the amount of active rocks.
                int rockAmount = ActiveRocks(Owner);
                float interlopant = Utilities.Saturate((float)rockAmount / PunctusProjectile.MaxCirclingRocks);

                // Get the base stats. These get stronger the more rocks are present.
                float shootSpeed = Lerp(MinShootSpeed, MaxShootSpeed, interlopant);
                float damageMultiplier = Lerp(MinDamageMultiplier, MaxDamageMultiplier, interlopant);

                // If all 6 are active, edit the stats again and mark the rock as buffed.
                if (interlopant >= 1f)
                {
                    shootSpeed = BuffedShootSpeed;
                    RockIsBuffed = true;
                }

                // Modify the rock's damage according to the above.
                Projectile.damage = (int)(Projectile.damage * damageMultiplier);

                // Launch at the owner's mouse.
                if (Main.myPlayer == Owner.whoAmI)
                    Projectile.velocity = Projectile.SafeDirectionTo(Owner.Calamity().mouseWorld) * shootSpeed;

                // Give the rock 300 frames of lifetime, to allow it to actually hit something if it was close to breaking.
                Projectile.timeLeft = 300;

                // Play a firing sound. This is the same one used by the defender guardian.
                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.95f, Volume = 0.9f }, Projectile.Center);

                // Create an explosion of dust and fire.
                for (int i = 0; i < 20; i++)
                {
                    Vector2 velocity = -Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(Main.rand.NextFloat(-0.15f, 0.15f)) * Main.rand.NextFloat(4f, 6f);
                    Particle rock = new SandyDustParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f), velocity, Color.SandyBrown,
                        Main.rand.NextFloat(1.25f, 1.55f), 90);
                    GeneralParticleHandler.SpawnParticle(rock);

                    Particle fire = new HeavySmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f), Vector2.Zero,
                        Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : WayfinderSymbol.Colors[2], 30, Main.rand.NextFloat(0.2f, 0.4f), 1f, glowing: true,
                        rotationSpeed: Main.rand.NextFromList(-1, 1) * 0.01f);
                    GeneralParticleHandler.SpawnParticle(fire);
                }
                
                // And ofc some lovely screenshake.
                if (CalamityConfig.Instance.Screenshake)
                    Owner.Infernum_Camera().CurrentScreenShakePower = 2f;

                // Update the current state, timer, and sync.
                CurrentState = State.Firing;
                Timer = 0f;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_Firing()
        {
            // Leave a trail of rocky dust behind the rock as it travels.
            Particle rockParticle = new SandyDustParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 3f, Projectile.height / 3f), Vector2.Zero, Color.SandyBrown,
                Main.rand.NextFloat(0.45f, 0.75f), 30);

            GeneralParticleHandler.SpawnParticle(rockParticle);

            // Rotate.
            Projectile.rotation += 0.3f;

            // Only home if buffed.
            if (!RockIsBuffed)
                return;

            // Locate the closest target, prioritising bosses, and lightly home towards them.
            NPC target = CalamityUtils.ClosestNPCAt(Projectile.Center, 1200f, true, true);

            if (target == null)
                return;

            Vector2 directionToTarget = Projectile.SafeDirectionTo(target.Center);

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, directionToTarget * 40f, 0.055f);
        }

        /// <summary>
        /// Calcuates the current number of rocks that are aiming, or have just been fired.
        /// </summary>
        /// <param name="owner">The player who's rock count you are trying to find.</param>
        /// <returns>The amount of rocks.</returns>
        public static int ActiveRocks(Player owner)
        {
            int total = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || Main.projectile[i].owner != owner.whoAmI || Main.projectile[i].type != ModContent.ProjectileType<PunctusRock>() || Main.projectile[i].ModProjectile is not PunctusRock rock)
                    continue;

                // Theres a bit of leeway with rocks that have just fired, as they can fire slightly ahead of the spear.
                if (rock.CurrentState is State.Aiming || (rock.CurrentState is State.Firing && rock.Timer < 30f))
                    total++;
            }
            return total;
        }

        /// <summary>
        /// Calculates the current number of rocks that are circling, or aiming.
        /// </summary>
        /// <param name="owner">The player who's rock count you are trying to find.</param>
        /// <returns>The amount of rocks.</returns>
        public static int PassiveRocks(Player owner)
        {
            int total = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || Main.projectile[i].owner != owner.whoAmI || Main.projectile[i].type != ModContent.ProjectileType<PunctusRock>() || Main.projectile[i].ModProjectile is not PunctusRock rock)
                    continue;

                if (rock.CurrentState is State.Circling or State.Aiming)
                    total++;
            }
            return total;
        }

        // Only deal damage if firing. Letting them body stuff would just be annoying.
        public override bool? CanDamage() => CurrentState is State.Firing;

        public override void OnKill(int timeLeft)
        {
            // Crumble away into rocks on death.
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity;
                if (Projectile.velocity == Vector2.Zero)
                    velocity = -Vector2.UnitY.RotatedByRandom(Tau) * Main.rand.NextFloat(0.5f, 2f);
                else
                    velocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * Main.rand.NextFloat(1f, 8f);
                Particle rock = new ProfanedRockParticle(Projectile.Center, velocity,
                    Color.White, Main.rand.NextFloat(0.65f, 0.95f), 60, Main.rand.NextFloat(0f, 0.2f), false);
                GeneralParticleHandler.SpawnParticle(rock);
            }
        }
        #endregion

        #region Drawing
        public void DrawRocks()
        {
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Common/Graphics/Particles/ProfanedRockParticle").Value;
            for (int i = 0; i < FormingRocks.Count; i++)
            {
                var rock = FormingRocks[i];

                // Get the interpolants for the rock overlay and bloom.
                float interpolant = Utils.GetLerpValue(rock.Lifetime - ProfanusRockParticle.StopAndGlowLength, rock.Lifetime - ProfanusRockParticle.StopAndGlowLength * 0.5f, rock.Time, true);
                float interpolantGlow = Utils.GetLerpValue(rock.Lifetime - ProfanusRockParticle.StopAndGlowLength, rock.Lifetime, rock.Time, true);

                Rectangle frame = texture.Frame(1, rock.FrameVariants, 0, rock.Variant);

                // Draw some backglow.
                int backglowAmount = 12;
                for (int j = 0; j < 12; j++)
                {
                    Vector2 backglowOffset = (TwoPi * j / backglowAmount).ToRotationVector2() * 2f;
                    Color backglowColor = WayfinderSymbol.Colors[1] * rock.Opacity;
                    Main.EntitySpriteDraw(texture, rock.Position - Main.screenPosition + backglowOffset, frame, backglowColor with { A = 0 } * rock.Opacity, rock.Rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
                }

                // Draw the rock.
                Main.spriteBatch.Draw(texture, rock.Position - Main.screenPosition, frame, rock.Color * rock.Opacity * (1- interpolant), rock.Rotation, frame.Size() * 0.5f, rock.Scale, SpriteEffects.None, 0f);

                // Glow red hot when dying, to form the actual rock.
                if (rock.Time >= rock.Lifetime - ProfanusRockParticle.StopAndGlowLength)
                {
                    // Draw the sprite overitself additively a few times.
                    Color glowColor = WayfinderSymbol.Colors[1];
                    for (int j = 0; j < 3; j++)
                        Main.EntitySpriteDraw(texture, rock.Position - Main.screenPosition, frame, glowColor with { A = 0 } * 20f * interpolant, rock.Rotation, frame.Size() * 0.5f, rock.Scale, SpriteEffects.None, 0);

                    // Draw a bloom flare, to cover up the main rock forming underneath it.
                    Texture2D bloom = InfernumTextureRegistry.BloomFlare.Value;
                    // Rapidly grow then shrink.
                    float scale = CalamityUtils.Convert01To010(interpolantGlow);
                    Main.spriteBatch.Draw(bloom, rock.Position - Main.screenPosition, null, glowColor with { A = 0 } * interpolantGlow * rock.Opacity * 0.5f, (Main.GlobalTimeWrappedHourly + i * 10f) * (i % 2 == 0 ? -3 : 3f), bloom.Size() * 0.5f, rock.Scale * 0.15f * scale, SpriteEffects.None, 0f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            // Draw a telegraph line aiming at the mouse. This is also a reference to the guardian defender.
            if (CurrentState is State.Aiming)
            {
                Texture2D invis = InfernumTextureRegistry.Invisible.Value;
                float opacity = Utils.GetLerpValue(0, 20, Timer, true);
                float scale = 400f * Lerp(0f, 3f, Utils.GetLerpValue(0f, 1250f, Projectile.Distance(Owner.Calamity().mouseWorld) * 2f, true));
                Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;
                laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
                laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);
                laserScopeEffect.Parameters["mainOpacity"].SetValue(Pow(opacity, 0.5f));
                laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(340f));
                laserScopeEffect.Parameters["laserAngle"].SetValue((Owner.Calamity().mouseWorld - Projectile.Center).ToRotation() * -1f);
                laserScopeEffect.Parameters["laserWidth"].SetValue((0.0015f + Pow(opacity, 5f) * (Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.002f + 0.002f)));
                laserScopeEffect.Parameters["laserLightStrenght"].SetValue(3f);
                laserScopeEffect.Parameters["color"].SetValue(Color.Lerp(WayfinderSymbol.Colors[1], Color.OrangeRed, 0.2f).ToVector3());
                laserScopeEffect.Parameters["darkerColor"].SetValue(WayfinderSymbol.Colors[2].ToVector3());
                laserScopeEffect.Parameters["bloomSize"].SetValue(0.12f + (1f - opacity) * 0.1f);
                laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.4f);
                laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(3f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, laserScopeEffect, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(invis, drawPosition, null, Color.White, 0f, invis.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

            // Draw a trail if buffed. This is the same as the ones used by the healer guardians projectiles, and is another reference.
            if (RockIsBuffed)
            {
                Texture2D streakTexture = InfernumTextureRegistry.Gleam.Value;
                for (int i = 1; i < Projectile.oldPos.Length; i++)
                {
                    if (Projectile.oldPos[i - 1] == Vector2.Zero || Projectile.oldPos[i] == Vector2.Zero)
                        continue;

                    float completionRatio = i / (float)Projectile.oldPos.Length;
                    float fade = Pow(completionRatio, 2f);
                    float scale = Projectile.scale * Lerp(1.3f, 0.9f, Utils.GetLerpValue(0f, 0.24f, completionRatio, true)) *
                        Lerp(0.9f, 0.56f, Utils.GetLerpValue(0.5f, 0.78f, completionRatio, true));
                    if (i == 1)
                        scale *= 0.5f;
                    Color drawColor = Color.Lerp(MagicSpiralCrystalShot.ColorSet[0], new Color(229, 255, 255), fade) * (1f - fade) * Projectile.Opacity;
                    drawColor.A = 0;

                    float rotation = Projectile.velocity.ToRotation() + PiOver2;
                    // Due to the speed of the rocks, 4 are needed to be drawn to ensure this actually looks somewhat good.
                    // TODO: Maybe change this and the healer projectile trails to be less, shit?
                    Vector2 trailDrawPosition = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f - Main.screenPosition;
                    Vector2 trailDrawPosition2 = Vector2.Lerp(trailDrawPosition, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, 0.25f);
                    Vector2 trailDrawPosition3 = Vector2.Lerp(trailDrawPosition, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, 0.56f);
                    Vector2 trailDrawPosition4 = Vector2.Lerp(trailDrawPosition, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, 0.75f);

                    Main.spriteBatch.Draw(streakTexture, trailDrawPosition, null, drawColor, rotation, streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(streakTexture, trailDrawPosition2, null, drawColor, rotation, streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(streakTexture, trailDrawPosition3, null, drawColor, rotation, streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(streakTexture, trailDrawPosition4, null, drawColor, rotation, streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }
            }

            // Draw backglow.
            int backglowAmount = 12;
            for (int i = 0; i < 12; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / backglowAmount).ToRotationVector2() * 5f;
                Color backglowColor = WayfinderSymbol.Colors[1] * lightColor.ToGreyscale();
                Main.EntitySpriteDraw(texture, drawPosition + backglowOffset, null, backglowColor with { A = 0 } * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            // Draw the main rock.
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor) * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            // Draw additional stuff if either the rock is buffed, or has just been created.
            bool drawInitialGlowyShit = (CurrentState is State.Circling && Timer >= GlowStartTime && Timer <= GlowEndTime && !HasDoneInitialGlow) || RockIsBuffed;
            if (drawInitialGlowyShit)
            {
                float opacity = RockIsBuffed ? Utils.GetLerpValue(0f, 10f, Timer, true) : Utils.GetLerpValue(CreationParticleLifetime + 20, CreationParticleLifetime, Timer, true);
                
                // Draw the rock over itself 2 times.
                Color backglowColor = WayfinderSymbol.Colors[1];
                for (int i = 0; i < 2; i++)
                    Main.EntitySpriteDraw(texture, drawPosition , null, backglowColor with { A = 0 } * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            // Draw any of the forming rock particles, over the top of this for the bloom effect.
            if (FormingRocks.Any())
                DrawRocks();
            return false;
        }
        #endregion
    }
}
