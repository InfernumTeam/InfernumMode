using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.NPCs.Abyss;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.DataStructures;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class GulperEelBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<GulperEelHead>();

        public const int SegmentCount = 20;

        public override void Load()
        {
            InfernumPlayer.PostUpdateEvent += (InfernumPlayer player) =>
            {
                Referenced<int> eelSwallowIndex = player.GetRefValue<int>("EelSwallowIndex");
                // Handle eel swallow behaviors.
                if (eelSwallowIndex.Value >= 0 && Main.npc[eelSwallowIndex.Value].active && Main.npc[eelSwallowIndex.Value].type == ModContent.NPCType<GulperEelHead>() && !Collision.SolidCollision(player.Player.TopLeft, player.Player.width, player.Player.height))
                {
                    // Be completely invisible when stuck, so as to give the illusion that they're inside of the eel.
                    player.Player.immuneAlpha = 260;

                    // Stick to the Gulper eel's mouth, changing the player's field of view.
                    player.Player.Center = Main.npc[eelSwallowIndex.Value].Center;
                    player.Player.velocity = Vector2.Zero;

                    player.Player.mount?.Dismount(player.Player);
                }

                // Reset the swallow index if it's no longer applicable.
                else if (eelSwallowIndex.Value != -1)
                    eelSwallowIndex.Value = -1;
            };
        }

        public override bool PreAI(NPC npc)
        {
            // Pick a target if a valid one isn't already decided.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Disable water distortion effects messing up the eel drawcode.
            npc.wet = false;

            ref float hasSpawnedSegments = ref npc.localAI[0];
            ref float jawRotation = ref npc.localAI[1];
            ref float slitherTimer = ref npc.Infernum().ExtraAI[0];
            ref float snapAnticipation = ref npc.Infernum().ExtraAI[1];
            ref float isHostile = ref npc.Infernum().ExtraAI[2];
            ref float hostileAimTimer = ref npc.Infernum().ExtraAI[3];
            ref float screamSlotID = ref npc.Infernum().ExtraAI[4];
            ref float turnCountdown = ref npc.Infernum().ExtraAI[5];

            Referenced<int> eelSwallowIndex = target.Infernum().GetRefValue<int>("EelSwallowIndex");

            int hostilityDelay = 167;
            float snapFieldOfView = 0.46f;
            float noticeFieldOfView = 0.73f;
            float snapSpeed = 24.5f;
            bool aboutToScream = false;
            bool canNoticePlayer = npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < noticeFieldOfView && npc.WithinRange(target.Center, 840f);
            bool swallowingPlayer = eelSwallowIndex.Value == npc.whoAmI;
            bool passiveMovement = swallowingPlayer || isHostile == 0f;
            bool canSnapAtPlayer = npc.velocity.Length() > 7.5f && npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < snapFieldOfView && npc.WithinRange(target.Center, 450f);
            if (!Collision.CanHitLine(npc.TopLeft, npc.width, npc.height, target.TopLeft, target.width, target.height) || swallowingPlayer)
            {
                canSnapAtPlayer = false;
                canNoticePlayer = false;
            }
            if (npc.justHit)
                canNoticePlayer = true;

            if (hasSpawnedSegments == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnSegments(npc);
                hasSpawnedSegments = 1f;
            }

            // Calculate raycast distances
            Vector2 left = npc.velocity.RotatedBy(-0.6f).ClampMagnitude(3f, 16f) * 0.8f;
            Vector2 right = npc.velocity.RotatedBy(0.6f).ClampMagnitude(3f, 16f) * 0.8f;
            float distanceToCollision = CalamityUtils.DistanceToTileCollisionHit(npc.Center, npc.velocity) ?? 500f;
            float distanceToCollisionLeft = CalamityUtils.DistanceToTileCollisionHit(npc.Center, left) ?? 500f;
            float distanceToCollisionRight = CalamityUtils.DistanceToTileCollisionHit(npc.Center, right) ?? 500f;

            if (hostileAimTimer <= 0f && canNoticePlayer)
            {
                hostileAimTimer = 1f;
                npc.netUpdate = true;
            }

            // Handle hostile animation triggers.
            if (hostileAimTimer >= 1f && hostileAimTimer < hostilityDelay)
            {
                // Jitter in a frenzy.
                npc.Center += Main.rand.NextVector2Circular(2f, 2f);

                aboutToScream = hostileAimTimer >= hostilityDelay - 150f;
                if (hostileAimTimer == hostilityDelay - 120f)
                    screamSlotID = SoundEngine.PlaySound(InfernumSoundRegistry.GulperEelScreamSound with { Volume = 1.5f }, target.Center).ToFloat();

                // Look at the target and very, very slowly approach them.
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 0.7f, 0.15f);

                hostileAimTimer++;
                if (hostileAimTimer >= hostilityDelay)
                {
                    isHostile = 1f;
                    SoundEngine.PlaySound(SoundID.Item96, npc.Center);
                    npc.velocity = npc.SafeDirectionTo(target.Center) * 18f;
                    npc.netUpdate = true;
                }
            }

            // Avoid crashing into walls if possible.
            else if ((distanceToCollision < 14f && !Collision.SolidCollision(npc.TopLeft, npc.width, npc.height)) || turnCountdown > 0f)
            {
                if (distanceToCollisionLeft >= 5f && distanceToCollisionLeft > distanceToCollisionRight)
                    npc.velocity = Vector2.Lerp(npc.velocity, left, 0.1f);
                else if (distanceToCollisionRight >= 5f && distanceToCollisionRight > distanceToCollisionLeft)
                    npc.velocity = Vector2.Lerp(npc.velocity, right, 0.1f);

                if (npc.velocity.Length() > 3f)
                    npc.velocity -= npc.velocity.SafeNormalize(Vector2.UnitY) * 0.9f;

                if (turnCountdown > 0f)
                    turnCountdown--;
                else
                    turnCountdown = 15f;
            }

            // Handle passive movement.
            else if (passiveMovement)
            {
                if (npc.velocity.Length() < 3f)
                    npc.velocity.Y -= 0.25f;
                if (npc.velocity.Length() > 6f)
                    npc.velocity *= 0.99f;
                npc.velocity = npc.velocity.RotatedBy(0.005f);

                // Randomly snap.
                if (Main.rand.NextBool(270))
                {
                    npc.velocity = npc.velocity.RotatedByRandom(0.97f) * 1.7f;
                    npc.netUpdate = true;
                }
            }

            // Handle attack movement.
            else if (!npc.WithinRange(target.Center, 200f))
                npc.velocity = (npc.velocity * 94f + npc.SafeDirectionTo(target.Center) * 19f) / 95f;

            // Avoid the world edges.
            if (npc.Center.X > Main.maxTilesX * 16f - 700f)
                npc.velocity.X -= 0.8f;
            if (npc.Center.X < 700f)
                npc.velocity.X += 0.8f;

            // Update the scream sound in terms of position.
            if (SoundEngine.TryGetActiveSound(SlotId.FromFloat(screamSlotID), out ActiveSound result))
                result.Position = target.Center;

            // Try to snap at and swallow the player.
            snapAnticipation = Clamp(snapAnticipation + canSnapAtPlayer.ToDirectionInt(), 0f, 30f);
            if (snapAnticipation >= 30f)
            {
                SoundEngine.PlaySound(SoundID.Item96, npc.Center);

                snapAnticipation = 0f;
                npc.velocity = npc.SafeDirectionTo(target.Center) * snapSpeed;
                npc.netUpdate = true;
            }

            // Swallow the player if they're on top of the hitbox of the eel's head and snapping.
            if (npc.velocity.Length() > 14.5f && npc.Hitbox.Intersects(target.Hitbox) && jawRotation > 0.1f && !target.immune)
                eelSwallowIndex.Value = npc.whoAmI;

            // Let the player go if hit while they're being swallowed.
            if (npc.justHit && swallowingPlayer)
            {
                eelSwallowIndex.Value = -1;
                target.velocity = npc.velocity.SafeNormalize(Vector2.Zero) * 16f;
                target.GiveIFrames(45, true);
                jawRotation = 0.8f;
            }

            // Decide the current jaw rotation.
            jawRotation = Clamp(jawRotation + (canSnapAtPlayer || aboutToScream ? 0.05f : -0.099f), 0f, 1.33f);

            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            // Draw behind tiles.
            npc.behindTiles = true;

            // Slither around.
            slitherTimer += Pi / 75f;

            return false;
        }

        public static void SpawnSegments(NPC npc)
        {
            int previousSegment = npc.whoAmI;
            for (int i = 0; i < SegmentCount; i++)
            {
                int nextSegmentIndex;
                if (i == 0)
                    nextSegmentIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<GulperEelBody>(), npc.whoAmI);
                else if (i < SegmentCount - 1)
                    nextSegmentIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<GulperEelBodyAlt>(), npc.whoAmI);
                else
                    nextSegmentIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<GulperEelTail>(), npc.whoAmI);
                Main.npc[nextSegmentIndex].realLife = npc.whoAmI;
                Main.npc[nextSegmentIndex].ai[2] = npc.whoAmI;
                Main.npc[nextSegmentIndex].ai[1] = previousSegment;
                Main.npc[nextSegmentIndex].Infernum().ExtraAI[0] = i + 1f;
                Main.npc[previousSegment].ai[0] = nextSegmentIndex;

                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextSegmentIndex, 0f, 0f, 0f, 0);

                previousSegment = nextSegmentIndex;
            }
        }

        public static float SegmentWidthFunction(NPC npc, float _) => npc.width * npc.scale * 0.5f;

        public static Vector2[] GetSegmentPositions(NPC npc)
        {
            List<Vector2> positions = new();

            // Loop to find all previous segments in the worm linked list until the tail is reached.
            int segmentIndex = (int)npc.ai[0];
            while (segmentIndex >= 0 && segmentIndex < Main.maxNPCs && Main.npc[segmentIndex].active && (Main.npc[segmentIndex].type == ModContent.NPCType<GulperEelBody>() || Main.npc[segmentIndex].type == ModContent.NPCType<GulperEelBodyAlt>()))
            {
                positions.Add(Main.npc[segmentIndex].Center);
                segmentIndex = (int)Main.npc[segmentIndex].ai[0];
            }

            return positions.ToArray();
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Why is this drawing the body with additive blending?
            Main.spriteBatch.ResetBlendState();

            Vector2 headDrawPosition = npc.Center - Main.screenPosition;

            // Initialize the segment drawer.
            npc.Infernum().OptionalPrimitiveDrawer ??= new(c => SegmentWidthFunction(npc, c), _ => Color.Gray * npc.Opacity, null, true, GameShaders.Misc["CalamityMod:PrimitiveTexture"]);

            Texture2D headTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/GulperEelHead").Value;
            Texture2D mouthTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/GulperEelMouth").Value;
            Texture2D tailTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/GulperEelTail").Value;
            Vector2[] segmentPositions = GetSegmentPositions(npc);

            Vector2 segmentAreaTopLeft = Vector2.One * 999999f;
            Vector2 segmentAreaTopRight = Vector2.Zero;

            for (int i = 0; i < segmentPositions.Length; i++)
            {
                segmentPositions[i] += -Main.screenPosition - npc.rotation.ToRotationVector2() * Math.Sign(npc.velocity.X) * 2f;
                if (segmentAreaTopLeft.X > segmentPositions[i].X)
                    segmentAreaTopLeft.X = segmentPositions[i].X;
                if (segmentAreaTopLeft.Y > segmentPositions[i].Y)
                    segmentAreaTopLeft.Y = segmentPositions[i].Y;

                if (segmentAreaTopRight.X < segmentPositions[i].X)
                    segmentAreaTopRight.X = segmentPositions[i].X;
                if (segmentAreaTopRight.Y < segmentPositions[i].Y)
                    segmentAreaTopRight.Y = segmentPositions[i].Y;
            }

            // Set shader parameters.
            float offsetAngle = (npc.position - npc.oldPos[1]).ToRotation();
            Vector2 primitiveArea = (segmentAreaTopRight - segmentAreaTopLeft).RotatedBy(-offsetAngle);
            while (Math.Abs(primitiveArea.X) < 180f)
                primitiveArea.X *= 1.5f;

            GameShaders.Misc["CalamityMod:PrimitiveTexture"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/GulperEelBody"));
            GameShaders.Misc["CalamityMod:PrimitiveTexture"].Shader.Parameters["uPrimitiveSize"].SetValue(primitiveArea);
            GameShaders.Misc["CalamityMod:PrimitiveTexture"].Shader.Parameters["flipVertically"].SetValue(npc.velocity.X > 0f);

            // Draw the head.
            float jawRotation = npc.localAI[1];
            SpriteEffects direction = npc.velocity.X > 0f ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 mouthDrawOffset = new Vector2(40f, (npc.velocity.X > 0f).ToDirectionInt() * 14f).RotatedBy(npc.rotation + (npc.velocity.X > 0f).ToDirectionInt() * jawRotation - PiOver2);
            Main.EntitySpriteDraw(mouthTexture, headDrawPosition + mouthDrawOffset, null, npc.GetAlpha(Color.Gray), npc.rotation + (npc.velocity.X > 0f).ToDirectionInt() * jawRotation, mouthTexture.Size() * new Vector2(0.5f, 0f), npc.scale, direction, 0);
            Main.EntitySpriteDraw(headTexture, headDrawPosition, npc.frame, npc.GetAlpha(Color.Gray), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0);

            if (segmentPositions.Length >= 2)
            {
                BezierCurve curve = new(segmentPositions);
                npc.Infernum().OptionalPrimitiveDrawer.Draw(segmentPositions, Vector2.Zero, 36);
                float tailRotation = (curve.Evaluate(0.98f) - curve.Evaluate(1f)).ToRotation();
                Vector2 tailDrawPosition = segmentPositions[^1] - (segmentPositions[^2] - segmentPositions[^1]).SafeNormalize(Vector2.Zero) * 20f;
                SpriteEffects tailDirection = npc.velocity.X > 0f ? SpriteEffects.None : SpriteEffects.FlipVertically;
                Main.EntitySpriteDraw(tailTexture, tailDrawPosition, null, npc.GetAlpha(Color.Gray), tailRotation, tailTexture.Size() * new Vector2(0f, 0.5f), npc.scale * 1.16f, tailDirection, 0);
            }
            return false;
        }
    }
}
