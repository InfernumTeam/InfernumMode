using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.DataStructures;
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
    public class ProfanusRocks : ModProjectile
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

            public override bool SetLifetime => true;

            public override bool UseCustomDraw => true;

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

            public override void Update()
            {
                Time++;
            }

            public override void CustomDraw(SpriteBatch spriteBatch)
            {
                Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
                Rectangle frame = texture.Frame(1, FrameVariants, 0, Variant);
                spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color * Opacity, Rotation, frame.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            }
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
        public static int CircleLength => 720;

        public static int CrumbleWarningLength => 180;

        public static int CanBeFiredLength => 180;

        public const int MaxRocks = 20;

        public static int RocksLifetime => 90;
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
            //Projectile.imm = true;
            //Projectile.localNPCHitCooldown = -1;

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
                int particleAmount = Main.rand.Next(10, 15);
                for (int i = 0; i < particleAmount; i++)
                {
                    Vector2 position = Projectile.Center;
                    // The initial velocity is that of the spear, which allows the rocks to look like they're moving in the same direction.
                    Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * Main.rand.NextFloat(1f, 8f);
                    var rock = new ProfanusRockParticle(position, velocity, Color.White, Main.rand.NextFloat(0.8f, 1f), RocksLifetime);
                    FormingRocks.Add(rock);
                }

                // These are also purely visual, and do not need to be synced.
                RockOffsets = new Vector2[MaxRocks];
                for (int i = 0; i < MaxRocks; i++)
                    RockOffsets[i] = Main.rand.NextVector2Circular(10f, 10f);

                // After being read, clear the spear velocity. It is no longer needed.
                Projectile.velocity = Vector2.Zero;

                // Only the owner should choose this.
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
                var rockPlayer = Owner.GetModPlayer<ProfanusPlayer>();

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
                UpdateRocks();

            // Shudder around if close to running out of time, and it has not been fired.
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

        public void UpdateRocks()
        {
            for (int i = 0; i < FormingRocks.Count; i++)
            {
                var rock = FormingRocks[i];

                // Fade out at the very end.
                rock.Opacity = Utils.GetLerpValue(rock.Lifetime, rock.Lifetime - 10f, rock.Time, true);

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

                int driftTime = ProfanusRockParticle.BaseDriftTime + driftDelays[i % (driftDelays.Length - 1)] * 3;
                if (rock.Time <= driftTime)
                {
                    rock.Position += rock.Velocity;
                    rock.Velocity *= 0.99f;
                }
                else
                {
                    // Rapidly fly towards the chosen point.
                    Vector2 targetPosition = Projectile.Center + RockOffsets[i % (RockOffsets.Length - 1)];
                    float interpolant = CalamityUtils.SineInEasing(Utils.GetLerpValue(driftTime, rock.Lifetime - ProfanusRockParticle.StopAndGlowLength, rock.Time, true), 1);
                    rock.Position = Vector2.Lerp(rock.StartingPosition, targetPosition, interpolant);
                }

                if (rock.Time == driftTime)
                    rock.StartingPosition = rock.Position;

                rock.Update();
                //rock.Time++;
            }

            FormingRocks.RemoveAll(rocks => rocks.Time >= rocks.Lifetime);
        }

        public void DoBehavior_Circling()
        {
            Projectile.Center = Owner.Center - (Owner.GetModPlayer<ProfanusPlayer>().RockTimer / 55f + (Tau * IndexWeAre / ProfanusProjectile.MaxRocks)).ToRotationVector2() * 100f;

            // Start off invisible, and fade in glowing when the rocks are moving inwards.
            if (Timer == RocksLifetime - 10)
                Projectile.Opacity = 1f;

            if (Projectile.Opacity < 0.9f)
                return;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || Main.projectile[i].type != ModContent.ProjectileType<ProfanusProjectile>() || Main.projectile[i].owner != Owner.whoAmI)
                    continue;

                ProfanusProjectile profanus = Main.projectile[i].ModProjectile as ProfanusProjectile;
                if (profanus.CurrentMode is ProfanusProjectile.UseMode.RockThrow && profanus.CurrentState is ProfanusProjectile.UseState.Aiming)
                {
                    AimingOffsetPosition = Projectile.Center - Owner.Center;
                    CurrentState = State.Aiming;
                    Timer = 0f;
                    Projectile.netUpdate = true;
                    break;
                }
            }
        }

        public void DoBehavior_Aiming()
        {
            if (Timer == 1)
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound with { Pitch = 0.9f, Volume = 0.9f }, Projectile.Center);

            float aimBackStrength = Utilities.Saturate(Timer / 20f);
            Vector2 aimBackOffset = Projectile.SafeDirectionTo(Owner.Calamity().mouseWorld) * -30f * aimBackStrength;
            Projectile.Center = Owner.Center + AimingOffsetPosition + aimBackOffset;

            Projectile.rotation += 0.2f * Utils.GetLerpValue(0f, 40f, Timer, true);

            Projectile.Center += Main.rand.NextVector2Circular(1f, 1f) * aimBackStrength;

            // Dunno if either are needed but I'd rather attempt to make it work in mp.
            Projectile.netSpam = 0;
            Projectile.netUpdate = true;

            if (Owner.ActiveItem() == null || Owner.ActiveItem().type != ModContent.ItemType<Profanus>() || !Owner.Calamity().mouseRight && aimBackStrength >= 1f)
            {
                int rockAmount = ActiveRocks(Owner);
                float interlopant = Utilities.Saturate((float)rockAmount / ProfanusProjectile.MaxRocks);
                float shootSpeed = Lerp(30f, 50f, interlopant);
                float damageMultiplier = Lerp(0.5f, 0.75f, interlopant);

                if (interlopant >= 1f)
                {
                    shootSpeed = 40f;
                    damageMultiplier = 0.8f;
                }

                Projectile.damage = (int)(Projectile.damage * damageMultiplier);


                if (Main.myPlayer == Owner.whoAmI)
                    Projectile.velocity = Projectile.SafeDirectionTo(Owner.Calamity().mouseWorld) * shootSpeed;

                Projectile.timeLeft = 300;

                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.95f, Volume = 0.9f }, Projectile.Center);
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
                if (CalamityConfig.Instance.Screenshake)
                    Owner.Infernum_Camera().CurrentScreenShakePower = 2f;

                if (ActiveRocks(Owner) >= 6)
                    RockIsBuffed = true;
                CurrentState = State.Firing;
                Timer = 0f;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_Firing()
        {
            Particle rockParticle = new SandyDustParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 3f, Projectile.height / 3f), Vector2.Zero, Color.SandyBrown,
                Main.rand.NextFloat(0.45f, 0.75f), 30);

            GeneralParticleHandler.SpawnParticle(rockParticle);

            Projectile.rotation += 0.3f;

            if (!RockIsBuffed)
                return;

            NPC target = CalamityUtils.ClosestNPCAt(Projectile.Center, 1200f, true, true);

            if (target == null)
                return;

            Vector2 directionToTarget = Projectile.SafeDirectionTo(target.Center);

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, directionToTarget * 40f, 0.055f);
        }

        public int ActiveRocks()
        {
            int total = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || Main.projectile[i].owner != Owner.whoAmI || Main.projectile[i].type != Type || Main.projectile[i].ModProjectile is not ProfanusRocks rock)
                    continue;

                if (rock.CurrentState == State.Aiming)
                    total++;
            }
            return total;
        }

        public static int ActiveRocks(Player owner)
        {
            int total = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || Main.projectile[i].owner != owner.whoAmI || Main.projectile[i].type != ModContent.ProjectileType<ProfanusRocks>() || Main.projectile[i].ModProjectile is not ProfanusRocks rock)
                    continue;

                if (rock.CurrentState is State.Aiming || (rock.CurrentState is State.Firing && rock.Timer < 30f))
                    total++;
            }
            return total;
        }

        public static int PassiveRocks(Player owner)
        {
            int total = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || Main.projectile[i].owner != owner.whoAmI || Main.projectile[i].type != ModContent.ProjectileType<ProfanusRocks>() || Main.projectile[i].ModProjectile is not ProfanusRocks rock)
                    continue;

                if (rock.CurrentState is State.Circling or State.Aiming)
                    total++;
            }
            return total;
        }

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
                float interpolant = Utils.GetLerpValue(rock.Lifetime - ProfanusRockParticle.StopAndGlowLength, rock.Lifetime - ProfanusRockParticle.StopAndGlowLength * 0.5f, rock.Time, true);
                float interpolantGlow = Utils.GetLerpValue(rock.Lifetime - ProfanusRockParticle.StopAndGlowLength, rock.Lifetime, rock.Time, true);

                Rectangle frame = texture.Frame(1, rock.FrameVariants, 0, rock.Variant);

                int backglowAmount = 12;
                for (int j = 0; j < 12; j++)
                {
                    Vector2 backglowOffset = (TwoPi * j / backglowAmount).ToRotationVector2() * 2f;
                    Color backglowColor = WayfinderSymbol.Colors[1] * rock.Opacity;
                    Main.EntitySpriteDraw(texture, rock.Position - Main.screenPosition + backglowOffset, frame, backglowColor with { A = 0 } * rock.Opacity, rock.Rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
                }

                Main.spriteBatch.Draw(texture, rock.Position - Main.screenPosition, frame, rock.Color * rock.Opacity * (1- interpolant), rock.Rotation, frame.Size() * 0.5f, rock.Scale, SpriteEffects.None, 0f);

                // Glow red hot when dying, to form the actual rock.
                if (rock.Time >= rock.Lifetime - ProfanusRockParticle.StopAndGlowLength)
                {
                    Color glowColor = WayfinderSymbol.Colors[1];
                    for (int j = 0; j < 3; j++)
                        Main.EntitySpriteDraw(texture, rock.Position - Main.screenPosition, frame, glowColor with { A = 0 } * 20f * interpolant, rock.Rotation, frame.Size() * 0.5f, rock.Scale, SpriteEffects.None, 0);


                    Texture2D bloom = InfernumTextureRegistry.BloomFlare.Value;//ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
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

            if (CurrentState is State.Aiming)
            {
                Texture2D invis = InfernumTextureRegistry.Invisible.Value;
                float opacity = Utils.GetLerpValue(0, 20, Timer, true);
                float scale = 400f * Lerp(1f, 2.5f, Utils.GetLerpValue(200f, 800f, Projectile.Distance(Owner.Calamity().mouseWorld) * 2f, true));
                Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;
                laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
                laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);
                laserScopeEffect.Parameters["mainOpacity"].SetValue(Pow(opacity, 0.5f));
                laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(340f));
                laserScopeEffect.Parameters["laserAngle"].SetValue((Owner.Calamity().mouseWorld - Projectile.Center).ToRotation() * -1f);
                laserScopeEffect.Parameters["laserWidth"].SetValue(0.0015f + Pow(opacity, 5f) * (Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.002f + 0.002f));
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

            int backglowAmount = 12;
            for (int i = 0; i < 12; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / backglowAmount).ToRotationVector2() * 5f;
                Color backglowColor = WayfinderSymbol.Colors[1] * lightColor.ToGreyscale();
                Main.EntitySpriteDraw(texture, drawPosition + backglowOffset, null, backglowColor with { A = 0 } * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor) * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            bool drawInitialGlowyShit = (CurrentState is State.Circling && Timer >= RocksLifetime - 20 && Timer <= RocksLifetime + 20) || RockIsBuffed;

            if (drawInitialGlowyShit)
            {
                float opacity = RockIsBuffed ? Utils.GetLerpValue(0f, 10f, Timer, true) : Utils.GetLerpValue(RocksLifetime + 20, RocksLifetime, Timer, true);

                Color backglowColor = WayfinderSymbol.Colors[1];
                for (int i = 0; i < 2; i++)
                    Main.EntitySpriteDraw(texture, drawPosition , null, backglowColor with { A = 0 } * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            if (FormingRocks.Any())
                DrawRocks();
            return false;
        }
        #endregion
    }
}
