using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Worldgen;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using WayfinderItem = InfernumMode.Content.Items.Misc.Wayfinder;

namespace InfernumMode.Content.Projectiles.Wayfinder
{
    public class WayfinderHoldout : ModProjectile
    {
        public enum UseContext
        {
            Teleport = 0,
            Create = 1,
            Destroy = 2
        }

        #region Fields + Properties
        public ref Player Owner => ref Main.player[Projectile.owner];
        public ref float Time => ref Projectile.ai[0];
        public static bool IsGateSet => WorldSaveSystem.WayfinderGateLocation != Vector2.Zero;

        public UseContext CurrentUseContext => (UseContext)Projectile.ai[1];

        public Vector2 CenterOffset;

        public SlotId SoundSlot;

        // Constants for readability.
        public const int FadeOutTime = 25;

        public const float SpinDelay = 20;
        public const float SpinMaxTime = 45;
        public const int TeleportationTime = 90;
        public const int TeleportMaxTime = 95;

        public const int UpwardsMovementTime = 80;
        public const int CreationTime = 90;
        public const int CreateMaxTime = 120;

        public const int DestructionTime = 90;
        public const int DestroyMaxTime = 120;
        #endregion

        #region Overrides
        public override string Texture => WayfinderItem.GetTexture();

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Wayfinder");
            Main.projFrames[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 56;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 7200;
            Projectile.penetrate = -1;
            Projectile.hide = true;
            Projectile.Opacity = 0;
        }

        public override void AI()
        {
            Item heldItem = Owner.ActiveItem();

            // Die if no longer holding the click button or otherwise cannot use the item.
            if (!Owner.channel || Owner.dead || !Owner.active || Owner.noItems || Owner.CCed || heldItem is null)
            {
                Projectile.Kill();
                return;
            }

            // Fade in.
            if (Time < 60)
                Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            // Stick to the owner.
            Vector2 ownerCenter = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Projectile.Center = ownerCenter + CenterOffset;
            AdjustPlayerValues();
            if (CenterOffset == default)
                CenterOffset = new Vector2(20 * Projectile.spriteDirection, -15);

            // Frames
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 8)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
                Projectile.frameCounter = 0;
            }

            // Update the sound position.
            if (SoundEngine.TryGetActiveSound(SoundSlot, out var sound) && sound.IsPlaying)
                sound.Position = Owner.Center;

            // Perform the correct behavior
            switch (CurrentUseContext)
            {
                case UseContext.Teleport:
                    DoBehavior_Teleport();
                    break;
                case UseContext.Create:
                    DoBehavior_Create();
                    break;
                case UseContext.Destroy:
                    DoBehavior_Destroy();
                    break;
            }

