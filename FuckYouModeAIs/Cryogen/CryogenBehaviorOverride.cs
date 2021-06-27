using CalamityMod.NPCs.Cryogen;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using CryogenBoss = CalamityMod.NPCs.Cryogen.Cryogen;

namespace InfernumMode.FuckYouModeAIs.Cryogen
{
    public class CryogenBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CryogenBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

		#region Enumerations
        internal enum CryogenAttackState
        {
            IcicleCircleBurst,
            PredictiveIcicles,
            Charge,
            TeleportAndReleaseIceBombs,
            SummonAuroraSpirits
		}
        #endregion

        #region AI

        public override bool PreAI(NPC npc)

        {
            npc.TargetClosest();

            Player target = Main.player[npc.target];
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float subphaseState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackState = ref npc.ai[2];

            if (lifeRatio < 0.9f && subphaseState == 0f)
            {
                EmitIceParticles(npc.Center, 3.5f, 40);
                for (int i = 1; i <= 5; i++)
                    Gore.NewGore(npc.Center, npc.velocity, InfernumMode.Instance.GetGoreSlot("Gores/CryogenChainGore" + i), npc.scale);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    attackState = 0f;
                    attackTimer = 0f;
                    subphaseState = 1f;
                    npc.netUpdate = true;
                }
            }

