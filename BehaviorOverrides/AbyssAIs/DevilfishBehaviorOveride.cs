using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Abyss;
using CalamityMod.Sounds;
using InfernumMode.OverridingSystem;
using InfernumMode.Sounds;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.AbyssAIs
{
    public class DevilfishBehaviorOveride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DevilFish>();

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            int enrageHitCount = 5;
            int slowdownTime = 40;
            int chargeTime = 54;

            // Ensure that the devilfish can target critters.
            npc.Infernum().IsAbyssPredator = true;
            NPCID.Sets.UsesNewTargetting[npc.type] = true;

            ref float hasNoticedPlayer = ref npc.Infernum().ExtraAI[0];
            ref float maskHits = ref npc.Infernum().ExtraAI[1];
            ref float movementState = ref npc.Infernum().ExtraAI[2];
            ref float attackTimer = ref npc.Infernum().ExtraAI[3];

            // Pick a target if a valid one isn't already decided.
            Utilities.TargetClosestAbyssPredator(npc, hasNoticedPlayer == 0f, 1600f, 1600f);
            NPCAimedTarget target = npc.GetTargetData();

            // Initialize the variant.
            if (npc.localAI[0] == 0f)
            {
                npc.localAI[0] = 1f;
                npc.localAI[1] = Main.rand.Next(2);
                npc.RemoveWaterSlowness();
                npc.netUpdate = true;
            }

            // Disable knockback and go through tiles.
            npc.knockBackResist = 0f;
            npc.noTileCollide = true;

            // Register hits.
            if (npc.justHit && maskHits < enrageHitCount)
            {
                maskHits++;

                // Break the mask and use a reasonable defense value once enough hits have been registered.
                if (maskHits >= enrageHitCount)
                {
                    npc.defense = 15;
                    npc.HitSound = SoundID.NPCHit1;
                    movementState = 0f;
                    attackTimer = 0f;

                    if (Main.netMode != NetmodeID.Server)
                    {
                        for (int i = 1; i <= 3; i++)
                            Gore.NewGore(npc.GetSource_FromAI(), npc.TopLeft, npc.velocity, InfernumMode.CalamityMod.Find<ModGore>($"DevilFishMask{i}").Type, 1f);
                    }
                    SoundEngine.PlaySound(DevilFish.MaskBreakSound, npc.position);

                    npc.ModNPC<DevilFish>().brokenMask = true;
                }

                hasNoticedPlayer = 1f;
                npc.netUpdate = true;
            }

            // Swim around slowly if no target was found.
            bool insideTiles = Collision.SolidCollision(npc.TopLeft, npc.width, npc.height);
            bool targetInLineOfSight = Collision.CanHitLine(npc.TopLeft, npc.width, npc.height, target.Position, target.Width, target.Height) || insideTiles;
            bool canAttackTarget = npc.WithinRange(target.Center, hasNoticedPlayer == 1f && target.Type == NPCTargetType.Player ? 900f : 480f) && targetInLineOfSight;
            if (!canAttackTarget)
            {
                if (target.Type != NPCTargetType.Player)
                    npc.target = 0;
                CalamityAI.PassiveSwimmingAI(npc, InfernumMode.Instance, 0, 0.01f, 0.15f, 0.15f, 4f, 4f, 0.1f);
                return false;
            }

            if (target.Type == NPCTargetType.Player)
                hasNoticedPlayer = 1f;

            // If a valid target was found, attempt to ram into it.
            // In doing so, the devilfish attempts to pick the point it's closest to and hover there before initiating the ram.
            Vector2 hoverOffset = new Vector2(420f, 300f) + (npc.whoAmI * 11f).ToRotationVector2() * 60f;
            Vector2 topLeft = target.Center - hoverOffset;
            Vector2 topRight = target.Center + hoverOffset * new Vector2(1f, -1f);
            Vector2 bottomLeft = target.Center + hoverOffset * new Vector2(-1f, 1f);
            Vector2 bottomRight = target.Center + hoverOffset;
            List<Vector2> validHoverSpots = new List<Vector2>()
            {
                topLeft,
                topRight,
                bottomLeft,
                bottomRight
            }.Where(p => !Collision.SolidCollision(p - Vector2.One * 80f, 160, 160)).OrderBy(p => p.DistanceSQ(npc.Center)).ToList();

            Vector2 hoverDestination = target.Center - Vector2.UnitY * 100f;
            if (validHoverSpots.Count >= 1)
                hoverDestination = validHoverSpots[0];

            bool kamikazeMode = maskHits >= enrageHitCount;
            float chargeSpeed = kamikazeMode ? 24f : 20f;
            switch ((int)movementState)
            {
                // Hover into position and look at the target.
                case 0:
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 16f, 0.12f);
                    npc.rotation = npc.AngleTo(target.Center);
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    if (npc.spriteDirection == -1)
                        npc.rotation += MathHelper.Pi;

                    if (npc.WithinRange(hoverDestination, 108f) && attackTimer >= 45f)
                    {
                        attackTimer = 0f;
                        movementState = 1f;
                        npc.velocity *= 0.3f;
                        npc.netUpdate = true;
                    }

                    break;

                // Slow down in anticipation of the charge.
                case 1:
                    if (attackTimer == 1f)
                        SoundEngine.PlaySound(InfernumSoundRegistry.DevilfishRoarSound with { Pitch = -0.1f, Volume = 0.5f }, npc.Center);

                    if (attackTimer < slowdownTime)
                    {
                        npc.velocity *= 0.96f;
                        npc.rotation = npc.AngleTo(target.Center);
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == -1)
                            npc.rotation += MathHelper.Pi;
                        break;
                    }

                    SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, npc.Center);

                    // Charge at the target.
                    attackTimer = 0f;
                    movementState = 2f;
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.netUpdate = true;

                    break;

                // Handle post-charge behaviors.
                case 2:
                    npc.rotation = npc.velocity.ToRotation();
                    if (npc.spriteDirection == -1)
                        npc.rotation += MathHelper.Pi;

                    // Explode if the mask is off.
                    if (kamikazeMode)
                        attackTimer = 0f;

                    if (kamikazeMode && insideTiles)
                    {
                        SoundEngine.PlaySound(CommonCalamitySounds.PlagueBoomSound, npc.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int explosionID = ModContent.ProjectileType<DevilfishExplosion>();
                            for (int i = 0; i < 100; i++)
                                Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2Circular(13f, 13f), explosionID, 250, 0f);
                        }

                        npc.life = 0;
                        npc.StrikeNPCNoInteraction(9999, 0f, 0);
                        npc.NPCLoot();
                    }
                    if (kamikazeMode && npc.velocity.Length() < 21f)
                        npc.velocity *= 1.03f;

                    if (attackTimer >= chargeTime && !kamikazeMode)
                    {
                        attackTimer = 0f;
                        movementState = 0f;
                        npc.velocity *= 0.5f;
                        npc.netUpdate = true;
                    }
                    break;
            }
            attackTimer++;

            return false;
        }
        #endregion AI and Behaviors

        #region Frames and Drawcode
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/Abyss/DevilFishGlow").Value;
            if (npc.localAI[1] == 1f)
            {
                texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Abyss/DevilFishAlt").Value;
                glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/Abyss/DevilFishGlowAlt").Value;
            }

            Vector2 drawPosition = npc.Center - Main.screenPosition;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
            return false;
        }
        #endregion Frames and Drawcode
    }
}
