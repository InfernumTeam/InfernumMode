using CalamityMod.NPCs;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

using EbonianSlimeGod = CalamityMod.NPCs.SlimeGod.SlimeGod;

namespace InfernumMode.FuckYouModeAIs.SlimeGod
{
    public class EbonianSlimeGodBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<EbonianSlimeGod>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum CrimulanSlimeGodAttackType
        {
            CorruptSpawning = 0,
            LongSlamWave = 1,
            ShortLeaps = 2,
            GelCloudSlam = 3
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGod) || !NPC.AnyNPCs(ModContent.NPCType<SlimeGodRun>()))
            {
                npc.life = 0;
                npc.StrikeNPCNoInteraction(9999, 0f, 0);
                npc.HitEffect();
                Main.PlaySound(SoundID.NPCDeath1, npc.Center);
                return false;
            }

            // This will affect the other gods as well in terms of behavior.
            ref float universalState = ref Main.npc[CalamityGlobalNPC.slimeGod].ai[0];
            ref float universalTimer = ref Main.npc[CalamityGlobalNPC.slimeGod].ai[1];
            ref float stuckTimer = ref npc.Infernum().ExtraAI[5];
            ref float stuckTeleportCountdown = ref npc.Infernum().ExtraAI[6];

            if (stuckTeleportCountdown > 0f)
            {
                stuckTeleportCountdown--;

                npc.velocity.X = 0f;
                npc.velocity.Y += 0.3f;
                npc.scale = 1f - stuckTeleportCountdown / 40f;
                npc.damage = 0;
                return false;
            }

            npc.damage = npc.defDamage;

            if (!Collision.CanHit(target.Center, 1, 1, npc.Center, 1, 1))
            {
                stuckTimer++;
                if (stuckTimer > 180f)
                {
                    stuckTimer = 0f;
                    npc.Center = target.Center - Vector2.UnitY * 10f;
                    stuckTeleportCountdown = 40f;
                    npc.netUpdate = true;
                }
            }
            else if (stuckTimer > 0f)
                stuckTimer--;

            // Set the universal whoAmI variable.
            CalamityGlobalNPC.slimeGodPurple = npc.whoAmI;
            npc.realLife = CalamityGlobalNPC.slimeGodRed;
            if (npc.realLife == -1)
                return false;
            npc.life = Main.npc[npc.realLife].life;
            npc.lifeMax = Main.npc[npc.realLife].lifeMax;

            switch ((CrimulanSlimeGodAttackType)(int)universalState)
            {
                case CrimulanSlimeGodAttackType.CorruptSpawning:
                    DoAttack_CorruptSpawning(npc, target, ref universalTimer);
                    break;
                case CrimulanSlimeGodAttackType.LongSlamWave:
                    DoAttack_LongSlamWave(npc, target, ref universalTimer);
                    break;
                case CrimulanSlimeGodAttackType.ShortLeaps:
                    DoAttack_ShortLeaps(npc, target, ref universalTimer);
                    break;
                case CrimulanSlimeGodAttackType.GelCloudSlam:
                    DoAttack_GelCloudSlam(npc, target, ref universalTimer);
                    break;
            }

            return false;
        }

        public static void DoAttack_CorruptSpawning(NPC npc, Player target, ref float attackTimer)
		{
            npc.noTileCollide = npc.Bottom.Y < target.Top.Y - 36f;

            Vector2 destination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 360f;
            destination.Y = MathHelper.Clamp(destination.Y - 3200f, 160f, Main.maxTilesX * 16f - 32f);

            if (WorldUtils.Find(destination.ToTileCoordinates(), Searches.Chain(new Searches.Down(450), new Conditions.IsSolid()), out Point result))
                destination.Y = result.Y * 16f - 8f;

            bool onSolidGround = WorldGen.SolidTile(Framing.GetTileSafely(npc.Bottom + Vector2.UnitY * 16f));

            if (MathHelper.Distance(npc.Center.X, destination.X) > 50f)
            {
                if (onSolidGround)
                {
                    npc.velocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Center, destination, 0.3f, 15f, out _);
                    npc.netUpdate = true;
                }
            }
            else
                npc.velocity.X *= 0.5f;

            int slimeSpawnCount = NPC.CountNPCS(ModContent.NPCType<SlimeSpawnCorrupt2>());
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 50f == 49f && slimeSpawnCount < 4)
			{
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SlimeSpawnCorrupt2>());
                npc.netUpdate = true;
			}

            if (npc.velocity.Y < 15f)
                npc.velocity.Y += 0.3f;
        }

        public static void DoAttack_LongSlamWave(NPC npc, Player target, ref float attackTimer)
        {
            npc.noTileCollide = npc.Bottom.Y < target.Top.Y - 36f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float waveShootCounter = ref npc.Infernum().ExtraAI[1];
            bool onSolidGround = WorldGen.SolidTile(Framing.GetTileSafely(npc.Bottom + Vector2.UnitY * 16f));

            if (onSolidGround && attackSubstate == 0f)
			{
                npc.velocity = -Vector2.UnitY * 24f;
                attackSubstate = 1f;
                npc.netUpdate = true;
			}

            if (attackSubstate == 1f)
			{
                npc.velocity.Y += 0.1f;
                if (npc.velocity.Y > 0f && onSolidGround)
				{
                    Main.PlaySound(SoundID.Item73, target.Center);

                    for (int i = 0; i < 20; i++)
                        Dust.NewDustPerfect(npc.Bottom + Vector2.UnitX * Main.rand.NextFloat(-npc.width, npc.height) * 0.5f, 75);

                    attackSubstate = 2f;
                    npc.netUpdate = true;
				}
			}

            if (attackSubstate == 2f)
			{
                waveShootCounter++;
                if (Main.netMode != NetmodeID.MultiplayerClient && waveShootCounter % 30f == 20f)
				{
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 waveSpawnPosition = npc.Bottom + new Vector2(waveShootCounter / 30f * i * 120f, 160f);
                        if (target.Center.Y > waveSpawnPosition.Y + 120f)
                            waveSpawnPosition.Y = target.Center.Y + 240f;

                        Utilities.NewProjectileBetter(waveSpawnPosition, -Vector2.UnitY * 6f, ModContent.ProjectileType<CursedGelWave>(), 95, 0f);
                    }
                }
            }

            npc.velocity.X *= 0.94f;
            if (npc.velocity.Y < 15f)
                npc.velocity.Y += 0.3f;
        }

        public static void DoAttack_ShortLeaps(NPC npc, Player target, ref float attackTimer)
        {
            int maxJumps = 6;
            npc.noGravity = false;
            npc.noTileCollide = npc.Bottom.Y < target.Top.Y - 36f;

            ref float jumpCount = ref npc.Infernum().ExtraAI[0];

            if (npc.velocity.Y == 0f)
            {
                npc.TargetClosest(true);
                npc.velocity.X *= 0.85f;
                float jumpDelay = 15f + 30f * (npc.life / (float)npc.lifeMax);
                float forwardJumpSpeed = 10f + 5f * (1f - npc.life / (float)npc.lifeMax);
                float jumpSpeed = 16;
                if (!Collision.CanHit(npc.Center, 1, 1, target.Center, 1, 1))
                    jumpSpeed += 4f;

                if (attackTimer > jumpDelay)
                {
                    npc.TargetClosest();

                    // Spawn abyss balls upward.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            Vector2 ballVelocity = Vector2.UnitX * Main.rand.NextBool(2).ToDirectionInt() * Main.rand.NextFloat(2f, 3.4f);
                            ballVelocity.Y -= Main.rand.NextFloat(3.5f, 9f);
                            Utilities.NewProjectileBetter(npc.Bottom - Vector2.UnitY * 30f, ballVelocity, ModContent.ProjectileType<RedirectingCursedBall>(), 100, 0f);
                        }
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
                npc.velocity.X *= 0.99f;
                if (npc.direction < 0 && npc.velocity.X > -1f)
                    npc.velocity.X = -1f;
                if (npc.direction > 0 && npc.velocity.X < 1f)
                    npc.velocity.X = 1f;
            }

            // Go to the next attack state after enough leaps have been performed.
            if (jumpCount >= maxJumps)
                attackTimer = 1000f;
        }

        public static void DoAttack_GelCloudSlam(NPC npc, Player target, ref float attackTimer)
        {
            bool onSolidGround = WorldGen.SolidTile(Framing.GetTileSafely(npc.Bottom + Vector2.UnitY * 16f));
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 60f)
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EbonianCloud>());

            npc.scale = 1f - (float)System.Math.Sin(Utils.InverseLerp(220f, 260f, attackTimer, true) * MathHelper.Pi);
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 240f)
            {
                int cloud = NPC.FindFirstNPC(ModContent.NPCType<EbonianCloud>());
                if (Main.npc.IndexInRange(cloud))
                {
                    npc.Center = Main.npc[cloud].Center;
                    Main.npc[cloud].active = false;
                    Main.npc[cloud].netUpdate = true;
                }

                float jumpSpeed = 22f;
                npc.velocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Center, target.Center, 0.35f, jumpSpeed, out _);

                for (int i = 0; i < 10; i++)
                {
                    Vector2 abyssBallSpwanPosition = npc.Bottom + Main.rand.NextVector2Circular(50f, 10f);
                    Utilities.NewProjectileBetter(abyssBallSpwanPosition, Main.rand.NextVector2Circular(7f, 7f), ModContent.ProjectileType<AbyssMine>(), 90, 0f);
                }

                npc.netUpdate = true;
            }

            if (attackTimer < 240f || npc.velocity.Y == 0f)
                npc.velocity.X *= 0.92f;

            if (npc.velocity.Y < 14f)
                npc.velocity.Y += 0.35f;
        }
        #endregion AI
    }
}
