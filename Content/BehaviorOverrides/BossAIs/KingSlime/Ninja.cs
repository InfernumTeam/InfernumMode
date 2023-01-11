using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.SlimeGod;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.KingSlime
{
    public class Ninja : ModNPC
    {
        public PrimitiveTrailCopy FireDrawer;
        public Player Target => Main.player[NPC.target];
        public bool HasCreatedSlimeExplosion
        {
            get => NPC.localAI[1] == 1f;
            set => NPC.localAI[1] = value.ToInt();
        }
        public ref float Time => ref NPC.ai[0];
        public ref float ShurikenShootCountdown => ref NPC.ai[1];
        public ref float TimeOfFlightCountdown => ref NPC.ai[2];
        public ref float TeleportCountdown => ref NPC.ai[3];
        public ref float KatanaUseTimer => ref NPC.Infernum().ExtraAI[0];
        public ref float KatanaUseLength => ref NPC.Infernum().ExtraAI[1];
        public ref float KatanaRotation => ref NPC.Infernum().ExtraAI[2];
        public ref float AttackDelayFuckYou => ref NPC.Infernum().ExtraAI[3];
        public ref float StuckTimer => ref NPC.localAI[0];
        public static ref float CurrentTeleportDirection => ref Main.npc[NPC.FindFirstNPC(NPCID.KingSlime)].Infernum().ExtraAI[6];
        public ref float SyncedDeathTimer => ref NPC.Infernum().ExtraAI[7];
        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Ninja");
            Main.npcFrameCount[NPC.type] = 9;
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 9;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = AIType = -1;
            NPC.width = NPC.height = 26;
            NPC.damage = 5;
            NPC.lifeMax = 100;
            NPC.knockBackResist = 0f;
            NPC.dontTakeDamage = true;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.netAlways = true;
            NPC.Calamity().canBreakPlayerDefense = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(StuckTimer);

        public override void ReceiveExtraAI(BinaryReader reader) => StuckTimer = reader.ReadSingle();

        public override void AI()
        {
            // Disappear if the main boss is not present.
            if (!NPC.AnyNPCs(NPCID.KingSlime))
            {
                Utils.PoofOfSmoke(NPC.Center);
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            // Create an explosion of slime and play a slimy sound to indicate that he escaped the King Slime.
            if (!HasCreatedSlimeExplosion)
            {
                SoundEngine.PlaySound(SlimeGodCore.PossessionSound, NPC.Center);
                for (int i = 0; i < 30; i++)
                {
                    Dust slime = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(20f, 20f), 4);
                    slime.color = new Color(78, 136, 255, 80);
                    slime.noGravity = true;
                    slime.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 14.5f);
                    slime.scale = 2.3f;
                }
                HasCreatedSlimeExplosion = true;
            }

            NPC.damage = KatanaUseTimer > 0 ? 115 : 0;
            NPC.noTileCollide = NPC.Bottom.Y < Target.Top.Y;
            AttackDelayFuckYou++;

            if (MathHelper.Distance(NPC.position.X, NPC.oldPosition.X) < 2f)
                StuckTimer += 2f;

            NPC.TargetClosest();

            Tile tileBelow = Framing.GetTileSafely(NPC.Bottom);
            bool onSolidGround = WorldGen.SolidTile(tileBelow);
            if (Main.tileSolidTop[tileBelow.TileType] && tileBelow.HasUnactuatedTile)
                onSolidGround = true;
            float horizontalDistanceFromTarget = MathHelper.Distance(Target.Center.X, NPC.Center.X);

            if (SyncedDeathTimer > 0)
            {
                DoBehaviorDeathAnimation();
                SyncedDeathTimer++;
                return;
            }

            if (ShurikenShootCountdown > 0f)
            {
                // Shoot 3 shurikens before the timer resets.
                if (ShurikenShootCountdown == 1f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int shurikenCount = (int)MathHelper.Lerp(2f, 6f, Utils.GetLerpValue(300f, 720f, NPC.Distance(Target.Center), true));
                        float shurikenSpeed = 5.5f;
                        if (BossRushEvent.BossRushActive)
                        {
                            shurikenSpeed = 23f;
                            shurikenCount *= 3;
                        }

                        for (int i = 0; i < shurikenCount; i++)
                        {
                            Vector2 shurikenVelocity = NPC.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.Lerp(-0.36f, 0.36f, i / (float)(shurikenCount - 1f))) * shurikenSpeed;
                            Utilities.NewProjectileBetter(NPC.Center + shurikenVelocity, shurikenVelocity, ModContent.ProjectileType<Shuriken>(), 72, 0f);
                        }
                    }

                    SoundEngine.PlaySound(SoundID.Item1, NPC.Center);
                }

                ShurikenShootCountdown--;
            }

            if (TimeOfFlightCountdown > 0f)
            {
                if (NPC.velocity.X != 0f)
                {
                    if (KatanaUseTimer > 0f)
                    {
                        NPC.rotation = KatanaRotation - MathHelper.PiOver2;
                        KatanaRotation += MathHelper.ToRadians(22f) * NPC.spriteDirection;
                        KatanaUseTimer--;
                    }
                    else
                    {
                        NPC.spriteDirection = (NPC.velocity.X > 0f).ToDirectionInt();

                        // Spin when going upward.
                        if (NPC.velocity.Y < 0f)
                            NPC.rotation += NPC.spriteDirection * 0.3f;
                        // And aim footfirst when going downward.
                        // Unless it's April 1st. In which case he becomes a goddamn bouncy ball lmao
                        else if (!Utilities.IsAprilFirst())
                            NPC.rotation = NPC.velocity.ToRotation() - MathHelper.PiOver2;
                    }
                }
                else
                    NPC.rotation = 0f;

                TimeOfFlightCountdown--;
                if (onSolidGround && TimeOfFlightCountdown < 35f)
                    TimeOfFlightCountdown = 0f;

                return;
            }
            else
            {
                NPC.rotation = 0f;
                NPC.spriteDirection = (NPC.velocity.X > 0f).ToDirectionInt();
            }

            if (Time % 150f > 130f)
                NPC.velocity.X *= 0.945f;
            else
                DoRunEffects();

            // Teleport if far from the target or it is typically possible to do so.
            bool canDashTeleport = (!NPC.WithinRange(Target.Center, 850f) || StuckTimer >= 150f) && AttackDelayFuckYou > 150f;

            if (TeleportCountdown > 0f)
            {
                DoTeleportEffects();
                TeleportCountdown--;
                return;
            }

            if (onSolidGround)
                KatanaUseTimer = 0f;

            if (NPC.WithinRange(Target.Center, 260f) && KatanaUseTimer <= 0f && AttackDelayFuckYou > 150f && onSolidGround)
            {
                NPC.spriteDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();
                NPC.velocity = NPC.SafeDirectionTo(Target.Center) * 8f;
                NPC.velocity.Y -= 4f;
                KatanaRotation = 0f;
                KatanaUseTimer = KatanaUseLength = 54f;
                ShurikenShootCountdown = 0f;
                NPC.netUpdate = true;

                SoundEngine.PlaySound(SoundID.Item1, NPC.Center);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && canDashTeleport)
            {
                StuckTimer = 0f;
                DoJump(12f);
                TeleportCountdown = 70f;
                NPC.netUpdate = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && horizontalDistanceFromTarget > 320f && Time % 60f == 59f && onSolidGround)
            {
                float jumpSpeed = (float)Math.Sqrt(horizontalDistanceFromTarget) * 0.5f;
                if (jumpSpeed >= 11f)
                    jumpSpeed = 11f;
                jumpSpeed *= Main.rand.NextFloat(1.15f, 1.4f);
                DoJump(jumpSpeed);

                NPC.netUpdate = true;
            }

            Time++;
        }

        public void DoBehaviorDeathAnimation()
        {
            // Variables.
            float kingSlimeCenterX = NPC.Infernum().ExtraAI[8];
            float kingSlimeCenterY = NPC.Infernum().ExtraAI[9];
            ref float localDeathTimer = ref NPC.Infernum().ExtraAI[10];
            ref float tearProjectileIndex = ref NPC.Infernum().ExtraAI[11];
            float chargeSpeed = 25f;
            float extraEndSlashDelay = 20f;
            Vector2 kingSlimeCenter = new(kingSlimeCenterX, kingSlimeCenterY);
            Vector2 teleportOffset = new(375f, -100f);

            // Calclate the length of the Vector, using pythagarus theorum.
            float teleportOffsetDifference = teleportOffset.Length();
            
            // Then the length in time it will take to move two of them at the charge speed.
            int dashLength = (int)(teleportOffsetDifference / chargeSpeed * 2);
            
            // Teleport if king slime has set a landing position.
            if (kingSlimeCenterX > 0 && localDeathTimer == 0)
            {
                // Ensures that the teleport only happens once by saving the current timer time.
                localDeathTimer = SyncedDeathTimer;
                
                // Teleport to the initial position.
                NPC.Center = kingSlimeCenter + teleportOffset;

                // Reset a bunch of variables and become invisible.
                NPC.velocity = Vector2.Zero;
                NPC.Opacity = 0;
                NPC.rotation = 0;
                NPC.noGravity = true;
                NPC.spriteDirection = -1;

                // Create disappearance dust.
                for (int i = 0; i < 6; i++)
                {
                    Dust ninjaDodgeDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 31, 0f, 0f, 100, default, 2f);
                    ninjaDodgeDust.position += Main.rand.NextVector2Square(-20f, 20f);
                    ninjaDodgeDust.velocity *= 0.4f;
                    ninjaDodgeDust.scale *= Main.rand.NextFloat(1f, 1.4f);
                    if (Main.rand.NextBool(2))
                    {
                        ninjaDodgeDust.scale *= Main.rand.NextFloat(1f, 1.4f);
                        ninjaDodgeDust.noGravity = true;
                    }
                }
            }
            
            else if (localDeathTimer > 0)
            {
                // Move through tiles after the teleport.
                NPC.noTileCollide = true;
                
                if (SyncedDeathTimer == localDeathTimer + 1f)
                {
                    // Create reappearance dust.
                    for (int i = 0; i < 6; i++)
                    {
                        Dust ninjaDodgeDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 31, 0f, 0f, 100, default, 2f);
                        ninjaDodgeDust.position += Main.rand.NextVector2Square(-20f, 20f);
                        ninjaDodgeDust.velocity *= 0.4f;
                        ninjaDodgeDust.scale *= Main.rand.NextFloat(1f, 1.4f);
                        if (Main.rand.NextBool(2))
                        {
                            ninjaDodgeDust.scale *= Main.rand.NextFloat(1f, 1.4f);
                            ninjaDodgeDust.noGravity = true;
                        }
                    }
                    
                    // Re-appear.
                    NPC.Opacity = 1f;

                    // Play a sound, and begin moving through king slime.
                    SoundEngine.PlaySound(InfernumSoundRegistry.VassalSlashSound, NPC.Center);
                    NPC.velocity = NPC.SafeDirectionTo(kingSlimeCenter) * chargeSpeed;
                    NPC.netUpdate = true;

                    // Create the slash.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        tearProjectileIndex = Utilities.NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<DeathSlash>(), 200, 0f);
                }

                // After the slash can concluded.
                if (SyncedDeathTimer > localDeathTimer + dashLength)
                {
                    // If the Ninja should vanish, this extraEndSlashDelay is the amount of time it takes for the slash to catch up to it.
                    if (SyncedDeathTimer > localDeathTimer + dashLength + extraEndSlashDelay)
                    {
                        // Reset the tear projectile index.
                        tearProjectileIndex = -1f;

                        // Delete the tear projectile.
                        Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<DeathSlash>());
                    }
                    
                    // Freeze in place.
                    NPC.velocity = Vector2.Zero;

                    // If fadeout effects have not finished, spawn dust. this is to prevent looping.
                    if (NPC.Opacity > 0)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            Dust ninjaDodgeDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 31, 0f, 0f, 100, default, 2f);
                            ninjaDodgeDust.position += Main.rand.NextVector2Square(-20f, 20f);
                            ninjaDodgeDust.velocity *= 0.4f;
                            ninjaDodgeDust.scale *= Main.rand.NextFloat(1f, 1.4f);
                            if (Main.rand.NextBool(2))
                            {
                                ninjaDodgeDust.scale *= Main.rand.NextFloat(1f, 1.4f);
                                ninjaDodgeDust.noGravity = true;
                            }
                        }
                    }

                    // Disappear.
                    NPC.Opacity = 0f;
                }
            }

            // Otherwise, go invisible and await being told where to teleport to.
            else
            {           
                // Reset the rotation such that it points forward, and disable natural gravity.
                NPC.rotation = 0f;
                NPC.noGravity = true;

                // If fadeout effects have not finished, spawn dust. this is to prevent looping.
                if (NPC.Opacity > 0f)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Dust ninjaDodgeDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 31, 0f, 0f, 100, default, 2f);
                        ninjaDodgeDust.position += Main.rand.NextVector2Square(-20f, 20f);
                        ninjaDodgeDust.velocity *= 0.4f;
                        ninjaDodgeDust.scale *= Main.rand.NextFloat(1f, 1.4f);
                        if (Main.rand.NextBool(2))
                        {
                            ninjaDodgeDust.scale *= Main.rand.NextFloat(1f, 1.4f);
                            ninjaDodgeDust.noGravity = true;
                        }
                    }
                }
                
                // Disappear.
                NPC.Opacity = 0f;
            }
        }

        public void DoJump(float jumpSpeed, Vector2? destination = null)
        {
            if (destination is null)
                destination = Target.Center;

            float gravity = 0.3f;
            NPC.velocity = Utilities.GetProjectilePhysicsFiringVelocity(NPC.Center, destination.Value, gravity, jumpSpeed, out _);
            ShurikenShootCountdown = 24f;

            // Use the Time of Flight formula to determine how long the jump will last.
            TimeOfFlightCountdown = (int)Math.Ceiling(Math.Abs(NPC.velocity.Y * 2f / gravity));
            NPC.spriteDirection = (Target.Center.X - NPC.Center.X > 0f).ToDirectionInt();

            NPC.netUpdate = true;
        }

        public void DoRunEffects()
        {
            if (TeleportCountdown > 0)
                return;

            int idealDirection = (Target.Center.X - NPC.Center.X > 0f).ToDirectionInt();
            float runAcceleration = 0.11f;
            float maxRunSpeed = 4.5f;

            // Accelerate much faster if decelerating to make the effect more smooth.
            if (idealDirection != Math.Sign(NPC.velocity.X))
                runAcceleration *= 4f;

            // Run towards the target.
            if (MathHelper.Distance(NPC.Center.X, Target.Center.X) > 40f)
                NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + idealDirection * runAcceleration, -maxRunSpeed, maxRunSpeed);
            else
                NPC.velocity *= 1.02f;

            bool onSolidGround = WorldGen.SolidTile(Framing.GetTileSafely(NPC.Bottom + Vector2.UnitY * 16f));
            Tile tileAheadAboveTarget = Framing.GetTileSafely(NPC.Bottom + new Vector2(NPC.spriteDirection * 16f, -16f));
            Tile tileAheadBelowTarget = Framing.GetTileSafely(NPC.Bottom + new Vector2(NPC.spriteDirection * 16f, 16f));

            // Jump if there's an impending obstacle.
            if (onSolidGround && tileAheadAboveTarget.HasTile && Main.tileSolid[tileAheadAboveTarget.TileType])
            {
                DoJump(10f);
                NPC.netUpdate = true;
            }

            // If the next tile below the ninja's feet is inactive or actuated, jump.
            if (onSolidGround && !tileAheadBelowTarget.HasTile && Main.tileSolid[tileAheadBelowTarget.TileType])
            {
                DoJump(11.5f);
                NPC.netUpdate = true;
            }

            // Jump if is stuck somewhat on the X axis.
            if (onSolidGround && MathHelper.Distance(NPC.position.X, NPC.oldPosition.X) < 2f)
            {
                DoJump(15f);
                NPC.netUpdate = true;
            }
            else
                StuckTimer = 0f;
        }

        public void DoTeleportEffects()
        {
            // Do the teleport dash.
            if (TeleportCountdown > 35f)
            {
                NPC.velocity.X = MathHelper.SmoothStep(0f, NPC.spriteDirection * 6f, Utils.GetLerpValue(35f, 70f, TeleportCountdown, true));
                NPC.Opacity = Utils.GetLerpValue(35f, 45f, TeleportCountdown, true);
            }

            // Decide where to teleport to.
            else if (TeleportCountdown == 35f)
            {
                Vector2 teleportPoint = Vector2.Zero;

                Vector2 top = Target.Center - Vector2.UnitY * 100f;
                if (top.Y < 100f)
                    top.Y = 100f;

                CurrentTeleportDirection *= -1f;
                NPC.spriteDirection = (int)CurrentTeleportDirection;

                int downwardMove = 0;
                while (true)
                {
                    downwardMove++;
                    if (WorldGen.SolidTile((int)top.X / 16, (int)top.Y / 16))
                        break;
                    if (Framing.GetTileSafely((int)top.X / 16, (int)top.Y / 16).HasTile && Main.tileSolidTop[Framing.GetTileSafely((int)top.X / 16, (int)top.Y / 16).TileType])
                        break;

                    top.Y += 16f;
                    downwardMove++;
                    if (downwardMove > 600)
                        break;
                }

                Vector2 groundedTargetPosition = top - Vector2.UnitY * 8f;

                for (int tries = 0; tries < 10000; tries++)
                {
                    Vector2 potentialSpawnPoint = groundedTargetPosition + new Vector2(Main.rand.NextFloat(-500f - tries * 0.06f, 500f + tries * 0.06f), Main.rand.NextFloat(-30f, 500f + tries * 0.03f));
                    Vector2 potentialEndPoint = potentialSpawnPoint + Vector2.UnitX * NPC.spriteDirection * 150f;

                    // Ignore a position is too close to the target.
                    if (Target.WithinRange(potentialSpawnPoint, 270f) || Target.WithinRange(potentialEndPoint, 270f))
                        continue;

                    // If it's close to the original position.
                    if (NPC.WithinRange(potentialSpawnPoint, 200f) || !Target.WithinRange(potentialSpawnPoint, 900f))
                        continue;

                    if (!Collision.CanHit(potentialSpawnPoint, 1, 1, Target.position, Target.width, Target.height))
                        continue;

                    // If the area would result in the ninja being stuck.
                    if (Collision.SolidCollision(potentialSpawnPoint - Vector2.One * 38f, 50, 50))
                        continue;

                    // If the side is incorrect.
                    if (Math.Sign(potentialSpawnPoint.X - NPC.Center.X) != NPC.spriteDirection)
                        continue;

                    // Or if there's no ground near the position.
                    Point teleportPointTileBottom = potentialSpawnPoint.ToTileCoordinates();
                    bool activeSolidTop = Main.tileSolidTop[Framing.GetTileSafely(teleportPointTileBottom.X, teleportPointTileBottom.Y).TileType] && Framing.GetTileSafely(teleportPointTileBottom.X, teleportPointTileBottom.Y).HasTile;
                    if (!WorldGen.SolidTile(teleportPointTileBottom.X, teleportPointTileBottom.Y + 1) && !activeSolidTop)
                        continue;

                    teleportPoint = potentialSpawnPoint.ToTileCoordinates().ToWorldCoordinates(8f, -20f);
                    break;
                }

                if (teleportPoint != Vector2.Zero)
                    NPC.Center = teleportPoint;
                NPC.netUpdate = true;
            }
            else
            {
                NPC.velocity.X = MathHelper.SmoothStep(0f, NPC.spriteDirection * 6f, Utils.GetLerpValue(0f, 35f, TeleportCountdown, true));
                NPC.Opacity = Utils.GetLerpValue(35f, 25f, TeleportCountdown, true);
            }

            // Spawn no dust if fading out a good amount.
            if (NPC.Opacity < 0.5f)
                return;

            // Release ninja dodge dust.
            if (TeleportCountdown % 3f == 2f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Dust ninjaDodgeDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 31, 0f, 0f, 100, default, 2f);
                    ninjaDodgeDust.position += Main.rand.NextVector2Square(-20f, 20f);
                    ninjaDodgeDust.velocity *= 0.4f;
                    ninjaDodgeDust.scale *= Main.rand.NextFloat(1f, 1.4f);
                    if (Main.rand.NextBool(2))
                    {
                        ninjaDodgeDust.scale *= Main.rand.NextFloat(1f, 1.4f);
                        ninjaDodgeDust.noGravity = true;
                    }
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Texture2D outlineTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/KingSlime/NinjaOutline").Value;
            Vector2 outlineDrawPosition = NPC.Center - Main.screenPosition - Vector2.UnitY * 6f;
            SpriteEffects direction = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            if (KatanaUseTimer > 0f)
            {
                Texture2D katanaTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/KingSlime/Katana").Value;
                Vector2 drawPosition = NPC.Center - Main.screenPosition - Vector2.UnitY.RotatedBy(NPC.rotation) * 5f;
                drawPosition -= NPC.rotation.ToRotationVector2() * NPC.spriteDirection * 22f;
                float rotation = MathHelper.PiOver4 + NPC.rotation;
                SpriteEffects katanaDirection = direction | SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == 1)
                {
                    katanaDirection |= SpriteEffects.FlipHorizontally;
                    rotation -= MathHelper.PiOver2;
                }
                else
                    rotation += MathHelper.PiOver2;
                Main.spriteBatch.Draw(katanaTexture, drawPosition, null, NPC.GetAlpha(drawColor), rotation, katanaTexture.Size() * 0.5f, 1f, katanaDirection, 0f);
            }
            Main.spriteBatch.Draw(outlineTexture, outlineDrawPosition, NPC.frame, Color.White * NPC.Opacity * 0.6f, NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale * 1.05f, direction, 0f);
            Main.spriteBatch.Draw(texture, outlineDrawPosition, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, direction, 0f);
            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            frameHeight = 48;
            if (TimeOfFlightCountdown > 0f || !NPC.collideY)
            {
                if (KatanaUseTimer > 0f)
                    NPC.frame.Y = frameHeight * 3;
                else
                    NPC.frame.Y = frameHeight * 8;
                return;
            }

            if (TeleportCountdown > 0f)
            {
                NPC.frame.Y = frameHeight * 3;
                return;
            }

            NPC.frameCounter++;
            if (NPC.frameCounter % 3f == 2f && NPC.collideY)
                NPC.frame.Y += frameHeight;

            if (NPC.frame.Y >= frameHeight * 8)
                NPC.frame.Y = 0;
        }

        public override bool CheckActive() => false;
    }
}
