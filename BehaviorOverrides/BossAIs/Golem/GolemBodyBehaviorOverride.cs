using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using InfernumMode.Tiles;
using Microsoft.Xna.Framework.Graphics;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public enum GolemAttackState
    {
        ArmBullets,
        FistSpin,
        SpikeTrapWaves,
        HeatRay,
        SpinLaser,
        Slingshot
    }

    public class GolemBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Golem;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public static int ArenaWidth = 115;
        public static int ArenaHeight = 105;

        public override bool PreAI(NPC npc)
        {
            ref float AITimer = ref npc.ai[0];
            ref float AttackState = ref npc.ai[1];
            ref float AttackTimer = ref npc.ai[2];

            ref float LeftFistNPC = ref npc.Infernum().ExtraAI[0];
            ref float RightFistNPC = ref npc.Infernum().ExtraAI[1];
            ref float AttachedHeadNPC = ref npc.Infernum().ExtraAI[2];
            ref float FreeHeadNPC = ref npc.Infernum().ExtraAI[3];
            ref float HeadState = ref npc.Infernum().ExtraAI[4];
            ref float EnrageState = ref npc.Infernum().ExtraAI[5];
            ref float ReturnFromEnrageState = ref npc.Infernum().ExtraAI[6];
            ref float AttackCooldown = ref npc.Infernum().ExtraAI[7];
            bool FreeHead = HeadState == 1;

            Vector2 attachedHeadCenterPos = new Vector2(npc.Center.X, npc.Top.Y);
            Vector2 leftHandCenterPos = new Vector2(npc.Left.X, npc.Left.Y);
            Vector2 rightHandCenterPos = new Vector2(npc.Right.X, npc.Right.Y);

            if (AITimer == 0f)
            {
                npc.TargetClosest();

                // If the NPC cap is reached, the fight will break, so just don't do anything
                int npcCount = Main.npc.Count(n => n.active);
                if (npcCount > Main.maxNPCs - 4)
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);

                    return false;
                }
                
                // Otherwise prepare the fight
                npc.life = npc.lifeMax = 80000;
                npc.noGravity = true;
                npc.noTileCollide = false;
                npc.chaseable = false;
                npc.Opacity = 1f;
                AttackState = (float)GolemAttackState.FistSpin;
                npc.netUpdate = true;

                // Set the whoAmI variable.
                NPC.golemBoss = npc.whoAmI;

                int freeHeadInt = NPC.NewNPC((int)npc.Center.X - 55, (int)npc.Top.Y, NPCID.GolemHeadFree);
                Main.npc[freeHeadInt].Center = attachedHeadCenterPos;
                Main.npc[freeHeadInt].dontTakeDamage = true;
                Main.npc[freeHeadInt].noGravity = true;
                Main.npc[freeHeadInt].noTileCollide = true;
                Main.npc[freeHeadInt].lifeMax = Main.npc[freeHeadInt].life = npc.lifeMax;
                Main.npc[freeHeadInt].ai[0] = npc.whoAmI;
                Main.npc[freeHeadInt].netUpdate = true;
                FreeHeadNPC = freeHeadInt;

                int attachedHeadInt = NPC.NewNPC((int)npc.Center.X - 55, (int)npc.Top.Y, NPCID.GolemHead);
                Main.npc[attachedHeadInt].Center = attachedHeadCenterPos;
                Main.npc[attachedHeadInt].lifeMax = Main.npc[attachedHeadInt].life = npc.lifeMax;
                Main.npc[attachedHeadInt].noGravity = true;
                Main.npc[attachedHeadInt].noTileCollide = true;
                Main.npc[attachedHeadInt].ai[0] = npc.whoAmI;
                Main.npc[attachedHeadInt].netUpdate = true;
                AttachedHeadNPC = attachedHeadInt;

                int leftHand = NPC.NewNPC((int)npc.Left.X, (int)npc.Left.Y, NPCID.GolemFistLeft);
                Main.npc[leftHand].lifeMax = Main.npc[leftHand].life = 1;
                Main.npc[leftHand].dontTakeDamage = true;
                Main.npc[leftHand].noGravity = true;
                Main.npc[leftHand].noTileCollide = false;
                Main.npc[leftHand].ai[0] = npc.whoAmI;
                Main.npc[leftHand].Center = leftHandCenterPos;
                Main.npc[leftHand].netUpdate = true;
                LeftFistNPC = leftHand;

                int rightHand = NPC.NewNPC((int)npc.Right.X, (int)npc.Right.Y, NPCID.GolemFistRight);
                Main.npc[rightHand].lifeMax = Main.npc[rightHand].life = 1;
                Main.npc[rightHand].dontTakeDamage = true;
                Main.npc[rightHand].noGravity = true;
                Main.npc[rightHand].noTileCollide = false;
                Main.npc[rightHand].ai[0] = npc.whoAmI;
                Main.npc[rightHand].Center = rightHandCenterPos;
                Main.npc[rightHand].netUpdate = true;
                RightFistNPC = rightHand;

                CreateGolemArena(npc);
                AITimer++;

                return false;
            }

            // Aquire a new target if the current one is dead or inactive.
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.TargetClosest(false);

                // If no possible target was found, die.
                if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    npc.velocity.Y += 0.5f;
                    if (npc.timeLeft > 120)
                        npc.timeLeft = 120;

                    if (!npc.WithinRange(Main.player[npc.target].Center, 4200f))
                    {
                        DespawnNPC((int)AttachedHeadNPC);
                        DespawnNPC((int)FreeHeadNPC);
                        DespawnNPC((int)LeftFistNPC);
                        DespawnNPC((int)RightFistNPC);

                        npc.life = 0;
                        npc.active = false;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);

                        DeleteGolemArena();
                    }

                    return false;
                }
                npc.netUpdate = true;
            }
            else
                npc.timeLeft = 7200;

            if (npc.Infernum().arenaRectangle != null)
            {
                Rectangle arena = npc.Infernum().arenaRectangle;

                // 0 is normal. 1 is enraged.
                EnrageState = (!Main.player[npc.target].Hitbox.Intersects(arena)).ToInt();
                npc.TargetClosest(false);
            }

            bool Enraged = EnrageState == 1f;

            ref NPC body = ref npc;
            ref NPC freeHead = ref Main.npc[(int)FreeHeadNPC];
            ref NPC attachedHead = ref Main.npc[(int)AttachedHeadNPC];
            ref NPC leftFist = ref Main.npc[(int)LeftFistNPC];
            ref NPC rightFist = ref Main.npc[(int)RightFistNPC];
            ref Player target = ref Main.player[npc.target];

            // Sync the heads, and end the fight if necessary
            if (!attachedHead.active || !freeHead.active || attachedHead.life <= 0 || freeHead.life <= 0)
            {
                DespawnNPC(attachedHead.whoAmI);
                DespawnNPC(freeHead.whoAmI);
                DespawnNPC(leftFist.whoAmI);
                DespawnNPC(rightFist.whoAmI);

                npc.life = 0;
                npc.HitEffect();
                npc.checkDead();
                npc.NPCLoot();
                npc.active = false;

                DeleteGolemArena();
            }
            else if (ReturnFromEnrageState == 0f)
            {
                // Sync head HP
                if (freeHead.life > attachedHead.life)
                    freeHead.life = attachedHead.life;
                else
                    attachedHead.life = freeHead.life;

                npc.life = attachedHead.life;

                // Sync positions of NPCs
                attachedHead.Center = attachedHeadCenterPos;

                // Only sync free head if it's not in the middle of doing something
                if (freeHead.dontTakeDamage)
                    freeHead.Center = attachedHeadCenterPos;
            }

            freeHead.ai[1]++;
            if (freeHead.ai[1] >= 240f)
                freeHead.ai[1] = 0f;

            attachedHead.ai[1]++;
            if (attachedHead.ai[1] >= 240f)
                attachedHead.ai[1] = 0f;

            float LifeRatio = npc.life / npc.lifeMax;
            npc.dontTakeDamage = true;

            if (AITimer < 60f)
            {
                leftFist.Center = leftHandCenterPos;
                rightFist.Center = rightHandCenterPos;

                if (npc.velocity.Y == 0f && AITimer > 10f)
                {
                    AITimer = 61f;
                    AttackState = (float)GolemAttackState.FistSpin;
                }
                else
                    npc.velocity.Y += 0.5f;

                AITimer++;
                return false;
            }

            if (Enraged)
            {
                // Swap to the free head so that death can ensue
                if (!FreeHead)
                    SwapHeads(npc);

                // Invincibility is lame
                freeHead.defense = 9999;

                // Accelerate the X and Y separately for sporadic movement
                if (freeHead.velocity.Length() < 2)
                    freeHead.velocity = freeHead.DirectionTo(target.Center) * 2f;

                if (Math.Abs((freeHead.Center + freeHead.velocity).X - target.Center.X) > Math.Abs(freeHead.Center.X - target.Center.X))
                    freeHead.velocity.X += freeHead.Center.X > target.Center.X ? -1f : 1f;
                else
                    freeHead.velocity.X *= 1.1f;

                if (Math.Abs((freeHead.Center + freeHead.velocity).Y - target.Center.Y) > Math.Abs(freeHead.Center.Y - target.Center.Y))
                    freeHead.velocity.Y += freeHead.Center.Y > target.Center.Y ? -1f : 1f;
                else
                    freeHead.velocity.Y *= 1.1f;

                freeHead.velocity = freeHead.velocity.ClampMagnitude(0f, 25f);

                // TODO - Spawn projectiles based on velocity path

                // Mark this so that if the player re-enters the arena then the AI will know to resync
                ReturnFromEnrageState = 1f;
                return false;
            }
            else if (ReturnFromEnrageState == 1f)
            {
                // Custom re-attach code rather than the method so that the fight can return to normal faster
                // Slow down for the first part
                // TODO - Use AngleBetween for this
                if (!(freeHead.velocity.ToRotation() < freeHead.DirectionTo(attachedHeadCenterPos).ToRotation() + MathHelper.ToRadians(3) &&
                    freeHead.velocity.ToRotation() > freeHead.DirectionTo(attachedHeadCenterPos).ToRotation() - MathHelper.ToRadians(3)) &&
                    !(freeHead.velocity.ToRotation() < MathHelper.WrapAngle(freeHead.DirectionTo(attachedHeadCenterPos).ToRotation() + MathHelper.Pi) + MathHelper.ToRadians(3) &&
                    freeHead.velocity.ToRotation() > MathHelper.WrapAngle(freeHead.DirectionTo(attachedHeadCenterPos).ToRotation() + MathHelper.Pi) - MathHelper.ToRadians(3)))
                {
                    freeHead.velocity *= 0.925f;
                    // Once stopped, approach the attached position
                    if (freeHead.velocity.Length() < 1)
                        freeHead.velocity = freeHead.DirectionTo(attachedHeadCenterPos);
                    return false;
                }

                // If it will pass, reattach
                // It's fine if the head was unattached before enraging, the attack will continue like normal
                if (attachedHead.Distance(freeHead.Center + freeHead.velocity) > attachedHead.Distance(freeHead.Center))
                {
                    freeHead.defense = freeHead.defDefense;
                    SwapHeads(npc);
                    freeHead.velocity = Vector2.Zero;
                    ReturnFromEnrageState = 0f;
                    return false;
                }

                // Accelerate towards the optimal position
                freeHead.velocity = (freeHead.velocity * 1.085f).ClampMagnitude(0, 20f);
                return false;
            }

            if (AttackCooldown <= 0f)
            {
                switch ((GolemAttackState)AttackState)
                {
                    case GolemAttackState.ArmBullets:
                        if (FreeHead)
                        {
                            ReAttachHead(npc);
                            break;
                        }

                        #region Arm Bullets

                        #endregion
                        
                        break;
                    case GolemAttackState.FistSpin:
                        if (FreeHead)
                        {
                            ReAttachHead(npc);
                            break;
                        }

                        #region Fist Spin

                        if (AttackTimer <= 240f)
                        {
                            // Rotate the fists around the body over the course of 3 seconds, spawning projectiles every so often
                            float rotation = MathHelper.Lerp(0f, MathHelper.TwoPi * 2, AttackTimer / 240f);
                            float distance = 160f;
                            rightFist.Center = body.Center + rotation.ToRotationVector2() * distance;
                            rightFist.rotation = rotation;
                            leftFist.Center = body.Center + MathHelper.WrapAngle(rotation + MathHelper.Pi).ToRotationVector2() * distance;
                            leftFist.rotation = rotation; ;

                            if (AttackTimer % 15f == 0f)
                            {
                                int type = ModContent.ProjectileType<FistBullet>();
                                int bullet = Utilities.NewProjectileBetter(rightFist.Center, Vector2.Zero, type, 280, 0);
                                if (Main.projectile.IndexInRange(bullet))
                                {
                                    Main.projectile[bullet].Infernum().ExtraAI[0] = 0f;
                                    Main.projectile[bullet].rotation = rotation;
                                    Main.projectile[bullet].Infernum().ExtraAI[2] = target.whoAmI;
                                }
                                bullet = Utilities.NewProjectileBetter(leftFist.Center, Vector2.Zero, type, 280, 0);
                                if (Main.projectile.IndexInRange(bullet))
                                {
                                    Main.projectile[bullet].Infernum().ExtraAI[0] = 0f;
                                    Main.projectile[bullet].rotation = MathHelper.WrapAngle(rotation + MathHelper.Pi);
                                    Main.projectile[bullet].Infernum().ExtraAI[2] = target.whoAmI;
                                }
                            }
                        }

                        if (AttackTimer >= 300f)
                        {
                            rightFist.Center = rightHandCenterPos;
                            leftFist.Center = leftHandCenterPos;
                            AttackCooldown = 90f;
                            GoToNextAttack(npc);
                            break;
                        }

                        AttackTimer++;

                        #endregion

                        break;
                    case GolemAttackState.SpikeTrapWaves:
                        if (FreeHead)
                        {
                            ReAttachHead(npc);
                            break;
                        }

                        #region Spiketrap Waves

                        #endregion

                        break;
                    case GolemAttackState.HeatRay:
                        if (!FreeHead)
                            SwapHeads(npc);

                        #region Heat Ray

                        #endregion
                        
                        break;
                    case GolemAttackState.SpinLaser:
                        if (!FreeHead)
                            SwapHeads(npc);

                        #region Spin Laser

                        #endregion

                        break;
                    case GolemAttackState.Slingshot:
                        if (FreeHead)
                        {
                            ReAttachHead(npc);
                            break;
                        }

                        #region Slingshot

                        #endregion

                        break;
                }
            }
            else
            {
                freeHead.velocity *= 0.9f;
                if (freeHead.velocity.Length() < 1f)
                    freeHead.velocity = Vector2.Zero;
                AttackCooldown--;
            }

            AITimer++;
            return false;
        }

        private void ReAttachHead(NPC npc)
        {
            NPC FreeHeadNPC = Main.npc[(int)npc.Infernum().ExtraAI[3]];
            NPC AttachedHeadNPC = Main.npc[(int)npc.Infernum().ExtraAI[2]];

            // If the free head is close enough or it will pass the correct position, reattach it
            if (FreeHeadNPC.Distance(AttachedHeadNPC.Center) < 1f || AttachedHeadNPC.Distance(FreeHeadNPC.Center + FreeHeadNPC.velocity) > AttachedHeadNPC.Distance(FreeHeadNPC.Center))
            {
                SwapHeads(npc);
                return;
            }

            // Otherwise accelerate towards the proper position
            FreeHeadNPC.velocity += FreeHeadNPC.DirectionTo(AttachedHeadNPC.Center) * 0.1f;
        }

        private void GoToNextAttack(NPC npc)
        {
            ref float AttackState = ref npc.ai[1];
            ref float AttackTimer = ref npc.ai[2];

            /* GolemAttackState NextAttack = (GolemAttackState)Main.rand.Next(0, Enum.GetNames(typeof(GolemAttackState)).Length);
            while ((float)NextAttack == AttackState)
                NextAttack = (GolemAttackState)Main.rand.Next(0, Enum.GetNames(typeof(GolemAttackState)).Length); */
            GolemAttackState NextAttack = GolemAttackState.FistSpin;

            AttackState = (float)NextAttack;
            AttackTimer = 0f;
        }

        private void DespawnNPC(int NPCID)
        {
            Main.npc[NPCID].life = 0;
            Main.npc[NPCID].active = false;
            Main.npc[NPCID].netUpdate = true;

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPCID);
        }

        private void SwapHeads(NPC npc)
        {
            ref float AttachedHeadNPC = ref npc.Infernum().ExtraAI[2];
            ref float FreeHeadNPC = ref npc.Infernum().ExtraAI[3];
            ref float FreeHead = ref npc.Infernum().ExtraAI[4];

            bool CurrentlyAttached = !Main.npc[(int)AttachedHeadNPC].dontTakeDamage;

            if (CurrentlyAttached)
            {
                Main.npc[(int)AttachedHeadNPC].dontTakeDamage = true;
                Main.npc[(int)FreeHeadNPC].dontTakeDamage = false;
                FreeHead = 1f;
            }
            else
            {
                Main.npc[(int)AttachedHeadNPC].dontTakeDamage = false;
                Main.npc[(int)FreeHeadNPC].dontTakeDamage = true;
                FreeHead = 0f;
            }
        }

        private void CreateGolemArena(NPC npc)
        {
            DeleteGolemArena();

            if (!Main.player.IndexInRange(npc.target))
                return;

            // TODO - If BR is active, set the area rectangle to null and return

            Player closest = Main.player[npc.target];

            int num = (int)closest.Center.X / 16;
            int num2 = (int)closest.Center.Y / 16;
            int altarX = 0;
            int altarY = 0;
            for (int i = num - 20; i < num + 20; i++)
            {
                for (int j = num2 - 20; j < num2 + 20; j++)
                {
                    if (Main.tile[i, j].active() && Main.tile[i, j].type == TileID.LihzahrdAltar)
                    {
                        altarX = i;
                        altarY = j;
                    }
                }
            }

            int arenaBottom = altarY + 15;
            Vector2 arenaCenter = new Vector2(altarX, arenaBottom - (ArenaHeight / 2) - 5);
            Vector2 arenaArea = new Vector2(ArenaWidth, ArenaHeight);
            npc.Infernum().arenaRectangle = Utils.CenteredRectangle(arenaCenter * 16f, arenaArea * 16f);
            
            int left = (int)(npc.Infernum().arenaRectangle.Center().X / 16 - arenaArea.X * 0.5f);
            int right = (int)(npc.Infernum().arenaRectangle.Center().X / 16 + arenaArea.X * 0.5f);
            int top = (int)(npc.Infernum().arenaRectangle.Center().Y / 16 - arenaArea.Y * 0.5f);
            int bottom = (int)(npc.Infernum().arenaRectangle.Center().Y / 16 + arenaArea.Y * 0.5f);
            int arenaTileType = ModContent.TileType<GolemArena>();
            for (int i = left; i <= right; i++)
            {
                for (int j = top; j <= bottom; j++)
                {
                    if (!WorldGen.InWorld(i, j))
                        continue;

                    // Create arena tiles.
                    if ((i == left || i == right || j == top || j == bottom) && !Main.tile[i, j].active())
                    {
                        Main.tile[i, j].type = (ushort)arenaTileType;
                        Main.tile[i, j].active(true);
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                        else
                            WorldGen.SquareTileFrame(i, j, true);
                    }

                    // Erase old arena tiles.
                    else if (Framing.GetTileSafely(i, j).type == arenaTileType)
                        Main.tile[i, j].active(false);
                }
            }

        }

        private void DeleteGolemArena()
        {
            int surface = (int)Main.worldSurface;
            for (int i = 0; i < Main.maxTilesX; i++)
            {
                for (int j = 0; j < surface; j++)
                {
                    if (Main.tile[i, j] != null)
                    {
                        if (Main.tile[i, j].type == ModContent.TileType<Tiles.GolemArena>())
                        {
                            Main.tile[i, j] = new Tile();
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                            }
                            else
                            {
                                WorldGen.SquareTileFrame(i, j, true);
                            }
                        }
                    }
                }
            }
        }

    }
}
