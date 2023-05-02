using CalamityMod;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class ColossalSquidTentacle : ModNPC
    {
        public bool RightSide
        {
            get => NPC.ai[0] == 1f;
            set => NPC.ai[0] = value.ToInt();
        }

        public NPC Owner => Main.npc.IndexInRange((int)NPC.ai[1]) && Main.npc[(int)NPC.ai[1]].active ? Main.npc[(int)NPC.ai[1]] : null;

        public PrimitiveTrailCopy TentacleDrawer = null;

        public Player Target => Main.player[NPC.target];

        public Vector2[] SegmentPositions
        {
            get
            {
                Vector2 stickPosition = Owner.Center + new Vector2(RightSide ? -50f : 30f, 52f).RotatedBy(Owner.rotation);
                Vector2 farLeft = Owner.Center - Vector2.UnitY * 500f;
                Vector2 farRight = NPC.Center + Owner.SafeDirectionTo(NPC.Center).RotatedBy(RightSide.ToDirectionInt() * -0.4f) * 450f;
                List<Vector2> segmentPositions = new();
                segmentPositions.Add(stickPosition);
                segmentPositions.Add(stickPosition - Vector2.UnitX * RightSide.ToDirectionInt() * 10f);
                for (int i = 0; i < 20; i++)
                {
                    float moveOffset = MathF.Sin(Owner.Infernum().ExtraAI[9] * 0.113f + i / 7f) * Utils.GetLerpValue(4f, 9f, i, true) * 30f;
                    if (Owner.Infernum().ExtraAI[9] <= 0f)
                        moveOffset = 0f;

                    Vector2 linearPosition = Vector2.CatmullRom(farLeft, stickPosition, NPC.Center, farRight, i / 19f);
                    Vector2 perpendicularOffset = (stickPosition - NPC.Center).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
                    segmentPositions.Add(linearPosition + perpendicularOffset * moveOffset);
                }

                for (int i = 0; i < 20; i++)
                    segmentPositions.Add(NPC.Center);
                return segmentPositions.ToArray();
            }
        }

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Tentacle");
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = AIType = -1;
            NPC.width = NPC.height = 1800;
            NPC.damage = 180;
            NPC.lifeMax = 5000;
            NPC.dontTakeDamage = true;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
        }

        public override void AI()
        {
            // Disappear if Infernum is not active.
            if (!InfernumMode.CanUseCustomAIs)
            {
                NPC.active = false;
                return;
            }

            if (Owner is null)
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }
            NPC.rotation = (SegmentPositions[^22] - SegmentPositions[^1]).ToRotation() - MathHelper.PiOver2;
            NPC.damage = Owner.dontTakeDamage ? 0 : NPC.defDamage;
        }

        public float SegmentWidthFunction(float _) => NPC.scale * 8f;

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            if (Owner is null)
                return false;

            foreach (Vector2 segmentPosition in SegmentPositions)
            {
                if (Utils.CenteredRectangle(segmentPosition, Vector2.One * 22f).Intersects(target.Hitbox))
                    return true;
            }

            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Owner is null)
                return false;

            // Bruh.
            spriteBatch.ResetBlendState();

            Vector2 headDrawPosition = NPC.Center - Main.screenPosition;
            headDrawPosition.Y += 3f;

            // Initialize the segment drawer.
            TentacleDrawer ??= new(c => SegmentWidthFunction(c), _ => Color.Gray * NPC.Opacity, null, true, GameShaders.Misc["CalamityMod:PrimitiveTexture"]);

            Texture2D headTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/ColossalSquidTentacleHead").Value;
            Vector2[] segmentPositions = SegmentPositions;

            Vector2 segmentAreaTopLeft = Vector2.One * 999999f;
            Vector2 segmentAreaTopRight = Vector2.Zero;

            for (int i = 0; i < segmentPositions.Length; i++)
            {
                segmentPositions[i] += -Main.screenPosition;
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

            GameShaders.Misc["CalamityMod:PrimitiveTexture"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/ColossalSquidTentacle"));
            GameShaders.Misc["CalamityMod:PrimitiveTexture"].Shader.Parameters["uPrimitiveSize"].SetValue(primitiveArea);
            GameShaders.Misc["CalamityMod:PrimitiveTexture"].Shader.Parameters["flipVertically"].SetValue(!RightSide);

            // Draw the end of the tentacle.
            float jawRotation = NPC.localAI[1];
            SpriteEffects direction = RightSide ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.EntitySpriteDraw(headTexture, headDrawPosition, null, Color.Gray * NPC.Opacity, NPC.rotation, headTexture.Size() * 0.5f, NPC.scale, direction, 0);

            if (segmentPositions.Length >= 2)
                TentacleDrawer.Draw(segmentPositions, Vector2.Zero, 36);

            return false;
        }

        public override bool CheckActive() => false;
    }
}