            if (lifeRatio < 0.7f && subphaseState == 1f)
            {
                EmitIceParticles(npc.Center, 3.5f, 50);
                for (int i = 1; i <= 7; i++)
                    Gore.NewGore(npc.Center, npc.velocity, InfernumMode.Instance.GetGoreSlot("Gores/CryogenGore" + i), npc.scale);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    attackState = 0f;
                    attackTimer = 0f;
                    subphaseState = 2f;
                    npc.netUpdate = true;
                }
            }

            npc.damage = npc.defDamage;
            if (subphaseState == 0f)
                DoWeakSubphase1Behavior(npc, target, ref attackTimer);
            else if (subphaseState == 1f)
                DoSubphase2Behavior(npc, target, ref attackTimer, ref attackState);
            else if (subphaseState == 2f)
                DoSubphase3Behavior(npc, target, ref attackTimer, ref attackState);

            if (npc.damage == 0)
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.55f, 0.1f);
            else
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 1f, 0.1f);
            Lighting.AddLight(npc.Center, Color.White.ToVector3());
            return false;
        }

        public static void DoWeakSubphase1Behavior(NPC npc, Player target, ref float attackTimer)
        {
            Vector2 destination = target.Center - Vector2.UnitY * 375f;

            if (!npc.WithinRange(destination, 90f))
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 11f, 0.8f);

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 55f == 54f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 30f).RotatedBy(MathHelper.Lerp(-0.6f, 0.6f, (i + 0.5f) / 5f)) * 9f;
                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<IceBlast>(), 120, 0f);
                }
            }

            npc.rotation = npc.velocity.X * 0.04f;
            attackTimer++;
        }

        public static void DoSubphase2Behavior(NPC npc, Player target, ref float attackTimer, ref float attackState)
        {
            CryogenAttackState[] attackCycle = new CryogenAttackState[]
            {
                CryogenAttackState.IcicleCircleBurst,
                CryogenAttackState.PredictiveIcicles,
                CryogenAttackState.Charge,
            };

            switch (attackCycle[(int)attackState % attackCycle.Length])
            {
                case CryogenAttackState.IcicleCircleBurst:
                    DoAttack_IcicleCircleBurst(npc, target, ref attackTimer, ref attackState, 1f);
                    break;
                case CryogenAttackState.PredictiveIcicles:
                    DoAttack_PredictiveIcicles(npc, target, ref attackTimer, ref attackState, 1f);
                    break;
                case CryogenAttackState.Charge:
                    DoAttack_Charge(npc, target, ref attackTimer, ref attackState, 1f);
                    break;
            }
            attackTimer++;
        }

        public static void DoSubphase3Behavior(NPC npc, Player target, ref float attackTimer, ref float attackState)
        {
            CryogenAttackState[] attackCycle = new CryogenAttackState[]
            {
                CryogenAttackState.IcicleCircleBurst,
                CryogenAttackState.TeleportAndReleaseIceBombs,
                CryogenAttackState.SummonAuroraSpirits,
                CryogenAttackState.IcicleCircleBurst,
                CryogenAttackState.PredictiveIcicles,
                CryogenAttackState.Charge,
                CryogenAttackState.TeleportAndReleaseIceBombs,
                CryogenAttackState.SummonAuroraSpirits,
                CryogenAttackState.PredictiveIcicles,
                CryogenAttackState.Charge,
            };

            switch (attackCycle[(int)attackState % attackCycle.Length])
            {
                case CryogenAttackState.IcicleCircleBurst:
                    DoAttack_IcicleCircleBurst(npc, target, ref attackTimer, ref attackState, 1.35f);
                    break;
                case CryogenAttackState.PredictiveIcicles:
                    DoAttack_PredictiveIcicles(npc, target, ref attackTimer, ref attackState, 1.35f);
                    break;
                case CryogenAttackState.Charge:
                    DoAttack_Charge(npc, target, ref attackTimer, ref attackState, 1.35f);
                    break;
                case CryogenAttackState.TeleportAndReleaseIceBombs:
                    DoAttack_TeleportAndReleaseIceBombs(npc, target, ref attackTimer, ref attackState, 1f);
                    break;
                case CryogenAttackState.SummonAuroraSpirits:
                    DoAttack_SummonAuroraSpirits(npc, target, ref attackTimer, ref attackState, 3);
                    break;
            }
            attackTimer++;
        }

        public static void DoAttack_IcicleCircleBurst(NPC npc, Player target, ref float attackTimer, ref float attackState, float attackPower)
        {
            float zeroBasedAttackPower = attackPower - 1f;
            int burstCount = 4;
            int burstCreationRate = 120 - (int)(zeroBasedAttackPower * 25f);
            int icicleCount = 12 + (int)(zeroBasedAttackPower * 5f);
            Vector2 destination = target.Center + new Vector2(target.velocity.X * 80f, -455f);
            if (!npc.WithinRange(destination, 90f))
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 15f, 1.4f);
            else
                npc.velocity *= 0.96f;
            npc.rotation = npc.velocity.X * 0.02f;
            npc.damage = 0;

            if (attackTimer % burstCreationRate == burstCreationRate - 1f)
            {
                EmitIceParticles(npc.Center, 3.5f, 25);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float angleOffset = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < icicleCount; i++)
                    {
                        int icicle = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<IcicleSpike>(), 125, 0f);
                        if (Main.projectile.IndexInRange(icicleCount))
                        {
                            Main.projectile[icicle].ai[0] = MathHelper.TwoPi * i / icicleCount + npc.AngleTo(target.Center) + angleOffset;
                            Main.projectile[icicle].ai[1] = npc.whoAmI;
                            Main.projectile[icicle].localAI[1] = 1f;
                        }

                        icicle = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<IcicleSpike>(), 120, 0f);
                        if (Main.projectile.IndexInRange(icicleCount))
                        {
                            Main.projectile[icicle].ai[0] = MathHelper.TwoPi * (i + 0.5f) / icicleCount + npc.AngleTo(target.Center) + angleOffset;
                            Main.projectile[icicle].ai[1] = npc.whoAmI;
                            Main.projectile[icicle].localAI[1] = 0.66f;
                        }
                    }
                }
            }

            if (attackTimer >= burstCreationRate * burstCount + 60f)
            {
                attackTimer = 0f;
                attackState++;
                npc.netUpdate = true;
            }
        }

        public static void DoAttack_PredictiveIcicles(NPC npc, Player target, ref float attackTimer, ref float attackState, float attackPower)
        {
            float zeroBasedAttackPower = attackPower - 1f;
            int burstCount = 6;
            int burstCreationRate = 120 - (int)(zeroBasedAttackPower * 12f);
            int icicleCount = 3 + (int)(zeroBasedAttackPower * 2f);
            Vector2 destination = target.Center - Vector2.UnitY * 395f;
            if (!npc.WithinRange(destination, 60f))
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 15f, 1.4f);
            else
                npc.velocity *= 0.945f;
            npc.rotation = npc.velocity.X * 0.02f;

            if (attackTimer % burstCreationRate == burstCreationRate - 1f)
            {
                EmitIceParticles(npc.Center, 3.5f, 25);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < icicleCount; i++)
                    {
                        Vector2 icicleVelocity = -Vector2.UnitY.RotatedByRandom(0.58f) * Main.rand.NextFloat(7f, 11f);
                        int icicle = Utilities.NewProjectileBetter(npc.Center, icicleVelocity, ModContent.ProjectileType<AimedIcicleSpike>(), 125, 0f);
                        if (Main.projectile.IndexInRange(icicleCount))
                            Main.projectile[icicle].ai[1] = i / (float)icicleCount * 68f;
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 icicleVelocity = (MathHelper.TwoPi * i / 4f + MathHelper.PiOver4).ToRotationVector2() * 6f;
                        Utilities.NewProjectileBetter(npc.Center, icicleVelocity, ModContent.ProjectileType<AimedIcicleSpike>(), 125, 0f);
                    }
                }
            }


            if (attackTimer >= burstCreationRate * burstCount + 60f)
            {
                attackTimer = 0f;
                attackState++;
                npc.netUpdate = true;
            }
        }

        public static void DoAttack_Charge(NPC npc, Player target, ref float attackTimer, ref float attackState, float attackPower)
        {
            float zeroBasedAttackPower = attackPower - 1f;
            int chargeCount = 6;
            int chargeHoverDelay = 45 - (int)(zeroBasedAttackPower * 15f);
            int chargeTime = 80 - (int)(zeroBasedAttackPower * 20f);
            float chargeSpeed = 17.5f + zeroBasedAttackPower * 4f;
            int attackCycleLength = chargeHoverDelay + chargeTime;
            if (attackTimer % attackCycleLength < chargeHoverDelay - 30f)
            {
                Vector2 destination = target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 200f, -295f);
                npc.Center = npc.Center.MoveTowards(destination, 6f);
            }

            if (attackTimer % attackCycleLength >= chargeHoverDelay - 30f && attackTimer % attackCycleLength < chargeHoverDelay)
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 7f, 0.05f);

            if (attackTimer % attackCycleLength == chargeHoverDelay)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 15f) * chargeSpeed;
                npc.netUpdate = true;
            }

            if (attackTimer % attackCycleLength > chargeHoverDelay)
            {
                if (attackTimer % attackCycleLength > chargeHoverDelay + chargeTime - 35)
                {
                    npc.velocity.X *= 0.98f;
                    npc.velocity.Y -= 0.7f;
                    npc.rotation = npc.rotation.AngleTowards(0f, 0.11f);
                }
                else
                    npc.rotation += npc.velocity.X * 0.03f;
            }

            if (attackTimer >= attackCycleLength * chargeCount)
            {
                attackTimer = 0f;
                attackState++;
                npc.netUpdate = true;
            }
        }

        public static void DoAttack_TeleportAndReleaseIceBombs(NPC npc, Player target, ref float attackTimer, ref float attackState, float attackPower)
        {
            float zeroBasedAttackPower = attackPower - 1f;
            int idleBombReleaseRate = 60 - (int)(zeroBasedAttackPower * 25f);
            int teleportWaitTime = 240 - (int)(zeroBasedAttackPower * 60f);
            int teleportTelegraphTime = teleportWaitTime - 90;

            ref float teleportPositionX = ref npc.Infernum().ExtraAI[0];
            ref float teleportPositionY = ref npc.Infernum().ExtraAI[1];
            
            // Idly release bombs.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % idleBombReleaseRate == idleBombReleaseRate - 1f)
            {
                Vector2 bombVelocity = npc.SafeDirectionTo(target.Center) * 12f;
                Utilities.NewProjectileBetter(npc.Center, bombVelocity, ModContent.ProjectileType<IceBomb2>(), 125, 0f);
            }

            // Decide a teleport postion and emit teleport particles there.
            if (attackTimer >= teleportTelegraphTime && attackTimer < teleportWaitTime)
            {
                if (teleportPositionX == 0f || teleportPositionY == 0f)
                {
                    Vector2 teleportPosition = target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(360f, 435f);
                    teleportPositionX = teleportPosition.X;
                    teleportPositionY = teleportPosition.Y;
                }
                else
                    EmitIceParticles(new Vector2(teleportPositionX, teleportPositionY), 3f, 6);

                npc.Opacity = Utils.InverseLerp(teleportWaitTime - 1f, teleportWaitTime - 45f, attackTimer, true);
            }

            // Do the teleport.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == teleportWaitTime)
            {
                npc.Center = new Vector2(teleportPositionX, teleportPositionY);
                npc.velocity = -Vector2.UnitY.RotateTowards(npc.AngleTo(target.Center), MathHelper.Pi / 3f) * 7f;

                for (int i = 0; i < 6; i++)
                {
                    Vector2 bombVelocity = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 11f;
                    Utilities.NewProjectileBetter(npc.Center, bombVelocity, ModContent.ProjectileType<IceBomb2>(), 125, 0f);
                }

                teleportPositionX = 0f;
                teleportPositionY = 0f;
                npc.netUpdate = true;
            }

            if (!npc.WithinRange(target.Center, 75f))
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 5.5f, 0.075f);

            npc.rotation = npc.velocity.X * 0.03f;

            if (attackTimer >= teleportWaitTime + 95f)
            {
                attackTimer = 0f;
                attackState++;
                npc.netUpdate = true;
            }
        }

        public static void DoAttack_SummonAuroraSpirits(NPC npc, Player target, ref float attackTimer, ref float attackState, int spiritCountOnEachSide)
        {
            Vector2 destination = target.Center - Vector2.UnitY * 300f;
            if (attackTimer < 80f)
                npc.Center = npc.Center.MoveTowards(destination, 9f);

            npc.rotation = npc.rotation.AngleTowards(0f, 0.04f);

            int spiritCount = NPC.CountNPCS(ModContent.NPCType<IceMass>());
            if (attackTimer == 80f)
            {
                for (int i = 0; i < spiritCountOnEachSide; i++)
                {
                    if (spiritCount > 8)
                        break;

                    Vector2 spawnPosition = target.Center + new Vector2(940f, MathHelper.Lerp(-450f, 450f, i / (float)spiritCountOnEachSide));
                    int spirit = NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<IceMass>());
                    if (Main.npc.IndexInRange(spirit))
                        Main.npc[spirit].velocity = -Vector2.UnitX * 8f;

                    spawnPosition = target.Center + new Vector2(-940f, MathHelper.Lerp(-450f, 450f, i / (float)spiritCountOnEachSide));
                    spirit = NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<IceMass>());
                    if (Main.npc.IndexInRange(spirit))
                        Main.npc[spirit].velocity = Vector2.UnitX * 8f;
                    spiritCount += 2;
                }
            }

            if (attackTimer >= 165f)
            {
                attackTimer = 0f;
                attackState++;
                npc.netUpdate = true;
            }
        }

        public static void EmitIceParticles(Vector2 position, float speed, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Dust ice = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(62f, 62f), 261);
                ice.color = Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat(0.15f, 0.7f));
                ice.velocity = Main.rand.NextVector2Circular(speed, speed) - Vector2.UnitY * 1.6f;
                ice.scale = Main.rand.NextFloat(1.2f, 1.6f);
                ice.fadeIn = 1.5f;
                ice.noGravity = true;
            }
        }

        #endregion AI

        #region Drawing
        internal static void SetupCustomBossIcon()
        {
            InfernumMode.Instance.AddBossHeadTexture("InfernumMode/FuckYouModeAIs/Cryogen/CryogenMapIcon", -1);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Main.projFrames[npc.type] = 12;

            Texture2D subphase1Texture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cryogen/Cryogen1");
            Texture2D subphase1TextureGlow = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cryogen/Cryogen1Glow");

            Texture2D subphase2Texture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cryogen/Cryogen2");
            Texture2D subphase2TextureGlow = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cryogen/Cryogen2Glow");

            Texture2D subphase3Texture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cryogen/Cryogen3");
            Texture2D subphase3TextureGlow = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cryogen/Cryogen3Glow");

            Texture2D subphase4Texture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cryogen/Cryogen4");
            Texture2D subphase4TextureGlow = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cryogen/Cryogen4Glow");

            Texture2D subphase5Texture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cryogen/Cryogen5");
            Texture2D subphase5TextureGlow = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cryogen/Cryogen5Glow");

            Texture2D subphase6Texture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cryogen/Cryogen6");
            Texture2D subphase6TextureGlow = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cryogen/Cryogen6Glow");

            Texture2D drawTexture = subphase1Texture;
            Texture2D glowTexture = subphase1TextureGlow;
            switch ((int)npc.ai[0])
            {
                case 0:
                    drawTexture = subphase1Texture;
                    glowTexture = subphase1TextureGlow;
                    break;
                case 1:
                    drawTexture = subphase2Texture;
                    glowTexture = subphase2TextureGlow;
                    break;
                case 2:
                    drawTexture = subphase3Texture;
                    glowTexture = subphase3TextureGlow;
                    break;
                case 3:
                    drawTexture = subphase4Texture;
                    glowTexture = subphase4TextureGlow;
                    break;
                case 4:
                    drawTexture = subphase5Texture;
                    glowTexture = subphase5TextureGlow;
                    break;
                case 5:
                    drawTexture = subphase6Texture;
                    glowTexture = subphase6TextureGlow;
                    break;
            }

            npc.frame.Width = drawTexture.Width;
            npc.frame.Height = drawTexture.Height / Main.projFrames[npc.type];
            npc.frameCounter++;
            if (npc.frameCounter >= 5)
            {
                npc.frame.Y += npc.frame.Height;
                if (npc.frame.Y >= Main.projFrames[npc.type] * npc.frame.Height)
                    npc.frame.Y = 0;

                npc.frameCounter = 0;
            }

            Vector2 drawPosition = npc.Center - Main.screenPosition;
            spriteBatch.Draw(drawTexture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowTexture, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            return false;
        }

        #endregion
    }
}
