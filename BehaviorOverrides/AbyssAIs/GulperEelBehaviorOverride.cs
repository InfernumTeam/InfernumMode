using CalamityMod;
using CalamityMod.NPCs.Abyss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.AbyssAIs
{
    public class GulperEelBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<GulperEelHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public const int SegmentCount = 20;
        
        public override bool PreAI(NPC npc)
        {
            // Pick a target if a valid one isn't already decided.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Disable water distortion effects messing up the eel drawcode.
            npc.wet = false;

            float snapFieldOfView = 0.46f;
            float snapSpeed = 24.5f;
            bool swallowingPlayer = target.Infernum().EelSwallowIndex == npc.whoAmI;
            bool passiveMovement = swallowingPlayer;
            bool canSnapAtPlayer = npc.velocity.Length() > 7.5f && npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < snapFieldOfView && npc.WithinRange(target.Center, 450f);
            if (!Collision.CanHitLine(npc.TopLeft, npc.width, npc.height, target.TopLeft, target.width, target.height) || swallowingPlayer)
                canSnapAtPlayer = false;

            ref float hasSpawnedSegments = ref npc.localAI[0];
            ref float jawRotation = ref npc.localAI[1];
            ref float slitherTimer = ref npc.Infernum().ExtraAI[0];
            ref float snapAnticipation = ref npc.Infernum().ExtraAI[1];

            if (hasSpawnedSegments == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnSegments(npc);
                hasSpawnedSegments = 1f;
            }

            // Move towards the target.
            Vector2 left = npc.velocity.RotatedBy(-0.29f) * 0.75f;
            Vector2 right = npc.velocity.RotatedBy(0.29f) * 0.75f;
            float distanceToCollision = CalamityUtils.DistanceToTileCollisionHit(npc.Center, npc.velocity) ?? 500f;
            float distanceToCollisionLeft = CalamityUtils.DistanceToTileCollisionHit(npc.Center, left) ?? 500f;
            float distanceToCollisionRight = CalamityUtils.DistanceToTileCollisionHit(npc.Center, right) ?? 500f;

            // Avoid crashing into walls if possible.
            if (distanceToCollision < 10f && !Collision.SolidCollision(npc.TopLeft, npc.width, npc.height) && npc.velocity.Length() > 2.6f)
            {
                if (distanceToCollisionLeft >= 5f && distanceToCollisionLeft > distanceToCollisionRight)
                    npc.velocity = left;
                else if (distanceToCollisionRight >= 5f && distanceToCollisionRight > distanceToCollisionLeft)
                    npc.velocity = right;
            }
            else if (passiveMovement)
            {
                if (npc.velocity.Length() > 6f)
                    npc.velocity *= 0.99f;
                npc.velocity = npc.velocity.RotatedBy(0.01f);
            }
            else if (!npc.WithinRange(target.Center, 200f))
                npc.velocity = (npc.velocity * 109f + npc.SafeDirectionTo(target.Center) * 19f) / 110f;

            // Try to snap at and swallow the player.
            snapAnticipation = MathHelper.Clamp(snapAnticipation + canSnapAtPlayer.ToDirectionInt(), 0f, 30f);
            if (snapAnticipation >= 30f)
            {
                SoundEngine.PlaySound(SoundID.Item96, npc.Center);

                snapAnticipation = 0f;
                npc.velocity = npc.SafeDirectionTo(target.Center) * snapSpeed;
                npc.netUpdate = true;
            }

            // Swallow the player if they're on top of the hitbox of the eel's head and snapping.
            if (npc.velocity.Length() > 14.5f && npc.Hitbox.Intersects(target.Hitbox) && jawRotation > 0.1f)
                target.Infernum().EelSwallowIndex = npc.whoAmI;

            // Let the player go if hit while they're being swallowed.
            if (npc.justHit && swallowingPlayer)
            {
                target.Infernum().EelSwallowIndex = -1;
                target.velocity = npc.velocity.SafeNormalize(Vector2.Zero) * 16f;
                jawRotation = 0.8f;
            }

            // Decide the current jaw rotation.
            jawRotation = MathHelper.Clamp(jawRotation + (canSnapAtPlayer ? 0.05f : -0.099f), 0f, 1.33f);

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Draw behind tiles.
            npc.behindTiles = true;

            // Slither around.
            slitherTimer += MathHelper.Pi / 75f;

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
            
            Texture2D headTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/GulperEelHead").Value;
            Texture2D mouthTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/GulperEelMouth").Value;
            Texture2D tailTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/GulperEelTail").Value;
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

            GameShaders.Misc["CalamityMod:PrimitiveTexture"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/GulperEelBody"));
            GameShaders.Misc["CalamityMod:PrimitiveTexture"].Shader.Parameters["uPrimitiveSize"].SetValue(primitiveArea);
            GameShaders.Misc["CalamityMod:PrimitiveTexture"].Shader.Parameters["flipVertically"].SetValue(npc.velocity.X > 0f);

            // Draw the head.
            float jawRotation = npc.localAI[1];
            SpriteEffects direction = npc.velocity.X > 0f ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 mouthDrawOffset = new Vector2(40f, (npc.velocity.X > 0f).ToDirectionInt() * 14f).RotatedBy(npc.rotation + (npc.velocity.X > 0f).ToDirectionInt() * jawRotation - MathHelper.PiOver2);
            Main.EntitySpriteDraw(mouthTexture, headDrawPosition + mouthDrawOffset, null, npc.GetAlpha(Color.Gray), npc.rotation + (npc.velocity.X > 0f).ToDirectionInt() * jawRotation, mouthTexture.Size() * new Vector2(0.5f, 0f), npc.scale, direction, 0);
            Main.EntitySpriteDraw(headTexture, headDrawPosition, npc.frame, npc.GetAlpha(Color.Gray), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0);

            if (segmentPositions.Length >= 2)
            {
                npc.Infernum().OptionalPrimitiveDrawer.Draw(segmentPositions, Vector2.Zero, 36);
                float tailRotation = (segmentPositions[^2] - segmentPositions[^1]).ToRotation();
                Vector2 tailDrawPosition = segmentPositions[^1] - (segmentPositions[^2] - segmentPositions[^1]).SafeNormalize(Vector2.Zero) * 24f;
                SpriteEffects tailDirection = npc.velocity.X > 0f ? SpriteEffects.None : SpriteEffects.FlipVertically;
                Main.EntitySpriteDraw(tailTexture, tailDrawPosition, null, npc.GetAlpha(Color.Gray), tailRotation, tailTexture.Size() * new Vector2(0f, 0.5f), npc.scale * 1.16f, tailDirection, 0);
            }
            return false;
        }
    }
}
