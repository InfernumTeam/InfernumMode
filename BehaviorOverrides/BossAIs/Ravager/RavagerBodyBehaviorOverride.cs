using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class RavagerBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<RavagerBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public const float BaseDR = 0.25f;
        public const float SittingStillDR = 0.66f;

        public const float ArenaBorderOffset = 1850f;

        #region Enumerations
        public enum RavagerAttackType
        {
            DarkRitual,
            FortressSlam,
            SpikeBarrage,
            Count
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Ensure that the NPC always draws things, even when far away.
            // Not doing this will result in the arena not being drawn if far from the target.
            NPCID.Sets.MustAlwaysDraw[npc.type] = true;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Create limbs.
            if (npc.localAI[0] == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC((int)npc.Center.X - 70, (int)npc.Center.Y + 88, ModContent.NPCType<RavagerLegLeft>(), npc.whoAmI);
                NPC.NewNPC((int)npc.Center.X + 70, (int)npc.Center.Y + 88, ModContent.NPCType<RavagerLegRight>(), npc.whoAmI);
                NPC.NewNPC((int)npc.Center.X - 120, (int)npc.Center.Y + 50, ModContent.NPCType<RavagerClawLeft>(), npc.whoAmI);
                NPC.NewNPC((int)npc.Center.X + 120, (int)npc.Center.Y + 50, ModContent.NPCType<RavagerClawRight>(), npc.whoAmI);
                NPC.NewNPC((int)npc.Center.X + 1, (int)npc.Center.Y - 20, ModContent.NPCType<RavagerHead>(), npc.whoAmI);
                npc.localAI[0] = 1f;
            }

            CalamityGlobalNPC.scavenger = npc.whoAmI;

            // Fade in.
            bool shouldNotAttack = false;
            if (npc.alpha > 0)
            {
                npc.alpha = Utils.Clamp(npc.alpha - 10, 0, 255);
                shouldNotAttack = true;
            }

            // Reset things every frame.
            npc.Calamity().DR = BaseDR;
            npc.damage = npc.defDamage;

            npc.noTileCollide = false;
            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f))
            {
                npc.noTileCollide = true;
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * -30f, 0.2f);
                if (!npc.WithinRange(target.Center, 3000f))
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            // Constantly give the target Weak Pertrification.
            if (Main.netMode != NetmodeID.Server)
            {
                if (!target.dead && target.active)
                    target.AddBuff(ModContent.BuffType<WeakPetrification>(), 15);
            }

            bool leftLegActive = false;
            bool rightLegActive = false;
            bool leftClawActive = false;
            bool rightClawActive = false;
            bool headActive = false;

            ref float darkMagicFireballShootTimer = ref npc.ai[1];
            ref float jumpTimer = ref npc.ai[2];
            ref float jumpState = ref npc.ai[3];
            ref float specialAttackStartDelay = ref npc.Infernum().ExtraAI[0];
            ref float specialAttackType = ref npc.Infernum().ExtraAI[1];
            ref float specialAttackTimer = ref npc.Infernum().ExtraAI[2];
            ref float attackDelay = ref npc.Infernum().ExtraAI[5];
            ref float horizontalArenaCenterX = ref npc.Infernum().ExtraAI[6];

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<RavagerHead>())
                    headActive = true;
                if (Main.npc[i].active && Main.npc[i].Infernum().ExtraAI[0] == 0f && Main.npc[i].type == ModContent.NPCType<RavagerClawRight>())
                    rightClawActive = true;
                if (Main.npc[i].active && Main.npc[i].Infernum().ExtraAI[0] == 0f && Main.npc[i].type == ModContent.NPCType<RavagerClawLeft>())
                    leftClawActive = true;
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<RavagerLegRight>())
                    rightLegActive = true;
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<RavagerLegLeft>())
                    leftLegActive = true;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool anyLimbsArePresent = leftLegActive || rightLegActive || leftClawActive || rightClawActive || headActive;
            bool shouldBeBuffed = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive;

            int darkMagicFireballShootRate = 75;
            int fireballsPerBurst = shouldBeBuffed ? 11 : 8;
            int jumpDelay = !leftClawActive || !rightClawActive ? 270 : 210;
            float darkMagicFireballSpeed = shouldBeBuffed ? 16f : 11.5f;
            float gravity = 0.625f;
            if (!anyLimbsArePresent)
            {
                darkMagicFireballShootRate += 40;
                gravity += 0.25f;
                jumpDelay -= 75;
            }
            if (shouldBeBuffed)
            {
                darkMagicFireballShootRate -= 20;
                jumpDelay /= 2;
            }
            if (BossRushEvent.BossRushActive)
            {
                darkMagicFireballShootRate = (int)(darkMagicFireballShootRate * 0.6f);
                fireballsPerBurst += 12;
                darkMagicFireballSpeed *= 1.7f;
                jumpDelay /= 2;
            }

            // Jump much more quickly when outside of the arena to minimize time where the target can't hit the boss.
            if (npc.Right.X < horizontalArenaCenterX - ArenaBorderOffset || npc.Left.X > horizontalArenaCenterX + ArenaBorderOffset)
                jumpDelay /= 3;

            // Decide the arena positions once the limbs are gone.
            if (horizontalArenaCenterX == 0f)
            {
                horizontalArenaCenterX = target.Center.X;
                npc.netUpdate = true;
            }

            // Once the arena has been decided restrict the target's position to within that area.
            else
            {
                float left = horizontalArenaCenterX - ArenaBorderOffset + 28f;
                float right = horizontalArenaCenterX + ArenaBorderOffset - 28f;
                target.Center = Vector2.Clamp(target.Center, new Vector2(left, -100f), new Vector2(right, Main.maxTilesY * 16f + 100f));
            }

            npc.dontTakeDamage = anyLimbsArePresent;
            npc.gfxOffY = -12;

            // Make the attack delay pass.
            attackDelay++;
            if (attackDelay < 135f)
                shouldNotAttack = true;

            // Handle special attacks. Only applicable once limbs are gone.
            if (specialAttackStartDelay < 720f)
            {
                if (!anyLimbsArePresent)
                    specialAttackStartDelay++;
            }
            else
            {
                switch ((RavagerAttackType)(int)specialAttackType)
                {
                    case RavagerAttackType.DarkRitual:
                        DoSpecialAttack_DarkRitual(npc, target, lifeRatio, shouldBeBuffed, ref specialAttackTimer, ref specialAttackType, ref specialAttackStartDelay);
                        break;
                    case RavagerAttackType.FortressSlam:
                        DoSpecialAttack_FortressSlam(npc, target, lifeRatio, shouldBeBuffed, ref gravity, ref specialAttackTimer, ref specialAttackType, ref specialAttackStartDelay);
                        break;
                    case RavagerAttackType.SpikeBarrage:
                        DoSpecialAttack_SpikeBarrage(npc, target, lifeRatio, shouldBeBuffed, ref gravity, ref specialAttackTimer, ref specialAttackType, ref specialAttackStartDelay);
                        break;
                }

                specialAttackTimer++;

                // Disable typical jumps/dark magic fireballs.
                jumpState = 0f;
                shouldNotAttack = true;
            }

            // Periodically release bursts of dark magic fireballs.
            if (!shouldNotAttack && darkMagicFireballShootTimer >= darkMagicFireballShootRate && jumpState == 0f)
            {
                Main.PlaySound(SoundID.Item100, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int darkMagicFireballDamage = shouldBeBuffed ? 335 : 215;
                    for (int i = 0; i < fireballsPerBurst; i++)
                    {
                        Vector2 darkMagicFireballVelocity = (MathHelper.TwoPi * i / fireballsPerBurst).ToRotationVector2() * darkMagicFireballSpeed;
                        Utilities.NewProjectileBetter(npc.Center + darkMagicFireballVelocity * 2f, darkMagicFireballVelocity, ModContent.ProjectileType<DarkMagicFireball>(), darkMagicFireballDamage, 0f);
                    }
                    darkMagicFireballShootTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Jump towards the target if they're far enough away and enough time passes.
            if (!shouldNotAttack && !npc.WithinRange(target.Center, 200f) && jumpState == 0f && npc.velocity.Y == 0f)
            {
                jumpTimer++;
                if (jumpTimer >= jumpDelay)
                {
                    jumpTimer = 0f;
                    jumpState = 1f;

                    npc.velocity.Y -= 10.005f;
                    if (target.position.Y + target.height < npc.Center.Y)
                        npc.velocity.Y -= 1.25f;
                    if (target.position.Y + target.height < npc.Center.Y - 40f)
                        npc.velocity.Y -= 1.5f;
                    if (target.position.Y + target.height < npc.Center.Y - 80f)
                        npc.velocity.Y -= 1.75f;
                    if (target.position.Y + target.height < npc.Center.Y - 120f)
                        npc.velocity.Y -= 2.5f;
                    if (target.position.Y + target.height < npc.Center.Y - 160f)
                        npc.velocity.Y -= 4f;
                    if (target.position.Y + target.height < npc.Center.Y - 200f)
                        npc.velocity.Y -= 4f;
                    if (target.position.Y + target.height < npc.Center.Y - 400f)
                        npc.velocity.Y -= 9f;
                    if (!Collision.CanHit(npc.Center, 1, 1, target.Center, 1, 1))
                        npc.velocity.Y -= 4.8f;
                    if (MathHelper.Distance(npc.Center.X, target.Center.X) < 225f)
                        npc.velocity.Y -= 5f;

                    npc.velocity.X = (target.Center.X > npc.Center.X).ToDirectionInt() * 19.5f;
                    npc.velocity *= 0.8f;
                    npc.netUpdate = true;
                }
            }

            if (jumpState == 1f)
            {
                // Make stomp sounds and dusts when hitting the ground again.
                if (npc.velocity.Y == 0f)
                {
                    Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 14, 1.25f, -0.25f);
                    for (int x = (int)npc.Left.X - 30; x < (int)npc.Right.X + 30; x += 10)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            Dust stompDust = Dust.NewDustDirect(new Vector2(x, npc.Bottom.Y), npc.width + 30, 4, 31, 0f, 0f, 100, default, 1.5f);
                            stompDust.velocity *= 0.2f;
                        }

                        Gore stompGore = Gore.NewGoreDirect(new Vector2(x, npc.Bottom.Y - 12f), default, Main.rand.Next(61, 64), 1f);
                        stompGore.velocity *= 0.4f;
                    }

                    int shockwaveDamage = shouldBeBuffed ? 380 : 250;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (!anyLimbsArePresent)
                            NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BreakableRockPillar>(), npc.whoAmI);
                        else
                            Utilities.NewProjectileBetter(npc.Bottom + Vector2.UnitY * 40f, Vector2.Zero, ModContent.ProjectileType<StompShockwave>(), shockwaveDamage, 0f);
                    }

                    jumpState = 0f;
                    npc.netUpdate = true;
                }

                // Fall through tiles in the way.
                if (!target.dead)
                {
                    if ((target.position.Y > npc.Bottom.Y && npc.velocity.Y > 0f) || (target.position.Y < npc.Bottom.Y && npc.velocity.Y < 0f))
                        npc.noTileCollide = true;
                    else if ((npc.velocity.Y > 0f && npc.Bottom.Y > target.Top.Y) || (Collision.CanHit(npc.position, npc.width, npc.height, target.Center, 1, 1) && !Collision.SolidCollision(npc.position, npc.width, npc.height)))
                        npc.noTileCollide = false;
                }
            }
            else
                npc.velocity.X *= 0.8f;

            // Do custom gravity stuff.
            npc.noGravity = true;
            EnforceCustomGravity(npc, gravity);

            darkMagicFireballShootTimer++;

            return false;
        }

        public static void DoSpecialAttack_DarkRitual(NPC npc, Player target, float lifeRatio, bool shouldBeBuffed, ref float attackTimer, ref float specialAttackType, ref float specialAttackStartDelay)
        {
            int sitStillTime = (int)MathHelper.Lerp(720f, 560f, 1f - lifeRatio);
            int soulTorrentReleaseTime = shouldBeBuffed ? 295 : 240;
            int soulShootRate = (int)MathHelper.Lerp(33f, 24f, 1f - lifeRatio);
            ref float attackState = ref npc.Infernum().ExtraAI[3];

            switch ((int)attackState)
            {
                // Fade out prior to teleporting.
                case 0:
                    npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.065f, 0f, 1f);

                    // Go to the next attack state once completely invisible.
                    if (npc.Opacity <= 0f)
                    {
                        attackState++;
                        npc.netUpdate = true;
                    }
                    break;

                // Find a place to teleport to and spawn a bunch of flame pillars.
                case 1:
                    bool performedSuccessfulTeleport = false;
                    Point checkArea = target.Center.ToTileCoordinates();
                    if (WorldUtils.Find(checkArea, Searches.Chain(new Searches.Down(4500), new Conditions.IsSolid()), out Point groundedPosition))
                        checkArea = groundedPosition;

                    for (int teleportAttempts = 0; teleportAttempts < 100000; teleportAttempts++)
                    {
                        float maxSearchArea = MathHelper.Lerp(35f, 180f, (float)Math.Sqrt(teleportAttempts / 100000f));

                        // Ensure that the teleport position is not too close to the target, to prevent cheap hits.
                        Vector2 teleportOffset;
                        do
                            teleportOffset = Main.rand.NextVector2Square(-maxSearchArea, maxSearchArea);
                        while (teleportOffset.Length() < 18f);

                        Point teleportPosition = (checkArea.ToVector2() + teleportOffset).ToPoint();

                        // Discard areas that are not open enough.
                        bool fitsForRavager = !Collision.SolidTiles(teleportPosition.X - 8, teleportPosition.X + 8, teleportPosition.Y - 9, teleportPosition.Y);
                        bool fitsForPillars = !Collision.SolidTiles(teleportPosition.X - 11, teleportPosition.X + 11, teleportPosition.Y - 4, teleportPosition.Y);

                        if (!fitsForPillars || !fitsForRavager)
                            continue;

                        // And discard areas that don't have solid ground.
                        bool solidGround = false;
                        for (int i = -8; i < 8; i++)
                        {
                            Tile ground = CalamityUtils.ParanoidTileRetrieval(teleportPosition.X + i, teleportPosition.Y + 1);
                            bool notAFuckingTree = ground.type != TileID.Trees && ground.type != TileID.PineTree && ground.type != TileID.PalmTree;
                            if (ground.nactive() && notAFuckingTree && (Main.tileSolid[ground.type] || Main.tileSolidTop[ground.type]))
                            {
                                solidGround = true;
                                break;
                            }
                        }

                        if (!solidGround)
                            continue;

                        // Do the teleport if a suitable position is found.
                        npc.Bottom = teleportPosition.ToWorldCoordinates(8f, -4f);

                        // And spawn a bunch of flame pillars.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int totalPillarsOnEachSide = lifeRatio < 0.45f ? 3 : 2;
                            for (int i = 0; i < totalPillarsOnEachSide; i++)
                            {
                                float horizontalSpawnOffset = MathHelper.Lerp(195f, 475f, i / 3f);
                                NPC.NewNPC((int)(npc.Bottom.X - horizontalSpawnOffset), (int)npc.Bottom.Y - 8, ModContent.NPCType<FlamePillar>());
                                NPC.NewNPC((int)(npc.Bottom.X + horizontalSpawnOffset), (int)npc.Bottom.Y - 8, ModContent.NPCType<FlamePillar>());
                            }
                        }

                        performedSuccessfulTeleport = true;
                        break;
                    }

                    // Go to the next attack state if a teleport was successfully preformed.
                    if (performedSuccessfulTeleport)
                    {
                        attackState++;
                        attackTimer = 0f;
                    }

                    // If not, finish the attack early.
                    else
                    {
                        attackState = 0f;
                        specialAttackType = (specialAttackType + 1f) % (int)RavagerAttackType.Count;
                        specialAttackStartDelay = 0f;
                        attackTimer = 0f;
                    }

                    npc.netUpdate = true;
                    break;
                // Sit still and channel energy from the pillars. DR is increased in this state.
                case 2:
                    bool anyPillarsArePresent = NPC.AnyNPCs(ModContent.NPCType<FlamePillar>());

                    // Go to the 0 DR if the pillars are gone as a reward.
                    npc.Calamity().DR = anyPillarsArePresent ? SittingStillDR : 0f;

                    // Fade back in after the teleport.
                    npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.12f, 0f, 1f);

                    // Go to the next attack state if any pillars remain.
                    // If not, go to the next attack early.
                    if (attackTimer >= sitStillTime)
                    {
                        if (anyPillarsArePresent)
                        {
                            attackState++;
                            attackTimer = 0f;
                            npc.netUpdate = true;

                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastShoot"), target.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<RavagerScreamBoom>(), 0, 0f);

                            for (int i = 0; i < Main.maxNPCs; i++)
                            {
                                bool isClaw = Main.npc[i].type == ModContent.NPCType<RavagerClawLeft>() || Main.npc[i].type == ModContent.NPCType<RavagerClawRight>();
                                if (Main.npc[i].active && isClaw)
                                {
                                    Main.npc[i].ai[0] = (int)RavagerClawLeftBehaviorOverride.RavagerClawAttackState.BlueFireBursts;
                                    Main.npc[i].ai[1] = 0f;
                                    Main.npc[i].netUpdate = true;
                                }
                            }
                        }
                        else
                        {
                            attackState = 0f;
                            specialAttackType = (specialAttackType + 1f) % (int)RavagerAttackType.Count;
                            specialAttackStartDelay = 0f;
                            attackTimer = 0f;
                        }
                    }
                    break;

                // Release a torrent of souls.
                // This is negated if the pillars were destroyed early.
                case 3:
                    // Cause any remaining pillars to crumble.
                    int flamePillarNPCType = ModContent.NPCType<FlamePillar>();
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].type != flamePillarNPCType || !Main.npc[i].active)
                            continue;

                        Main.npc[i].life = 0;
                        Main.npc[i].HitEffect();
                        Main.npc[i].checkDead();
                        Main.npc[i].active = false;
                    }

                    Vector2 shootPosition = npc.Center - Vector2.UnitY * 36f;
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % soulShootRate == soulShootRate - 1f && attackTimer < soulTorrentReleaseTime)
                    {
                        int soulCount = shouldBeBuffed ? 3 : Main.rand.Next(1, 3);
                        for (int i = 0; i < soulCount; i++)
                        {
                            int projectileType = Main.rand.NextBool(3) ? ModContent.ProjectileType<RedSoul>() : ModContent.ProjectileType<BlueSoul>();
                            int soulDamage = shouldBeBuffed ? 335 : 225;
                            Vector2 soulShootVelocity = (target.Center - shootPosition).SafeNormalize(-Vector2.UnitY).RotatedByRandom(0.42f) * Main.rand.NextFloat(8.5f, 10.5f);
                            if (shouldBeBuffed)
                                soulShootVelocity *= 1.37f;

                            Utilities.NewProjectileBetter(shootPosition, soulShootVelocity, projectileType, soulDamage, 0f);
                        }
                    }

                    // Go to the next attack.
                    if (attackTimer >= soulTorrentReleaseTime + 45f)
                    {
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            bool isClaw = Main.npc[i].type == ModContent.NPCType<RavagerClawLeft>() || Main.npc[i].type == ModContent.NPCType<RavagerClawRight>();
                            if (Main.npc[i].active && isClaw)
                            {
                                Main.npc[i].ai[0] = (int)RavagerClawLeftBehaviorOverride.RavagerClawAttackState.Hover;
                                Main.npc[i].ai[1] = 0f;
                                Main.npc[i].netUpdate = true;
                            }
                        }

                        attackState = 0f;
                        specialAttackType = (specialAttackType + 1f) % (int)RavagerAttackType.Count;
                        specialAttackStartDelay = 0f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;
            }
        }

        public static void DoSpecialAttack_FortressSlam(NPC npc, Player target, float lifeRatio, bool shouldBeBuffed, ref float gravity, ref float attackTimer, ref float specialAttackType, ref float specialAttackStartDelay)
        {
            int hoverTime = (int)MathHelper.Lerp(75f, 48f, 1f - lifeRatio);
            int hoverSlowdownTime = 15;
            int slamCount = 4;
            float hoverSpeed = MathHelper.Lerp(29f, 35f, 1f - lifeRatio);
            float verticalHoverOffset = MathHelper.Lerp(520f, 420f, 1f - lifeRatio);
            float slamGravityFactor = MathHelper.Lerp(2.85f, 3.15f, 1f - lifeRatio);
            ref float slamCounter = ref npc.Infernum().ExtraAI[3];
            ref float attackState = ref npc.Infernum().ExtraAI[4];

            if (shouldBeBuffed)
            {
                hoverTime -= 14;
                hoverSlowdownTime = 8;
                hoverSpeed += 5.6f;
                verticalHoverOffset -= 80f;
                slamGravityFactor *= 1.5f;
            }

            if (BossRushEvent.BossRushActive)
            {
                hoverTime -= 27;
                hoverSlowdownTime = 6;
                hoverSpeed *= 1.4f;
                verticalHoverOffset -= 80f;
                slamGravityFactor *= 1.7f;
            }

            switch ((int)attackState)
            {
                // Attempt to hover above the target.
                case 0:
                    // Negate gravity.
                    gravity = 0f;

                    // Don't do damage.
                    npc.damage = 0;

                    Vector2 hoverDestination = target.Center - Vector2.UnitY * verticalHoverOffset;
                    if (!npc.WithinRange(hoverDestination, 160f))
                    {
                        npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * hoverSpeed, 0.2f);
                        npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, 1.8f);
                    }
                    else
                    {
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, 0.3f);
                        npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, 2f);

                        // Stay away from the target directly, to prevent cheap hits.
                        npc.Center -= npc.SafeDirectionTo(target.Center, -Vector2.UnitY) * Utils.InverseLerp(270f, 165f, npc.Distance(target.Center), true) * 28f;
                    }

                    // Prepare to slow down after enough time has passed.
                    if (attackTimer >= hoverTime)
                    {
                        attackTimer = 0f;
                        attackState++;
                        npc.netUpdate = true;
                    }
                    break;

                // Slow down and prepare to fall.
                case 1:
                    // Negate gravity.
                    gravity = 0f;
                    npc.velocity *= 0.65f;

                    // Prepare to fall after enough time has passed.
                    if (attackTimer >= hoverSlowdownTime)
                    {
                        attackTimer = 0f;
                        attackState++;
                        npc.velocity.Y = 0.5f;
                        npc.noTileCollide = true;
                        npc.netUpdate = true;
                    }
                    break;

                // Fall very, very quickly.
                // Once tiles are hit, a shockwave and flames are created.
                case 2:
                    gravity *= slamGravityFactor;
                    npc.velocity.X = 0f;

                    // Fall through tiles in the way.
                    if (!target.dead)
                    {
                        if ((target.position.Y + 40f > npc.Bottom.Y && npc.velocity.Y > 0f) || (target.position.Y < npc.Bottom.Y && npc.velocity.Y < 0f))
                            npc.noTileCollide = true;
                        else if ((npc.velocity.Y > 0f && npc.Bottom.Y > target.Top.Y) || (Collision.CanHit(npc.position, npc.width, npc.height, target.Center, 1, 1) && !Collision.SolidCollision(npc.position, npc.width, npc.height)))
                            npc.noTileCollide = false;
                    }

                    // Make stomp sounds and dusts when hitting the ground again.
                    if (npc.velocity.Y == 0f && attackTimer > 15f)
                    {
                        Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 14, 1.25f, -0.25f);
                        for (int x = (int)npc.Left.X - 30; x < (int)npc.Right.X + 30; x += 10)
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                Dust stompDust = Dust.NewDustDirect(new Vector2(x, npc.Bottom.Y), npc.width + 30, 4, 31, 0f, 0f, 100, default, 1.5f);
                                stompDust.velocity *= 0.2f;
                            }

                            Gore stompGore = Gore.NewGoreDirect(new Vector2(x, npc.Bottom.Y - 12f), default, Main.rand.Next(61, 64), 1f);
                            stompGore.velocity *= 0.4f;
                        }

                        int shockwaveDamage = shouldBeBuffed ? 440 : 265;
                        int emberDamage = shouldBeBuffed ? 330 : 220;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            // Create the shockwave.
                            Utilities.NewProjectileBetter(npc.Bottom + Vector2.UnitY * 40f, Vector2.Zero, ModContent.ProjectileType<StompShockwave>(), shockwaveDamage, 0f);

                            // And embers that fly upward.
                            for (int i = 0; i < 30; i++)
                            {
                                Vector2 emberSpawnPosition = npc.Bottom + new Vector2(Main.rand.NextFloatDirection() * npc.width * 0.5f, 15f);
                                Vector2 emberShootVelocity = Vector2.UnitY * Main.rand.NextFloat(3f, 7f);
                                emberShootVelocity.X += MathHelper.Lerp(-29f, 29f, i / 29f) + Main.rand.NextFloatDirection() * 0.3f;
                                Utilities.NewProjectileBetter(emberSpawnPosition, emberShootVelocity, ModContent.ProjectileType<RisingDarkMagicFireball>(), emberDamage, 0f);
                            }
                        }

                        attackTimer = 0f;
                        attackState++;
                        npc.netUpdate = true;
                    }

                    break;

                // Sit in place for a moment.
                case 3:
                    if (attackTimer >= 20f)
                    {
                        attackTimer = 0f;
                        attackState = 0f;
                        slamCounter++;

                        if (slamCounter >= slamCount)
                        {
                            slamCounter = 0f;
                            specialAttackType = (specialAttackType + 1f) % (int)RavagerAttackType.Count;
                            specialAttackStartDelay = 0f;
                        }
                    }
                    break;
            }
        }

        public static void DoSpecialAttack_SpikeBarrage(NPC npc, Player target, float lifeRatio, bool shouldBeBuffed, ref float gravity, ref float attackTimer, ref float specialAttackType, ref float specialAttackStartDelay)
        {
            int spikeShootRate = (int)MathHelper.Lerp(12f, 8f, 1f - lifeRatio);
            int spikeShootTime = (int)MathHelper.Lerp(450f, 570f, 1f - lifeRatio);
            int spikeShootDelay = (int)MathHelper.Lerp(72f, 50f, 1f - lifeRatio);
            int spikeShootTransitionDelay = 145;
            float idealHoverSpeed = 26.5f;
            float horizontalArenaCenterX = npc.Infernum().ExtraAI[6];
            float spikeShootAngularVariance = MathHelper.ToRadians(35.55f);

            // This is an estimation based on a rewritten horizontal range formula.
            // Since gravity is not constant it is not precise, but works well enough as an approximation.
            // A flat multiplier is applied to make up for potential discrepancy (If the speed is too low parts of the arena will be "free" zones for the attack).
            float spikeShootSpeed = (float)Math.Sqrt(ArenaBorderOffset * RavagerSpike.AverageGravity / (float)Math.Sin(spikeShootAngularVariance * 2f)) * 1.4f;

            ref float attackState = ref npc.Infernum().ExtraAI[3];
            if (shouldBeBuffed)
            {
                spikeShootRate -= 2;
                spikeShootTime += 75;
                spikeShootDelay = (int)(spikeShootDelay * 0.6f);
                idealHoverSpeed = 35f;
            }

            switch ((int)attackState)
            {
                // Fly into the air, above the ideal X position.
                case 0:
                    // Negate gravity.
                    gravity = 0f;

                    Vector2 hoverDestination = new Vector2(horizontalArenaCenterX, target.Center.Y - 640f);
                    Vector2 idealHoverVelocity = npc.SafeDirectionTo(hoverDestination) * idealHoverSpeed;
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, idealHoverVelocity.X, 0.0725f);
                    npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, idealHoverVelocity.Y, 0.12f);
                    npc.velocity = npc.velocity.MoveTowards(idealHoverVelocity, 1.85f);

                    // Go to the next attack state once the destination has been reached.
                    if (npc.WithinRange(hoverDestination, 45f))
                    {
                        attackState++;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;
                // Slam downward until reaching ground.
                case 1:
                    gravity *= 3.33f;
                    npc.velocity.X *= 0.67f;

                    // Fall through tiles in the way.
                    if (!target.dead)
                    {
                        if ((target.position.Y > npc.Bottom.Y && npc.velocity.Y > 0f) || (target.position.Y < npc.Bottom.Y && npc.velocity.Y < 0f))
                            npc.noTileCollide = true;
                        else if ((npc.velocity.Y > 0f && npc.Bottom.Y > target.Top.Y) || (Collision.CanHit(npc.position, npc.width, npc.height, target.Center, 1, 1) && !Collision.SolidCollision(npc.position, npc.width, npc.height)))
                            npc.noTileCollide = false;
                    }

                    // Make stomp sounds and dusts when hitting the ground again.
                    if (npc.velocity.Y == 0f && attackTimer > 15f)
                    {
                        Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 14, 1.25f, -0.25f);
                        for (int x = (int)npc.Left.X - 30; x < (int)npc.Right.X + 30; x += 10)
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                Dust stompDust = Dust.NewDustDirect(new Vector2(x, npc.Bottom.Y), npc.width + 30, 4, 31, 0f, 0f, 100, default, 1.5f);
                                stompDust.velocity *= 0.2f;
                            }

                            Gore stompGore = Gore.NewGoreDirect(new Vector2(x, npc.Bottom.Y - 12f), default, Main.rand.Next(61, 64), 1f);
                            stompGore.velocity *= 0.4f;
                        }

                        attackTimer = 0f;
                        attackState++;
                        npc.netUpdate = true;
                    }
                    break;
                // Sit in place and release spikes into the air.
                // They fly far but descend relatively slowly to prevent bullshit hits.
                case 2:
                    bool canShoot = attackTimer > spikeShootDelay && attackTimer < spikeShootDelay + spikeShootTime;
                    if (canShoot && attackTimer % spikeShootRate == spikeShootRate - 1f)
                    {
                        Main.PlaySound(SoundID.Item39, npc.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                int spikeDamage = shouldBeBuffed ? 305 : 200;
                                Vector2 spikeVelocity = -Vector2.UnitY.RotatedByRandom(spikeShootAngularVariance) * spikeShootSpeed * Main.rand.NextFloat(0.65f, 1.1f);
                                Utilities.NewProjectileBetter(npc.Center, spikeVelocity, ModContent.ProjectileType<RavagerSpike>(), spikeDamage, 0f);
                            }
                        }
                    }

                    // Gain more DR while sitting in place to prevent obliteration.
                    npc.Calamity().DR = SittingStillDR;

                    // Go to the next attack.
                    if (attackTimer >= spikeShootDelay + spikeShootTime + spikeShootTransitionDelay)
                    {
                        attackState = 0f;
                        specialAttackType = (specialAttackType + 1f) % (int)RavagerAttackType.Count;
                        specialAttackStartDelay = 0f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;
            }
        }

        public static void EnforceCustomGravity(NPC npc, float gravity)
        {
            float maxFallSpeed = 38f;
            if (npc.wet)
            {
                if (npc.honeyWet)
                {
                    gravity *= 0.33f;
                    maxFallSpeed *= 0.4f;
                }
                else if (npc.lavaWet)
                {
                    gravity *= 0.66f;
                    maxFallSpeed *= 0.7f;
                }
            }

            npc.velocity.Y += gravity;
            if (npc.velocity.Y > maxFallSpeed)
                npc.velocity.Y = maxFallSpeed;
        }
        #endregion AI

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float horizontalArenaCenterX = npc.Infernum().ExtraAI[6];
            Texture2D borderTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Ravager/RockBorder");

            // Draw obstructive pillars if an arena center is defined.
            if (horizontalArenaCenterX != 0f)
            {
                for (int i = -20; i < 20; i++)
                {
                    float verticalOffset = borderTexture.Height * i;

                    for (int direction = -1; direction <= 1; direction += 2)
                    {
                        Vector2 drawPosition = new Vector2(horizontalArenaCenterX - ArenaBorderOffset * direction, Main.LocalPlayer.Center.Y + verticalOffset);
                        drawPosition.Y -= drawPosition.Y % borderTexture.Height;
                        drawPosition -= Main.screenPosition;
                        spriteBatch.Draw(borderTexture, drawPosition, null, Color.White, 0f, borderTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                    }
                }
            }
            return true;
        }
        #endregion
    }
}
