using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Prime
{
    /*
    public class PrimeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.SkeletronPrime;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region Enumerations
        public enum PrimeAttackType
        {
            SpawnEffects,
            MetalBurst,
            RocketRelease
        }

        public enum PrimeFrameType
        {
            ClosedMouth,
            OpenMouth,
            Spikes
        }
        #endregion

        #region AI
        public static bool AnyArms => true || NPC.AnyNPCs(NPCID.PrimeCannon) || NPC.AnyNPCs(NPCID.PrimeLaser) || NPC.AnyNPCs(NPCID.PrimeVice) || NPC.AnyNPCs(NPCID.PrimeSaw);
        public override bool PreAI(NPC npc)
        {
            npc.frame = new Rectangle(0, 10000, 1, 1);
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float frameType = ref npc.localAI[0];

            npc.TargetClosest();

            Player target = Main.player[npc.target];

            switch ((PrimeAttackType)(int)attackType)
            {
                case PrimeAttackType.SpawnEffects:
                    DoAttack_SpawnEffects(npc, target, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.MetalBurst:
                    DoAttack_MetalBurst(npc, target, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.RocketRelease:
                    DoAttack_RocketRelease(npc, target, attackTimer, ref frameType);
                    break;
            }

            attackTimer++;
            return false;
        }

        #region Specific Attacks
        public static void DoAttack_SpawnEffects(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            bool canHover = attackTimer < 180f;

            if (canHover)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY.RotatedBy(MathHelper.TwoPi * 3f * attackTimer / 180f - MathHelper.PiOver2) * 625f;
                hoverDestination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 50f;

                npc.velocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), 33f);
                npc.rotation = npc.rotation.AngleLerp(npc.velocity.X * 0.04f, 0.1f);
                frameType = (int)PrimeFrameType.ClosedMouth;
            }
            else
            {
                if (attackTimer >= 195f)
                    frameType = (int)PrimeFrameType.OpenMouth;
                npc.velocity *= 0.85f;
                npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                if (attackTimer > 210f)
                {
                    Main.PlaySound(SoundID.Roar, target.Center, 0);
                    GotoNextAttackState(npc);
                }
            }
        }
        
        public static void DoAttack_MetalBurst(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            int shootRate = 45;
            int shootCount = 5;
            float wrappedTime = attackTimer % shootRate;

            Vector2 destination = target.Center - Vector2.UnitY * 380f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 36f, 0.4f);
            npc.rotation = npc.velocity.X * 0.04f;

            // Open the mouth a little bit before shooting.
            frameType = wrappedTime >= shootRate * 0.7f ? (int)PrimeFrameType.OpenMouth : (int)PrimeFrameType.ClosedMouth;

            if (Main.netMode != NetmodeID.MultiplayerClient && wrappedTime == shootRate - 1f)
            {
                for (int i = 0; i < 16; i++)
                {
                    Vector2 spikeVelocity = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * 5.5f;
                    Utilities.NewProjectileBetter(npc.Center + spikeVelocity * 12f, spikeVelocity, ModContent.ProjectileType<MetallicSpike>(), 115, 0f);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= shootRate * (shootCount + 0.65f))
                GotoNextAttackState(npc);
        }

        public static void DoAttack_RocketRelease(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            int cycleTime = 36;
            int rocketCountPerCycle = 7;
            int shootCycleCount = 4;
            float wrappedTime = attackTimer % cycleTime;

            npc.rotation = npc.velocity.X * 0.04f;

            frameType = (int)PrimeFrameType.ClosedMouth;
            if (wrappedTime > cycleTime - rocketCountPerCycle * 2f)
            {
                frameType = (int)PrimeFrameType.OpenMouth;
                npc.velocity *= 0.925f;

                if (Main.netMode != NetmodeID.MultiplayerClient && wrappedTime % 3f == 2f)
                {
                    float rocketSpeed = Main.rand.NextFloat(10.5f, 12f);
                    Vector2 rocketVelocity = Main.rand.NextVector2CircularEdge(rocketSpeed, rocketSpeed);
                    if (rocketVelocity.Y < -1f)
                        rocketVelocity.Y = -1f;
                    rocketVelocity = Vector2.Lerp(rocketVelocity, npc.SafeDirectionTo(target.Center).RotatedByRandom(0.4f) * rocketVelocity.Length(), 0.6f);
                    rocketVelocity = rocketVelocity.SafeNormalize(-Vector2.UnitY) * rocketSpeed;
                    Utilities.NewProjectileBetter(npc.Center + Vector2.UnitY * 33f, rocketVelocity, ProjectileID.SaucerMissile, 115, 0f);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= cycleTime * (shootCycleCount + 0.4f))
                GotoNextAttackState(npc);
        }
        #endregion Specific Attacks

        #region General Helper Functions
        public static void GotoNextAttackState(NPC npc)
        {
            PrimeAttackType currentAttack = (PrimeAttackType)(int)npc.ai[0];
            PrimeAttackType nextAttack = PrimeAttackType.MetalBurst;
            if (AnyArms)
                nextAttack = currentAttack == PrimeAttackType.MetalBurst ? PrimeAttackType.RocketRelease : PrimeAttackType.MetalBurst;

            npc.ai[0] = (int)nextAttack;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }
        #endregion General Helper Function

        #endregion AI

        #region Frames and Drawcode
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            Texture2D eyeGlowTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/PrimeEyes");
            Rectangle frame = texture.Frame(1, Main.npcFrameCount[npc.type], 0, (int)npc.localAI[0]);
            Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
            for (int i = 9; i >= 0; i -= 2)
            {
                Vector2 drawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                Color afterimageColor = npc.GetAlpha(lightColor);
                afterimageColor.R = (byte)(afterimageColor.R * (10 - i) / 20);
                afterimageColor.G = (byte)(afterimageColor.G * (10 - i) / 20);
                afterimageColor.B = (byte)(afterimageColor.B * (10 - i) / 20);
                afterimageColor.A = (byte)(afterimageColor.A * (10 - i) / 20);
                spriteBatch.Draw(Main.npcTexture[npc.type], drawPosition, frame, afterimageColor, npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(texture, baseDrawPosition, frame, npc.GetAlpha(lightColor), npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(eyeGlowTexture, baseDrawPosition, frame, new Color(200, 200, 200, 255), npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
        #endregion Frames and Drawcode
    }
    */
}