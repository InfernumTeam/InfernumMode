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
using Terraria.Audio;
using Terraria.GameContent;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena
{
    public class Exowl : ModNPC
    {
        public int NPCToAttachTo = -1;

        public bool UseConfusionEffect = false;

        public bool IsIllusion = false;

        public float CircleRadius;

        public float CircleOffsetAngle;

        public Vector2 CircleCenter;

        public PrimitiveTrail FlameTrail = null;

        public PrimitiveTrail LightningDrawer = null;

        public PrimitiveTrail LightningBackgroundDrawer = null;

        public NPC Athena => Main.npc[GlobalNPCOverrides.Athena];

        public Player Target => Main.player[Athena.target];

        public ref float AttackTimer => ref Athena.ai[1];

        public ref float AttackState => ref NPC.ai[0];

        public ref float IndividualAttackTimer => ref NPC.ai[1];

        public ref float MinionRedCrystalGlow => ref Athena.localAI[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("XM-04 Exowl");
            Main.npcFrameCount[NPC.type] = 3;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = AIType = -1;
            NPC.damage = 5;
            NPC.width = 44;
            NPC.height = 46;
            NPC.defense = 40;
            NPC.lifeMax = 20000;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.canGhostHeal = false;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.netAlways = true;
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
            NPC.damage = 0;
            NPC.timeLeft = 3600;
            if (!Main.npc.IndexInRange(GlobalNPCOverrides.Athena))
            {
                NPC.active = false;
                return;
            }

            // Circle in place if variables for such behavior are used.
            if (CircleCenter != Vector2.Zero)
            {
                NPC.Center = CircleCenter + CircleOffsetAngle.ToRotationVector2() * CircleRadius;
                NPC.rotation = NPC.AngleTo(CircleCenter) + MathHelper.PiOver2;
                CircleOffsetAngle += MathHelper.ToRadians(0.67f);
            }

            // Charge in an attempt to confuse the target if variables for such behavior are used.
            if (UseConfusionEffect)
                DoBehavior_ConfusionCharges();
        }

        public void DoBehavior_ConfusionCharges()
        {
            // Use contact damage as necessary.
            if (!IsIllusion)
                NPC.damage = 500;
            else
                NPC.dontTakeDamage = true;

            switch ((int)AttackState)
            {
                // Rise upward.
                case 0:
                    float horizontalOffset = MathHelper.Lerp(350f, 560f, NPC.whoAmI % 7f / 7f);
                    Vector2 flyDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * horizontalOffset, -240f);
                    Vector2 idealVelocity = NPC.SafeDirectionTo(flyDestination) * 30f;
                    NPC.velocity = (NPC.velocity * 29f + idealVelocity) / 29f;
                    NPC.velocity = NPC.velocity.MoveTowards(idealVelocity, 1.5f);

                    if (NPC.WithinRange(flyDestination, 40f) || IndividualAttackTimer > 150f)
                    {
                        AttackState = 1f;
                        NPC.velocity *= 0.65f;
                        NPC.netUpdate = true;
                    }
                    break;

                // Slow down and look at the target.
                case 1:
                    NPC.spriteDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();
                    NPC.velocity *= 0.96f;
                    NPC.velocity = NPC.velocity.MoveTowards(Vector2.Zero, 0.7f);

                    // Charge once sufficiently slowed down.
                    float chargeSpeed = 40f;
                    if (NPC.velocity.Length() < 1.25f)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, NPC.Center);
                        SoundEngine.PlaySound(SoundID.Zombie, NPC.Center, 68);
                        AttackState = 2f;
                        IndividualAttackTimer = 0f;
                        NPC.velocity = NPC.SafeDirectionTo(Target.Center) * chargeSpeed;
                        NPC.netUpdate = true;
                    }
                    break;

                // Charge and swoop.
                case 2:
                    float angularTurnSpeed = MathHelper.Pi / 300f;
                    idealVelocity = NPC.SafeDirectionTo(Target.Center);
                    Vector2 leftVelocity = NPC.velocity.RotatedBy(-angularTurnSpeed);
                    Vector2 rightVelocity = NPC.velocity.RotatedBy(angularTurnSpeed);
                    if (leftVelocity.AngleBetween(idealVelocity) < rightVelocity.AngleBetween(idealVelocity))
                        NPC.velocity = leftVelocity;
                    else
                        NPC.velocity = rightVelocity;

                    if (IndividualAttackTimer > 25f)
                    {
                        AttackState = 0f;
                        IndividualAttackTimer = 0f;
                        NPC.velocity = Vector2.Lerp(NPC.velocity, -Vector2.UnitY * 12.5f, 0.14f);
                        NPC.netUpdate = true;
                    }
                    break;
            }
            IndividualAttackTimer++;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            NPC.frame.Y = (int)(NPC.frameCounter / 7 % Main.npcFrameCount[NPC.type]) * frameHeight;
        }

        public override bool CheckActive() => false;

        public float FlameTrailPulse => (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f + NPC.whoAmI * 111.5856f) * 0.5f + 0.5f;

        public float FlameTrailWidthFunction(float completionRatio)
        {
            float maxWidth = MathHelper.Lerp(12f, 18f, FlameTrailPulse);
            return MathHelper.SmoothStep(maxWidth, 6f, completionRatio);
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true);
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
            return MathHelper.Lerp(0.5f, 1.3f, (float)Math.Sin(MathHelper.Pi * completionRatio)) * NPC.scale;
        }

        public Color LightningColorFunction(float completionRatio)
        {
            float fadeToWhite = MathHelper.Lerp(0f, 0.65f, (float)Math.Sin(MathHelper.TwoPi * completionRatio + Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            Color baseColor = Color.Lerp(Color.Cyan, Color.White, fadeToWhite);
            Color color = Color.Lerp(baseColor, Color.Cyan, ((float)Math.Sin(MathHelper.Pi * completionRatio + Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f) * 0.8f) * 0.65f;
            color.A = 40;
            if (NPC.Opacity <= 0f)
                return Color.Transparent;
            return color;
        }

        public float LightningBackgroundWidthFunction(float completionRatio) => LightningWidthFunction(completionRatio) * 4f;

        public Color LightningBackgroundColorFunction(float _)
        {
            Color backgroundColor = Color.CornflowerBlue;
            Color color = backgroundColor * NPC.Opacity * 0.4f;
            return color;
        }

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/Exowl_Glowmask").Value;
            Vector2 origin = NPC.frame.Size() * 0.5f;

            void drawInstance(Vector2 drawPosition, bool drawThrusters, Color? colorOverride = null)
            {
                // Declare the primitive drawers if they have yet to be defined.
                if (FlameTrail is null)
                    FlameTrail = new PrimitiveTrail(FlameTrailWidthFunction, FlameTrailColorFunction, null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);
                if (LightningDrawer is null)
                    LightningDrawer = new PrimitiveTrail(LightningWidthFunction, LightningColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);
                if (LightningBackgroundDrawer is null)
                    LightningBackgroundDrawer = new PrimitiveTrail(LightningBackgroundWidthFunction, LightningBackgroundColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);

                Main.spriteBatch.EnterShaderRegion();

                // Draw electricity between NPCs.
                if (NPCToAttachTo >= 0 && Main.npc[NPCToAttachTo].active)
                {
                    NPC npcToAttachTo = Main.npc[NPCToAttachTo];
                    Vector2 end = npcToAttachTo.Center + npcToAttachTo.rotation.ToRotationVector2() * 30f;
                    List<Vector2> arm2ElectricArcPoints = AresTeslaOrb.DetermineElectricArcPoints(NPC.Center, end, 250290787);
                    LightningBackgroundDrawer.Draw(arm2ElectricArcPoints, -Main.screenPosition, 40);
                    LightningDrawer.Draw(arm2ElectricArcPoints, -Main.screenPosition, 40);
                }

                // Draw thrusters as necessary.
                if (drawThrusters)
                {
                    // Prepare the flame trail shader with its map texture.
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ScarletDevilStreak").Value);

                    // Draw a flame trail on the thrusters.
                    for (int direction = -1; direction <= 1; direction += 2)
                    {
                        Vector2 baseDrawOffset = new Vector2(0f, -10f).RotatedBy(NPC.rotation);
                        baseDrawOffset += new Vector2(direction * 18f, 0f).RotatedBy(NPC.rotation);

                        float backFlameLength = 70f;
                        Vector2 drawStart = NPC.Center + baseDrawOffset;
                        Vector2 drawEnd = drawStart - (NPC.rotation - MathHelper.PiOver2 - MathHelper.PiOver4 * direction).ToRotationVector2() * backFlameLength;
                        Vector2[] drawPositions = new Vector2[]
                        {
                            drawStart,
                            drawEnd
                        };

                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 drawOffset = (MathHelper.TwoPi * i / 3f).ToRotationVector2() * 2f;
                            FlameTrail.Draw(drawPositions, drawOffset - Main.screenPosition, 33);
                        }
                    }
                }
                Main.spriteBatch.ExitShaderRegion();

                // Draw the glowmask and regular texture.
                // This is influenced by the crystal glow at the end.
                Color glowmaskColor = Color.Lerp(Color.White, new Color(1f, 0f, 0f, 0.3f), MinionRedCrystalGlow);
                Main.spriteBatch.Draw(texture, drawPosition, NPC.frame, NPC.GetAlpha(colorOverride ?? drawColor), NPC.rotation, origin, NPC.scale, 0, 0f);

                for (int i = 0; i < 2; i++)
                    Main.spriteBatch.Draw(glowmask, drawPosition, NPC.frame, NPC.GetAlpha(colorOverride ?? glowmaskColor), NPC.rotation, origin, NPC.scale, 0, 0f);
                if (MinionRedCrystalGlow > 0f)
                {
                    float backimageOpacity = MathHelper.Lerp(0f, 0.1f, MinionRedCrystalGlow);
                    Main.spriteBatch.Draw(glowmask, drawPosition, NPC.frame, NPC.GetAlpha(colorOverride ?? Color.White) * backimageOpacity, NPC.rotation, origin, NPC.scale, 0, 0f);
                }
            }

            Color? color = null;
            if (UseConfusionEffect)
                color = Color.Lerp(Color.White, new Color(89, 207, 218, 51), IsIllusion ? 1f : 0.51f);

            if (UseConfusionEffect)
            {
                float illusionFadeInterpolant = Athena.Infernum().ExtraAI[3];
                float illusionOffsetFactor = MathHelper.Lerp(0f, 75f, illusionFadeInterpolant);

                for (int i = 0; i < 8; i++)
                {
                    Color hologramColor = Main.hslToRgb(i / 7f, 1f, 0.5f);
                    hologramColor.A = 51;
                    float drawOffsetFactor = 20f;
                    Vector3 offsetInformation = Vector3.Transform(Vector3.Forward,
                        Matrix.CreateRotationX((Main.GlobalTimeWrappedHourly - 0.3f + i * 0.1f) * 0.7f * MathHelper.TwoPi) *
                        Matrix.CreateRotationY((Main.GlobalTimeWrappedHourly - 0.8f + i * 0.3f) * 0.7f * MathHelper.TwoPi) *
                        Matrix.CreateRotationZ((Main.GlobalTimeWrappedHourly + 0.1f + i * 0.5f) * 0.1f * MathHelper.TwoPi));
                    drawOffsetFactor += Utils.GetLerpValue(-1f, 1f, offsetInformation.Z, true) * 12f;
                    Vector2 drawOffset = new Vector2(offsetInformation.X, offsetInformation.Y) * drawOffsetFactor;
                    if (!IsIllusion)
                        drawOffset *= 0.4f;

                    drawInstance(NPC.Center - Main.screenPosition + drawOffset, false, Color.Lerp(color.Value, hologramColor, 0.36f) * 0.27f);
                }
            }
            drawInstance(NPC.Center - Main.screenPosition, true, color);

            return false;
        }
    }
}
