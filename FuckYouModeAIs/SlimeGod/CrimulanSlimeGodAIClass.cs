using CalamityMod.NPCs;
using CalamityMod.NPCs.SlimeGod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using CrimulanSGBig = CalamityMod.NPCs.SlimeGod.SlimeGodRun;
using CrimulanSGSmall = CalamityMod.NPCs.SlimeGod.SlimeGodRunSplit;
using EbonianSGBig = CalamityMod.NPCs.SlimeGod.SlimeGod;
using EbonianSGSmall = CalamityMod.NPCs.SlimeGod.SlimeGodSplit;

namespace InfernumMode.FuckYouModeAIs.SlimeGod
{
    public class CrimulanSlimeGodAIClass
    {
        #region Enumerations
        public enum CrimulanSlimeGodAttackType
        {
            IchorSlam = 0,
            ShortLeaps = 1,
            BigSlam = 2,
        }
        #endregion

        #region AI

        [OverrideAppliesTo("SlimeGodRun", typeof(CrimulanSlimeGodAIClass), "CrimulanSlimeGodAI", EntityOverrideContext.NPCAI)]
        public static bool CrimulanSlimeGodAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGod) || !Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodPurple))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            // This will affect the other gods as well in terms of behavior.
            ref float universalState = ref Main.npc[CalamityGlobalNPC.slimeGod].ai[0];
            ref float universalTimer = ref Main.npc[CalamityGlobalNPC.slimeGod].ai[1];

            // Set the universal whoAmI variable.
            CalamityGlobalNPC.slimeGodRed = npc.whoAmI;
            npc.realLife = CalamityGlobalNPC.slimeGodPurple;
            npc.life = Main.npc[npc.realLife].life;
            npc.lifeMax = Main.npc[npc.realLife].lifeMax;

            switch ((CrimulanSlimeGodAttackType)(int)universalState)
            {
                case CrimulanSlimeGodAttackType.IchorSlam:
                    DoAttack_IchorSlam(npc, target, ref universalTimer);
                    break;
                case CrimulanSlimeGodAttackType.ShortLeaps:
                    DoAttack_ShortLeaps(npc, target, ref universalTimer);
                    break;
                case CrimulanSlimeGodAttackType.BigSlam:
                    DoAttack_BigSlam(npc, target, ref universalTimer);
                    break;
            }

            return false;
        }

        public static void DoAttack_IchorSlam(NPC npc, Player target, ref float attackTimer)
        {
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            if (attackSubstate == 0f)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 350f;
                npc.velocity = (npc.velocity * 5f + npc.SafeDirectionTo(destination) * 11f) / 6f;
                npc.noGravity = true;

                if (npc.WithinRange(destination, 25f))
                    attackTimer = 100f;

                if (attackTimer >= 100f)
                {
                    npc.velocity = Vector2.UnitY * 7f;
                    attackSubstate = 1f;
                    npc.netUpdate = true;
                }
            }

            else
            {
                if (npc.velocity.Y == 0f && attackTimer < 225f)
                {
                    Main.PlaySound(SoundID.NPCHit1, npc.Center);
                    attackSubstate = 2f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Vector2 shootVelocity = Vector2.UnitX * i * 9f;
                            Utilities.NewProjectileBetter(npc.Bottom, shootVelocity, ModContent.ProjectileType<IchorGelWave>(), 95, 0f);
                            Utilities.NewProjectileBetter(npc.Bottom - Vector2.UnitY * 30f, shootVelocity, ModContent.ProjectileType<IchorGelWave>(), 95, 0f);
                        }
                    }

                    // Create tile collision dust.
                    Collision.HitTiles(npc.BottomLeft, Vector2.UnitY * 10f, npc.width, 20);

                    attackTimer = 225f;
                    npc.velocity.X = 0f;
                    npc.netUpdate = true;
                }

                if (npc.velocity.Y < 11f)
                    npc.velocity.Y += 0.2f;
            }
        }

        public static void DoAttack_ShortLeaps(NPC npc, Player target, ref float attackTimer)
        {
            int maxJumps = 5;
            ref float jumpCount = ref npc.Infernum().ExtraAI[0];
            npc.noGravity = false;

            if (npc.velocity.Y == 0f)
            {
                npc.TargetClosest(true);
                npc.velocity.X *= 0.85f;
                float jumpDelay = 15f + 30f * (npc.life / (float)npc.lifeMax);
                float forwardJumpSpeed = 6f + 5f * (1f - npc.life / (float)npc.lifeMax);
                float jumpSpeed = 6f;
                if (!Collision.CanHit(npc.Center, 1, 1, target.Center, 1, 1))
                    jumpSpeed += 2.4f;

                if (attackTimer > jumpDelay)
                {
                    npc.TargetClosest();

                    // Spawn slimes every second leap.
                    if (Main.netMode != NetmodeID.MultiplayerClient && jumpCount % 2 == 1)
                    {
                        int slimeType = Main.rand.NextBool(3) ? ModContent.NPCType<SlimeSpawnCrimson2>() : ModContent.NPCType<SlimeSpawnCrimson>();
                        NPC.NewNPC((int)npc.Center.X, (int)npc.Bottom.Y - 10, slimeType);
                    }

                    if (jumpCount == 2f || jumpCount == 4f)
                    {
                        jumpSpeed *= 2f;
                        forwardJumpSpeed /= 2f;
                    }

                    npc.velocity.Y = -jumpSpeed;
                    npc.velocity.X = forwardJumpSpeed * npc.direction;
                    jumpCount++;

                    npc.netUpdate = true;
                }
            }
            else
            {
                if (jumpCount < maxJumps)
                    attackTimer = 0f;

                npc.velocity.X *= 0.99f;
                if (npc.direction < 0 && npc.velocity.X > -1f)
                    npc.velocity.X = -1f;
                if (npc.direction > 0 && npc.velocity.X < 1f)
                    npc.velocity.X = 1f;
            }

            // Go to the next attack state after enough leaps have been performed.
            if (jumpCount >= maxJumps && attackTimer >= 60f)
                attackTimer = 1000f;
        }

        public static void DoAttack_BigSlam(NPC npc, Player target, ref float attackTimer)
        {
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            if (attackSubstate == 0f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 12f, 0.04f);
                npc.noGravity = true;

                if (attackTimer >= 60f)
                {
                    npc.velocity = Vector2.UnitY * 7f;
                    attackSubstate = 1f;
                    npc.netUpdate = true;
                }
            }

            else
            {
                if (npc.velocity.Y == 0f && attackTimer < 225f)
                {
                    Main.PlaySound(SoundID.NPCHit1, npc.Center);
                    attackSubstate = 2f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 13; i++)
                        {
                            Vector2 ballVelocity = Vector2.UnitX * Main.rand.NextBool(2).ToDirectionInt() * Main.rand.NextFloat(5f, 8f);
                            ballVelocity.Y -= Main.rand.NextFloat(3.5f, 8f);
                            Utilities.NewProjectileBetter(npc.Bottom - Vector2.UnitY * 30f, ballVelocity, ModContent.ProjectileType<RedirectingIchorBall>(), 100, 0f);
                        }
                    }

                    // Create tile collision dust.
                    Collision.HitTiles(npc.BottomLeft, Vector2.UnitY * 10f, npc.width, 20);

                    npc.velocity.X = 0f;
                    attackTimer = 225f;
                    npc.netUpdate = true;
                }

                if (npc.velocity.Y < 14f)
                    npc.velocity.Y += 0.7f;
            }
        }
        #endregion AI
    }
}
