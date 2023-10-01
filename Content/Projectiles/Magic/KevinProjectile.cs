using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.Items.Weapons.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Magic
{
    public class KevinProjectile : ModProjectile
    {
        public ManagedRenderTarget LightningTarget
        {
            get;
            private set;
        }

        public ManagedRenderTarget TemporaryAuxillaryTarget
        {
            get;
            private set;
        }

        public Vector2 LightningCoordinateOffset
        {
            get;
            set;
        }

        // This stores the sound slot of the electric sound it makes, so it may be properly updated in terms of position and looped.
        public SlotId ElectricitySound;

        public Player Owner => Main.player[Projectile.owner];

        // This is necessary because render target work must be done.
        public bool CanUpdate
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value.ToInt();
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float TargetIndex => ref Projectile.ai[1];

        public ref float LightningDistance => ref Projectile.localAI[0];

        public static Color LightningColor => Color.Lerp(Color.Cyan, Color.DeepSkyBlue, 0.7f);

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Kevin");
            Main.projFrames[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 38;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 7200;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void OnSpawn(IEntitySource source)
        {
            LightningTarget = new(false, (_, _2) =>
            {
                return new(Main.instance.GraphicsDevice, Kevin.LightningArea, Kevin.LightningArea, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);
            });
            TemporaryAuxillaryTarget = new(false, (_, _2) =>
            {
                return new(Main.instance.GraphicsDevice, Kevin.LightningArea, Kevin.LightningArea, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);
            });
            RenderTargetManager.RenderTargetUpdateLoopEvent += UpdateLightningField;
            LightningCoordinateOffset = Vector2.Zero;
        }

        public override void AI()
        {
            // Die if no longer holding the click button or otherwise cannot use the item.
            if (!Owner.channel || Owner.dead || !Owner.active || Owner.noItems || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            // Stick to the owner.
            Projectile.Center = Owner.MountedCenter;

            // Decide a target every frame.
            TargetIndex = -1;
            NPC potentialTarget = Projectile.Center.ClosestNPCAt(Kevin.TargetingDistance);
            if (potentialTarget != null)
            {
                TargetIndex = potentialTarget.whoAmI;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(potentialTarget.Center), 0.6f);
                LightningDistance = Projectile.Distance(potentialTarget.Center);
            }

            // If no target was found, aim the lightning in the direction of the mouse.
            else if (Main.myPlayer == Owner.whoAmI)
            {
                Vector2 aimDirection = Projectile.SafeDirectionTo(Main.MouseWorld);
                if (Projectile.velocity != aimDirection)
                {
                    LightningDistance = Projectile.Distance(Main.MouseWorld) * Main.rand.NextFloat(0.9f, 1.1f);
                    Projectile.velocity = aimDirection;
                    Projectile.netUpdate = true;
                }
            }

            // Clamp the lightning distance so that it does not exceed the range of the render target.
            float maxLightningRange = Kevin.LightningArea * 0.5f - 8f;
            if (LightningDistance >= maxLightningRange)
                LightningDistance = maxLightningRange;

            // Update the sound's position.
            if (SoundEngine.TryGetActiveSound(ElectricitySound, out var t) && t.IsPlaying)
                t.Position = Projectile.Center;
            else
                ElectricitySound = SoundEngine.PlaySound(InfernumSoundRegistry.KevinElectricitySound with { Volume = 0.6f }, Projectile.Center);

            // Continuously use mana. If the owner has no more mana to use, destroy this projectile.
            if (Time % 5f == 4f && !Owner.CheckMana(Owner.ActiveItem(), -1, true))
                Projectile.Kill();

            AdjustPlayerValues();

            // Decide frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 3 % Main.projFrames[Type];

            Time++;
        }

        public void AdjustPlayerValues()
        {
            Projectile.timeLeft = 2;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();

            Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation();
            Owner.ChangeDir(Projectile.spriteDirection);

            Projectile.Center += Projectile.velocity.SafeNormalize(Vector2.UnitY) * 20f;

            // Update the player's arm directions to make it look as though they're holding the flamethrower.
            float frontArmRotation = Projectile.rotation - PiOver2;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);

            Projectile.rotation += PiOver2;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return projHitbox.Distance(targetHitbox.Center()) <= Kevin.TargetingDistance;
        }

        public void UpdateLightningField()
        {
            // Update the lightning effect every frame.
            Main.instance.GraphicsDevice.SetRenderTarget(TemporaryAuxillaryTarget.Target);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);

            Main.Rasterizer = RasterizerState.CullNone;
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);

            Main.instance.GraphicsDevice.Textures[0] = LightningTarget.Target;
            Main.instance.GraphicsDevice.Textures[1] = InfernumTextureRegistry.WavyNoise.Value;

            float angularOffset = Projectile.oldRot[0] - Projectile.oldRot[1];
            Vector2 lightningDirection = Projectile.velocity.SafeNormalize(Vector2.Zero);

            LightningCoordinateOffset += lightningDirection * -0.003f;

            // Supply a bunch of parameters to the shader.
            var shader = InfernumEffectsRegistry.KevinLightningShader.Shader;
            shader.Parameters["uColor"].SetValue(LightningColor.ToVector3());
            shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["actualSize"].SetValue(LightningTarget.Target.Size());
            shader.Parameters["screenMoveOffset"].SetValue(Main.screenPosition - Main.screenLastPosition);
            shader.Parameters["lightningDirection"].SetValue(lightningDirection);
            shader.Parameters["lightningAngle"].SetValue(angularOffset);
            shader.Parameters["noiseCoordsOffset"].SetValue(LightningCoordinateOffset);
            shader.Parameters["currentFrame"].SetValue(Main.GameUpdateCount);
            shader.Parameters["lightningLength"].SetValue(LightningDistance / LightningTarget.Target.Width + 0.5f);
            shader.Parameters["zoomFactor"].SetValue(15f);
            shader.Parameters["bigArc"].SetValue(Main.rand.NextBool(5));
            shader.CurrentTechnique.Passes["UpdatePass"].Apply();

            // Draw the result to the next frame and copy it over.
            Main.spriteBatch.Draw(LightningTarget.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
            Main.spriteBatch.End();

            Main.Rasterizer = RasterizerState.CullNone;
            LightningTarget.Target.CopyContentsFrom(TemporaryAuxillaryTarget.Target);
        }

        // Kevin should be at maximum brightness at all times.
        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            if (LightningTarget != null || !LightningTarget.IsDisposed)
            {

                Main.Rasterizer = RasterizerState.CullNone;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(LightningTarget.Target, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation - PiOver2, LightningTarget.Target.Size() * 0.5f, Projectile.scale, 0, 0f);
                Main.spriteBatch.ResetBlendState();
            }

            Texture2D kevin = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = new(0, Projectile.height * Projectile.frame, Projectile.width, Projectile.height);
            Vector2 origin = frame.Size() * 0.5f;
            Main.spriteBatch.Draw(kevin, drawPosition, frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool? CanHitNPC(NPC target) => target.whoAmI == TargetIndex ? null : false;

        // Apply hit effects to the affected NPC.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 8f);
                Color sparkColor = Color.Lerp(LightningColor, Color.White, Main.rand.NextFloat(0.5f));
                GeneralParticleHandler.SpawnParticle(new SparkParticle(target.Center, sparkVelocity, false, 45, 0.8f, sparkColor));

                sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 23f);
                Color arcColor = Color.Lerp(LightningColor, Color.White, Main.rand.NextFloat(0.1f, 0.65f));
                GeneralParticleHandler.SpawnParticle(new ElectricArc(target.Center, sparkVelocity, arcColor, 0.76f, 27));
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                RenderTargetManager.RenderTargetUpdateLoopEvent -= UpdateLightningField;
                Main.QueueMainThreadAction(() =>
                {
                    LightningTarget?.Dispose();
                    TemporaryAuxillaryTarget?.Dispose();
                });
            }

            if (SoundEngine.TryGetActiveSound(ElectricitySound, out var t) && t.IsPlaying)
                t.Stop();
        }
    }
}
