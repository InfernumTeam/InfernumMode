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
using InfernumMode.Particles;
using CalamityMod.Particles;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena
{
    public class Exowl : ModNPC
    {
        public int NPCToAttachTo = -1;

        public int ExplodeCountdown;

        public bool UseConfusionEffect = false;

        public bool IsIllusion = false;

        public float CircleRadius;

        public float CircleOffsetAngle;

        public Vector2 CircleCenter;

        public PrimitiveTrail FlameTrail = null;

        public PrimitiveTrail LightningDrawer = null;

        public PrimitiveTrail LightningBackgroundDrawer = null;

        public static NPC Athena => Main.npc[GlobalNPCOverrides.Athena];

        public ref float AttackState => ref npc.ai[0];

        public ref float IndividualAttackTimer => ref npc.ai[1];

        public static Player Target => Main.player[Athena.target];

        public static ref float AttackTimer => ref Athena.ai[1];

        public static ref float MinionRedCrystalGlow => ref Athena.localAI[1];

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
            writer.Write(ExplodeCountdown);
            writer.Write(CircleRadius);
            writer.Write(CircleOffsetAngle);
            writer.WriteVector2(CircleCenter);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ExplodeCountdown = reader.ReadInt32();
            CircleRadius = reader.ReadSingle();
            CircleOffsetAngle = reader.ReadSingle();
            CircleCenter = reader.ReadVector2();
        }

        public static void MakeAllExowlsExplode()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type == ModContent.NPCType<Exowl>())
                {
                    Main.npc[i].ModNPC<Exowl>().ExplodeCountdown = 60;
                    Main.npc[i].netUpdate = true;
                }
            }
        }

        public override void AI()
        {
            npc.damage = 0;
            npc.timeLeft = 3600;

            if (ExplodeCountdown > 0)
            {
                npc.velocity *= 0.9f;
                npc.Center += Main.rand.NextVector2Circular(5f, 5f);
                ExplodeCountdown--;

                if (ExplodeCountdown <= 0)
                {
                    Main.PlaySound(SoundID.DD2_KoboldExplosion, npc.Center);
                    ElectricExplosionRing explosion = new ElectricExplosionRing(npc.Center, Vector2.Zero, CalamityUtils.ExoPalette, 1.4f, 95, 0.8f);
                    GeneralParticleHandler.SpawnParticle(explosion);
                    npc.active = false;
                }
                return;
            }

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

            // Charge in an attempt to confuse the target if variables for such behavior are used.
            if (UseConfusionEffect)
                DoBehavior_ConfusionCharges();
        }

        public void DoBehavior_ConfusionCharges()
        {
            // Use contact damage as necessary.
            if (!IsIllusion)
                npc.damage = 500;
            else
                npc.dontTakeDamage = true;

            switch ((int)AttackState)
            {
                // Rise upward.
                case 0:
                    float horizontalOffset = MathHelper.Lerp(350f, 560f, npc.whoAmI % 7f / 7f);
                    Vector2 flyDestination = Target.Center + new Vector2((Target.Center.X < npc.Center.X).ToDirectionInt() * horizontalOffset, -240f);
                    Vector2 idealVelocity = npc.SafeDirectionTo(flyDestination) * 30f;
                    npc.velocity = (npc.velocity * 29f + idealVelocity) / 29f;
                    npc.velocity = npc.velocity.MoveTowards(idealVelocity, 1.5f);

                    if (npc.WithinRange(flyDestination, 40f) || IndividualAttackTimer > 150f)
                    {
                        AttackState = 1f;
                        npc.velocity *= 0.65f;
                        npc.netUpdate = true;
                    }
                    break;

                // Slow down and look at the target.
                case 1:
                    npc.spriteDirection = (Target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.velocity *= 0.96f;
                    npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.7f);

                    // Charge once sufficiently slowed down.
                    float chargeSpeed = 40f;
                    if (npc.velocity.Length() < 1.25f)
                    {
                        Main.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                        Main.PlaySound(SoundID.Zombie, npc.Center, 68);
                        AttackState = 2f;
                        IndividualAttackTimer = 0f;
                        npc.velocity = npc.SafeDirectionTo(Target.Center) * chargeSpeed;
                        npc.netUpdate = true;
                    }
                    break;

                // Charge and swoop.
                case 2:
                    float angularTurnSpeed = MathHelper.Pi / 300f;
                    idealVelocity = npc.SafeDirectionTo(Target.Center);
                    Vector2 leftVelocity = npc.velocity.RotatedBy(-angularTurnSpeed);
                    Vector2 rightVelocity = npc.velocity.RotatedBy(angularTurnSpeed);
                    if (leftVelocity.AngleBetween(idealVelocity) < rightVelocity.AngleBetween(idealVelocity))
                        npc.velocity = leftVelocity;
                    else
                        npc.velocity = rightVelocity;

                    if (IndividualAttackTimer > 25f)
                    {
                        AttackState = 0f;
                        IndividualAttackTimer = 0f;
                        npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 12.5f, 0.14f);
                        npc.netUpdate = true;
                    }
                    break;
            }
            IndividualAttackTimer++;
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
            Vector2 origin = npc.frame.Size() * 0.5f;

            void drawInstance(Vector2 drawPosition, bool drawThrusters, Color? colorOverride = null)
            {
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
                    LightningBackgroundDrawer.Draw(arm2ElectricArcPoints, -Main.screenPosition, 30);
                    LightningDrawer.Draw(arm2ElectricArcPoints, -Main.screenPosition, 30);
                }

                // Draw thrusters as necessary.
                if (drawThrusters)
                {
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

                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 drawOffset = (MathHelper.TwoPi * i / 3f).ToRotationVector2() * 2f;
                            FlameTrail.Draw(drawPositions, drawOffset - Main.screenPosition, 28);
                        }
                    }
                }

                // Draw the glowmask and regular texture.
                // This is influenced by the crystal glow at the end.
                Color glowmaskColor = Color.Lerp(Color.White, new Color(1f, 0f, 0f, 0.3f), MinionRedCrystalGlow);
                Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(colorOverride ?? drawColor), npc.rotation, origin, npc.scale, 0, 0f);

                for (int i = 0; i < 2; i++)
                    Main.spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(colorOverride ?? glowmaskColor), npc.rotation, origin, npc.scale, 0, 0f);
                if (MinionRedCrystalGlow > 0f)
                {
                    float backimageOpacity = MathHelper.Lerp(0f, 0.1f, MinionRedCrystalGlow);
                    Main.spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(colorOverride ?? Color.White) * backimageOpacity, npc.rotation, origin, npc.scale, 0, 0f);
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
                        Matrix.CreateRotationX((Main.GlobalTime - 0.3f + i * 0.1f) * 0.7f * MathHelper.TwoPi) *
                        Matrix.CreateRotationY((Main.GlobalTime - 0.8f + i * 0.3f) * 0.7f * MathHelper.TwoPi) *
                        Matrix.CreateRotationZ((Main.GlobalTime + 0.1f + i * 0.5f) * 0.1f * MathHelper.TwoPi));
                    drawOffsetFactor += Utils.InverseLerp(-1f, 1f, offsetInformation.Z, true) * 12f;
                    Vector2 drawOffset = new Vector2(offsetInformation.X, offsetInformation.Y) * drawOffsetFactor;
                    if (!IsIllusion)
                        drawOffset *= 0.4f;

                    drawInstance(npc.Center - Main.screenPosition + drawOffset, false, Color.Lerp(color.Value, hologramColor, 0.36f) * 0.27f);
                }
            }
            drawInstance(npc.Center - Main.screenPosition, true, color);

            return false;
        }
    }
}