            if (CurrentUseContext != UseContext.Teleport)
            {
                if (Time >= 120 - FadeOutTime)
                {
                    float opacityInterpolant = (Time - (120 - FadeOutTime)) / FadeOutTime;
                    Projectile.Opacity = Lerp(1f, 0f, opacityInterpolant);
                }
            }
            Time++;
        }

        public override bool? CanDamage() => false;

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
        public override void OnKill(int timeLeft)
        {
            if (Time > 90)
                return;
            if (SoundEngine.TryGetActiveSound(SoundSlot, out var t) && t.IsPlaying)
                t.Stop();
        }
        #endregion

        #region Methods
        public void AdjustPlayerValues()
        {
            Projectile.timeLeft = 2;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();

            if (Main.myPlayer == Projectile.owner)
            {
                if (Projectile.velocity != Projectile.oldVelocity)
                    Projectile.netUpdate = true;
                Projectile.spriteDirection = Owner.direction;
            }

            Owner.ChangeDir(Projectile.spriteDirection);

            Projectile.Center += Projectile.velocity * 20f;

            // Update the player's arm directions to make it look as though they're holding the flamethrower.
            float frontArmRotation = -Projectile.velocity.ToRotation() + Owner.direction * -0.4f;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);
        }

        public void DoBehavior_Teleport()
        {
            int dustSpawnRate = (int)Lerp(8, 1, Time / TeleportMaxTime);
            Vector2 baseOffset = new(20 * Projectile.spriteDirection, -15);

            // Why is this a problem?
            Owner.fallStart = (int)(Owner.position.Y / 16f);

            if (IsGateSet && Main.myPlayer == Projectile.owner)
                MoonlordDeathDrama.RequestLight(Utils.GetLerpValue(30f, 64f, Time, true), Owner.Center);

            if (Time == 0)
            {
                if (IsGateSet)
                    SoundSlot = SoundEngine.PlaySound(InfernumSoundRegistry.WayfinderTeleport with { Volume = 0.7f }, Owner.Center);
                else
                    SoundSlot = SoundEngine.PlaySound(InfernumSoundRegistry.WayfinderFail with { Volume = 0.7f }, Owner.Center);
            }

            // Periodically emit dust, increasing over time to indicate charging up
            if (Main.netMode is not NetmodeID.MultiplayerClient && Time % dustSpawnRate == 0 && Time < TeleportationTime)
            {
                CreateFireDust(Projectile.Center, Main.rand.NextFloat(2f, 2.5f));
            }

            if (Time is >= SpinDelay and < TeleportationTime)
            {
                if (Time <= SpinMaxTime)
                {
                    float spinInterpolant = (Time - SpinDelay) / (SpinMaxTime - SpinDelay);
                    Projectile.rotation = Lerp(0, -0.75f * Projectile.spriteDirection, spinInterpolant);
                    CenterOffset = Vector2.Lerp(baseOffset, baseOffset + new Vector2(5 * Projectile.spriteDirection, -10), spinInterpolant);
                }
                else
                {
                    float moveInterpolant = (Time - SpinMaxTime) / (TeleportationTime - SpinMaxTime);
                    CenterOffset.Y = Lerp(-25, -45, moveInterpolant);
                }
            }
            if (Time is > 70 and < TeleportationTime && IsGateSet)
                CreateFlameExplosion(Owner.Center, 10, 20, 2, 0.6f, 45);

            // Teleport the player at the appropriate time.
            if (Time == TeleportationTime)
            {
                if (WorldSaveSystem.WayfinderGateLocation != Vector2.Zero)
                {
                    Owner.Bottom = WorldSaveSystem.WayfinderGateLocation;
                    Owner.velocity = Vector2.Zero;
                    Owner.noFallDmg = true;
                    CreateFlameExplosion(Owner.Center, 10, 20, 40, 0.6f, 60);
                }
                else
                    CombatText.NewText(Owner.Hitbox, Color.Gold, Language.GetTextValue($"Mods.InfernumMode.Status.WayfinderGateNotSet"), true);
            }
            // This has to be delayed due to the projectile position not being updated on the same frame as the owner position.
            if (Time == TeleportationTime + 1 && IsGateSet)
            {
                Vector2 tip = GetTip();
                for (int i = 0; i < 60; i++)
                {
                    CreateFireDust(tip, Main.rand.NextFloat(4f, 8f));
                    Color color = Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : WayfinderSymbol.Colors[2];
                    Particle particle = new GenericSparkle(tip, (Vector2.One * Main.rand.NextFloat(1f, 5f)).RotatedByRandom(TwoPi), color, WayfinderSymbol.Colors[0], Main.rand.NextFloat(0.2f, 0.4f), 60, Main.rand.NextFloat(-0.1f, 0.1f));
                    GeneralParticleHandler.SpawnParticle(particle);
                }
            }
            if (Time >= TeleportMaxTime)
            {
                float opacityInterpolant = (Time - TeleportMaxTime) / FadeOutTime;
                Projectile.Opacity = Lerp(1f, 0f, opacityInterpolant);
            }
            if (Time >= TeleportMaxTime + FadeOutTime)
            {
                Projectile.Kill();
            }
        }

        public void DoBehavior_Create()
        {
            int initialMovementTime = 35;
            int initialRotationDelay = 5;
            Vector2 baseOffset = new(20 * Projectile.spriteDirection, -15);
            int dustSpawnRate = (int)Lerp(8, 2, Time / CreateMaxTime);

            if (Time == 0)
                SoundSlot = SoundEngine.PlaySound(InfernumSoundRegistry.WayfinderCreateSound with { Volume = 0.7f }, Owner.Center);

            // Periodically emit dust, increasing over time to indicate charging up
            if (Main.netMode is not NetmodeID.MultiplayerClient && Time % dustSpawnRate == 0 && Time < CreationTime)
                CreateFireDust(Projectile.Center, Main.rand.NextFloat(2f, 2.5f));

            // The initial movement. The Wayfinder is spinning to face the ground, while moving out in front of the player in preperation for stabbing the ground.
            if (Time <= initialMovementTime)
            {
                // Movement
                CenterOffset = Vector2.Lerp(baseOffset, baseOffset + new Vector2(5 * Projectile.spriteDirection, -10), Time / 35);
                // Rotation.
                if (Time >= initialRotationDelay)
                {
                    float interpolant = (Time - initialRotationDelay) / initialMovementTime - initialRotationDelay;
                    Projectile.rotation = Lerp(0, PiOver2 * 1.57f * Projectile.spriteDirection, interpolant);
                }
            }
            // Slowly rise up, before slamming down.
            else if (Time < CreationTime)
            {
                float y;
                if (Time < UpwardsMovementTime)
                    y = Lerp(0, -15, (Time - initialMovementTime) / (UpwardsMovementTime - initialMovementTime));
                else
                    y = Lerp(-15, 25, (Time - UpwardsMovementTime) / (CreationTime - UpwardsMovementTime));
                CenterOffset.Y = -25 + y;
            }
            // Set the new gate location.
            if (Time == CreationTime)
            {
                Vector2 tip = GetTip();
                // Find the tile at the tip. The -5 here is because the tip is slightly embedded into the ground.
                Tile tipTile = Main.tile[(int)tip.X / 16, ((int)tip.Y - 5) / 16];
                if (tipTile.IsTileSolid())
                {
                    // If its a solid tile, find the closest tile under the player and set the location to that.
                    WorldUtils.Find(new Vector2(Owner.position.X, Owner.position.Y).ToTileCoordinates(), Searches.Chain(new Searches.Down(200), new GenCondition[]
                    {
                        new CustomTileConditions.IsSolidOrSolidTop(),
                        new CustomTileConditions.ActiveAndNotActuated()
                    }), out Point playerBottom);

                    if (playerBottom != GenSearch.NOT_FOUND)
                        WorldSaveSystem.WayfinderGateLocation = playerBottom.ToWorldCoordinates(8, 0);
                    else
                        return;
                }
                else
                {
                    // Else set the location to the tip.
                    WorldUtils.Find(new Vector2(tip.X, tip.Y).ToTileCoordinates(), Searches.Chain(new Searches.Down(200), new GenCondition[]
                    {
                        new CustomTileConditions.IsSolidOrSolidTop(),
                        new CustomTileConditions.ActiveAndNotActuated()
                    }), out Point newBottom);

                    if (newBottom != GenSearch.NOT_FOUND)
                        WorldSaveSystem.WayfinderGateLocation = newBottom.ToWorldCoordinates(8, 0);
                    else
                        return;
                }

                Owner.Infernum_Camera().CurrentScreenShakePower = 5;
                for (int i = 0; i < 60; i++)
                {
                    CreateFireDust(tip, Main.rand.NextFloat(4f, 8f));
                    Color color = Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : WayfinderSymbol.Colors[2];
                    Particle particle = new GenericSparkle(WorldSaveSystem.WayfinderGateLocation, (Vector2.One * Main.rand.NextFloat(1f, 5f)).RotatedByRandom(TwoPi), color, WayfinderSymbol.Colors[0], Main.rand.NextFloat(0.2f, 0.4f), 60, Main.rand.NextFloat(-0.1f, 0.1f));
                    GeneralParticleHandler.SpawnParticle(particle);
                }

                // Create a bunch of fire at the creation point.
                CreateFlameExplosion(WorldSaveSystem.WayfinderGateLocation, 40, 40, 40, 1);

                // Kill any existing gates, one will be recreated in WorldUpdatingSystem. This allows for creation effects for the gate.#
                for (int i = 0; i < Main.projectile.Length; i++)
                {
                    Projectile proj = Main.projectile[i];

                    if (proj.type == ModContent.ProjectileType<WayfinderGate>() && proj.active)
                    {
                        CreateFlameExplosion(proj.Center, 40, 40, 40, 1);
                        proj.active = false;
                        break;
                    }
                }
            }

            if (Time >= CreateMaxTime)
                Projectile.Kill();
        }

        public void DoBehavior_Destroy()
        {
            int dustSpawnRate = (int)Lerp(10, 1, Time / DestructionTime);
            int flameSpawnAmount = (int)Lerp(2, 6, Time / DestructionTime);

            if (Time == 0)
            {
                if (IsGateSet)
                    SoundSlot = SoundEngine.PlaySound(InfernumSoundRegistry.WayfinderDestroySound with { Volume = 0.7f }, Owner.Center);
                else
                    SoundSlot = SoundEngine.PlaySound(InfernumSoundRegistry.WayfinderFail with { Volume = 0.7f }, Owner.Center);
            }

            Vector2 tip = GetTip();
            if (Main.netMode is not NetmodeID.MultiplayerClient && Time % dustSpawnRate == 0 && Time < DestructionTime)
            {
                CreateFireDust(Projectile.Center, Main.rand.NextFloat(2f, 2.5f));
                if (IsGateSet)
                    CreateFlameExplosion(Projectile.Center + new Vector2(5 * Projectile.spriteDirection, -5), 10, 15, flameSpawnAmount, 0.1f, 45);
            }

            if (Time == DestructionTime)
            {
                if (IsGateSet)
                {
                    // Set the dreamgate location to its default value.
                    WorldSaveSystem.WayfinderGateLocation = Vector2.Zero;
                    // And kill the projectile.
                    for (int i = 0; i < Main.projectile.Length; i++)
                    {
                        Projectile projectile = Main.projectile[i];
                        if (Main.projectile[i].type == ModContent.ProjectileType<WayfinderGate>())
                        {
                            CreateFlameExplosion(projectile.Center, 40, 40, 40, 1);
                            projectile.active = false;
                            break;
                        }
                    }

                    for (int i = 0; i < 60; i++)
                    {
                        CreateFireDust(tip, Main.rand.NextFloat(4f, 8f));
                        Color color = Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : WayfinderSymbol.Colors[2];
                        Particle particle = new GenericSparkle(tip, (Vector2.One * Main.rand.NextFloat(1f, 5f)).RotatedByRandom(TwoPi), color, WayfinderSymbol.Colors[0], Main.rand.NextFloat(0.2f, 0.4f), 60, Main.rand.NextFloat(-0.1f, 0.1f));
                        GeneralParticleHandler.SpawnParticle(particle);
                    }
                }
                else
                    CombatText.NewText(Owner.Hitbox, Color.Gold, Language.GetTextValue($"Mods.InfernumMode.Status.WayfinderGateNotSet"), true);
            }

            if (Time >= DestroyMaxTime)
                Projectile.Kill();
        }

        public static void CreateFireDust(Vector2 position, float speed, Vector2? positionOffset = null)
        {
            positionOffset ??= Vector2.UnitX * Main.rand.NextFloatDirection() * 0.05f;

            int dustType = WorldSaveSystem.WayfinderGateLocation == Vector2.Zero ? DustID.Torch : DustID.IceTorch;

            Dust fire = Dust.NewDustPerfect(position + positionOffset.Value, dustType, Vector2.One, 0, Color.White * 0.1f, 2f);
            fire.velocity = fire.velocity.RotatedByRandom(TwoPi) * speed;
            fire.fadeIn = 0.6f;
            fire.noGravity = true;
            Color color = Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : WayfinderSymbol.Colors[2];
            Particle particle = new GenericSparkle(position, (Vector2.One * Main.rand.NextFloat(2f, 3)).RotatedByRandom(TwoPi), color, WayfinderSymbol.Colors[0], Main.rand.NextFloat(0.15f, 0.35f), 30, Main.rand.NextFloat(-0.1f, 0.1f));
            GeneralParticleHandler.SpawnParticle(particle);
        }
        public static void CreateFlameExplosion(Vector2 position, float maxPositionOffsetX, float maxPositionOffsetY, int amount, float baseScale, int lifeTime = 90)
        {
            for (int j = 0; j < amount; j++)
            {
                Vector2 firePosition = position + Main.rand.NextVector2Circular(maxPositionOffsetX, maxPositionOffsetY);
                Color fireColor = Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], Main.rand.NextFloat(0.3f, 0.7f));
                float fireScale = Main.rand.NextFloat(baseScale, baseScale + 0.3f);
                float fireRotationSpeed = Main.rand.NextFloat(-0.05f, 0.05f);

                var particle = new HeavySmokeParticle(firePosition, Vector2.Zero, fireColor, lifeTime, fireScale, 1, fireRotationSpeed, true, 0f, true);
                GeneralParticleHandler.SpawnParticle(particle);
            }
        }
        public Vector2 GetTip()
        {
            Vector2 tip;
            if (Projectile.rotation is not 0)
            {
                tip = Projectile.position + new Vector2(Projectile.width, 0);
                tip = tip.RotatedBy(Projectile.rotation * Projectile.spriteDirection, Projectile.Center) + new Vector2(-2, -13);
                return tip;
            }
            float width;
            if (Projectile.spriteDirection == -1)
                width = 0;
            else
                width = Projectile.width;
            tip = Projectile.position + new Vector2(width, 0);
            tip = tip.RotatedBy(Projectile.rotation * Projectile.spriteDirection, Projectile.Center);
            return tip;
        }
        #endregion

        #region Drawing
        public void DrawBackglow(Texture2D texture, float distance, Rectangle sourceRectangle, Vector2 origin, SpriteEffects spriteEffects)
        {
            if (!IsGateSet)
                distance *= 0.5f;
            for (int i = 0; i < 12; i++)
            {
                Vector2 afterimageOffset = (TwoPi * i / 12f).ToRotationVector2() * distance;
                Color afterimageColor = new Color(1f, 0.6f, 0.4f, 0f) * 0.7f;
                Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + afterimageOffset, sourceRectangle, Projectile.GetAlpha(afterimageColor), Projectile.rotation, origin, Projectile.scale * 0.75f, spriteEffects, 0f);
            }
        }

        public void DrawShaderOverlay(Texture2D texture, Rectangle sourceRectangle, Color lightColor, float opacity, Vector2 origin, SpriteEffects spriteEffects)
        {
            Main.spriteBatch.EnterShaderRegion();

            DrawData drawData = new(texture, Projectile.Center - Main.screenPosition, sourceRectangle, lightColor * opacity * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale * 0.75f, spriteEffects, 0);
            InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/WayfinderLayer"));
            InfernumEffectsRegistry.RealityTear2Shader.Apply(drawData);

            drawData.Draw(Main.spriteBatch);

            Main.spriteBatch.ExitShaderRegion();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glowTexture = ModContent.Request<Texture2D>(Texture + "Glow").Value;
            Rectangle sourceRectangle = texture.Frame(1, Main.projFrames[Projectile.type], frameY: Projectile.frame);
            Vector2 origin = sourceRectangle.Size() * 0.5f;

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Owner.direction == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            // Backglow.
            if (CurrentUseContext == UseContext.Teleport && Time >= SpinDelay)
            {
                float interpolant = (Time - SpinDelay) / (TeleportationTime - SpinDelay);
                float distance = Lerp(0f, 4f, interpolant);
                DrawBackglow(texture, distance, sourceRectangle, origin, spriteEffects);
            }
            else if (CurrentUseContext == UseContext.Create && Time is > UpwardsMovementTime)
            {
                float interpolant = (Time - UpwardsMovementTime) / (CreationTime - UpwardsMovementTime);
                float distance = Lerp(0f, 3f, interpolant);
                distance = Clamp(distance, 0, 4);
                DrawBackglow(texture, distance, sourceRectangle, origin, spriteEffects);
            }
            else if (CurrentUseContext == UseContext.Destroy && Time is < DestroyMaxTime)
            {
                float interpolant = Time / DestroyMaxTime;
                float distance = Lerp(0f, 8f, interpolant);
                //distance = Clamp(distance, 0, );
                DrawBackglow(texture, distance, sourceRectangle, origin, spriteEffects);
            }

            // Main drawing
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, lightColor * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale * 0.75f, spriteEffects, 0);
            Main.EntitySpriteDraw(glowTexture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale * 0.75f, spriteEffects, 0);

            // Shader overlay.
            if (CurrentUseContext == UseContext.Teleport && Time >= SpinMaxTime && IsGateSet)
            {
                float interpolant = (Time - SpinMaxTime) / (TeleportationTime - SpinMaxTime);
                float opacity = Lerp(0, 0.2f, interpolant);
                DrawShaderOverlay(texture, sourceRectangle, lightColor, opacity, origin, spriteEffects);
            }
            else if (CurrentUseContext == UseContext.Create && Time is > UpwardsMovementTime)
            {
                float interpolant = (Time - UpwardsMovementTime) / (CreationTime - UpwardsMovementTime);
                float opacity = Lerp(0, 0.2f, interpolant);
                DrawShaderOverlay(texture, sourceRectangle, lightColor, opacity, origin, spriteEffects);
            }
            else if (CurrentUseContext == UseContext.Destroy && Time is < DestroyMaxTime && IsGateSet)
            {
                float interpolant = Time / DestroyMaxTime;
                float opacity = Lerp(-0.4f, 0.4f, interpolant);
                DrawShaderOverlay(texture, sourceRectangle, lightColor, opacity, origin, spriteEffects);
            }
            return false;
        }
        #endregion
    }
}
