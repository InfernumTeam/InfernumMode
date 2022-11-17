using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.NPCs;
using InfernumMode.InverseKinematics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.AbyssAIs
{
    public class ColossalSquidTentacle : ModNPC
    {
        public bool RightSide
        {
            get => NPC.ai[0] == 1f;
            set => NPC.ai[0] = value.ToInt();
        }

        public NPC Owner => Main.npc.IndexInRange((int)NPC.ai[1]) && Main.npc[(int)NPC.ai[1]].active ? Main.npc[(int)NPC.ai[1]] : null;

        public PrimitiveTrailCopy LimbDrawer = null;

        public Player Target => Main.player[NPC.target];

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Ghostly Leg");
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = AIType = -1;
            NPC.width = NPC.height = 1800;
            NPC.damage = 245;
            NPC.lifeMax = 5000;
            NPC.dontTakeDamage = true;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
            NPC.scale = 1.15f;
        }

        public override void AI()
        {
            if (Owner is null)
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }
        }

        public float SegmentWidthFunction(float _) => NPC.scale * 12f;

        public Vector2[] GetSegmentPositions()
        {
            Vector2 stickPosition = Owner.Center + new Vector2(RightSide.ToDirectionInt() * 60f);
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            if (Owner is null)
                return false;

            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Owner is null)
                return false;

            Vector2 headDrawPosition = NPC.Center - Main.screenPosition;

            // Initialize the segment drawer.
            NPC.Infernum().OptionalPrimitiveDrawer ??= new(c => SegmentWidthFunction(c), _ => Color.Gray * NPC.Opacity, null, true, GameShaders.Misc["CalamityMod:PrimitiveTexture"]);

            Texture2D headTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/GulperEelHead").Value;
            Texture2D mouthTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/GulperEelMouth").Value;
            Texture2D tailTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/GulperEelTail").Value;
            Vector2[] segmentPositions = GetSegmentPositions(NPC);

            Vector2 segmentAreaTopLeft = Vector2.One * 999999f;
            Vector2 segmentAreaTopRight = Vector2.Zero;

            for (int i = 0; i < segmentPositions.Length; i++)
            {
                segmentPositions[i] += -Main.screenPosition - NPC.rotation.ToRotationVector2() * Math.Sign(NPC.velocity.X) * 2f;
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
            float offsetAngle = (NPC.position - NPC.oldPos[1]).ToRotation();
            Vector2 primitiveArea = (segmentAreaTopRight - segmentAreaTopLeft).RotatedBy(-offsetAngle);
            while (Math.Abs(primitiveArea.X) < 180f)
                primitiveArea.X *= 1.5f;

            GameShaders.Misc["CalamityMod:PrimitiveTexture"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/GulperEelBody"));
            GameShaders.Misc["CalamityMod:PrimitiveTexture"].Shader.Parameters["uPrimitiveSize"].SetValue(primitiveArea);
            GameShaders.Misc["CalamityMod:PrimitiveTexture"].Shader.Parameters["flipVertically"].SetValue(NPC.velocity.X > 0f);

            // Draw the head.
            float jawRotation = NPC.localAI[1];
            SpriteEffects direction = NPC.velocity.X > 0f ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 mouthDrawOffset = new Vector2(40f, (NPC.velocity.X > 0f).ToDirectionInt() * 14f).RotatedBy(NPC.rotation + (NPC.velocity.X > 0f).ToDirectionInt() * jawRotation - MathHelper.PiOver2);
            Main.EntitySpriteDraw(mouthTexture, headDrawPosition + mouthDrawOffset, null, NPC.GetAlpha(Color.Gray), NPC.rotation + (NPC.velocity.X > 0f).ToDirectionInt() * jawRotation, mouthTexture.Size() * new Vector2(0.5f, 0f), NPC.scale, direction, 0);
            Main.EntitySpriteDraw(headTexture, headDrawPosition, NPC.frame, NPC.GetAlpha(Color.Gray), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, direction, 0);

            if (segmentPositions.Length >= 2)
            {
                BezierCurve curve = new(segmentPositions);
                NPC.Infernum().OptionalPrimitiveDrawer.Draw(segmentPositions, Vector2.Zero, 36);
                float tailRotation = (curve.Evaluate(0.98f) - curve.Evaluate(1f)).ToRotation();
                Vector2 tailDrawPosition = segmentPositions[^1] - (segmentPositions[^2] - segmentPositions[^1]).SafeNormalize(Vector2.Zero) * 20f;
                SpriteEffects tailDirection = NPC.velocity.X > 0f ? SpriteEffects.None : SpriteEffects.FlipVertically;
                Main.EntitySpriteDraw(tailTexture, tailDrawPosition, null, NPC.GetAlpha(Color.Gray), tailRotation, tailTexture.Size() * new Vector2(0f, 0.5f), NPC.scale * 1.16f, tailDirection, 0);
            }
            return false;
        }

        public override bool CheckActive() => false;
    }
}
