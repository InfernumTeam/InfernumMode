using CalamityMod;
using CalamityMod.Projectiles.Boss;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena
{
    public class Exowl : ModNPC
    {
        public int NPCToAttachTo = -1;

        public Vector2 CircleCenter;

        public float CircleRadius;

        public float CircleOffsetAngle;

        public PrimitiveTrail FlameTrail = null;

        public PrimitiveTrail LightningDrawer = null;

        public PrimitiveTrail LightningBackgroundDrawer = null;

        public NPC Athena => Main.npc[GlobalNPCOverrides.Athena];

        public ref float AttackTimer => ref Athena.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("XM-04 Exowl");
            Main.npcFrameCount[npc.type] = 3;
        }

        public override void SetDefaults()
        {
            npc.aiStyle = aiType = -1;
            npc.damage = 5;
            npc.width = 44;
            npc.height = 46;
            npc.defense = 40;
            npc.lifeMax = 20000;
            npc.knockBackResist = 0f;
            npc.lavaImmune = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.canGhostHeal = false;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath14;
            npc.netAlways = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(CircleRadius);
            writer.Write(CircleOffsetAngle);
            writer.WriteVector2(CircleCenter);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            CircleRadius = reader.ReadSingle();
            CircleOffsetAngle = reader.ReadSingle();
            CircleCenter = reader.ReadVector2();
        }

        public override void AI()
        {
            npc.damage = 0;
            npc.timeLeft = 3600;
            if (!Main.npc.IndexInRange(GlobalNPCOverrides.Athena))
            {
                npc.active = false;
                return;
            }

            // Circle in place if variables for such behavior are used.
            if (CircleCenter != Vector2.Zero)
            {
                npc.Center = CircleCenter + CircleOffsetAngle.ToRotationVector2() * CircleRadius;
                npc.rotation = npc.AngleTo(CircleCenter) + MathHelper.PiOver2;
                CircleOffsetAngle += MathHelper.ToRadians(0.67f);
            }
        }

        public override void FindFrame(int frameHeight)
        {
            npc.frameCounter++;
            npc.frame.Y = (int)(npc.frameCounter / 7 % Main.npcFrameCount[npc.type]) * frameHeight;
        }

        public override bool CheckActive() => false;

        public float FlameTrailPulse => (float)Math.Sin(Main.GlobalTime * 6f + npc.whoAmI * 111.5856f) * 0.5f + 0.5f;

        public float FlameTrailWidthFunction(float completionRatio)
        {
            float maxWidth = MathHelper.Lerp(12f, 18f, FlameTrailPulse);
            return MathHelper.SmoothStep(maxWidth, 6f, completionRatio);
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.InverseLerp(0.8f, 0.27f, completionRatio, true) * Utils.InverseLerp(0f, 0.067f, completionRatio, true);
            trailOpacity *= MathHelper.Lerp(1f, 0.5f, 1f - FlameTrailPulse);
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.27f);
            Color middleColor = Color.Lerp(Color.Orange, Color.Blue, 0.74f);
            Color endColor = Color.DarkCyan;
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
            color.A /= 2;
            return color;
        }

        public float LightningWidthFunction(float completionRatio)
        {
            return MathHelper.Lerp(0.5f, 1.3f, (float)Math.Sin(MathHelper.Pi * completionRatio)) * npc.scale;
        }

        public Color LightningColorFunction(float completionRatio)
        {
            float fadeToWhite = MathHelper.Lerp(0f, 0.65f, (float)Math.Sin(MathHelper.TwoPi * completionRatio + Main.GlobalTime * 4f) * 0.5f + 0.5f);
            Color baseColor = Color.Lerp(Color.Cyan, Color.White, fadeToWhite);
            Color color = Color.Lerp(baseColor, Color.Cyan, ((float)Math.Sin(MathHelper.Pi * completionRatio + Main.GlobalTime * 4f) * 0.5f + 0.5f) * 0.8f) * 0.65f;
            color.A = 40;
            if (npc.Opacity <= 0f)
                return Color.Transparent;
            return color;
        }

        public float LightningBackgroundWidthFunction(float completionRatio) => LightningWidthFunction(completionRatio) * 4f;

        public Color LightningBackgroundColorFunction(float _)
        {
            Color backgroundColor = Color.CornflowerBlue;
            Color color = backgroundColor * npc.Opacity * 0.4f;
            return color;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            Texture2D glowmask = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/Exowl_Glowmask");
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = npc.frame.Size() * 0.5f;

            spriteBatch.EnterShaderRegion();

            // Declare the primitive drawers if they have yet to be defined.
            if (FlameTrail is null)
                FlameTrail = new PrimitiveTrail(FlameTrailWidthFunction, FlameTrailColorFunction, null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);
            if (LightningDrawer is null)
                LightningDrawer = new PrimitiveTrail(LightningWidthFunction, LightningColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);
            if (LightningBackgroundDrawer is null)
                LightningBackgroundDrawer = new PrimitiveTrail(LightningBackgroundWidthFunction, LightningBackgroundColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);

            // Draw electricity between NPCs.
            if (NPCToAttachTo >= 0 && Main.npc[NPCToAttachTo].active)
            {
                NPC npcToAttachTo = Main.npc[NPCToAttachTo];
                Vector2 end = npcToAttachTo.Center + npcToAttachTo.rotation.ToRotationVector2() * 30f;
                List<Vector2> arm2ElectricArcPoints = AresTeslaOrb.DetermineElectricArcPoints(npc.Center, end, 250290787);
                LightningBackgroundDrawer.Draw(arm2ElectricArcPoints, -Main.screenPosition, 40);
                LightningDrawer.Draw(arm2ElectricArcPoints, -Main.screenPosition, 40);
            }

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.GetTexture("CalamityMod/ExtraTextures/ScarletDevilStreak"));

            // Draw a flame trail on the thrusters.
            for (int direction = -1; direction <= 1; direction += 2)
            {
                Vector2 baseDrawOffset = new Vector2(0f, -10f).RotatedBy(npc.rotation);
                baseDrawOffset += new Vector2(direction * 18f, 0f).RotatedBy(npc.rotation);

                float backFlameLength = 70f;
                Vector2 drawStart = npc.Center + baseDrawOffset;
                Vector2 drawEnd = drawStart - (npc.rotation - MathHelper.PiOver2 - MathHelper.PiOver4 * direction).ToRotationVector2() * backFlameLength;
                Vector2[] drawPositions = new Vector2[]
                {
                    drawStart,
                    drawEnd
                };

                for (int i = 0; i < 6; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 2f;
                    FlameTrail.Draw(drawPositions, drawOffset - Main.screenPosition, 40);
                }
            }
            spriteBatch.ExitShaderRegion();

            spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(drawColor), npc.rotation, origin, npc.scale, 0, 0f);
            spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, 0, 0f);

            return false;
        }
    }
}
