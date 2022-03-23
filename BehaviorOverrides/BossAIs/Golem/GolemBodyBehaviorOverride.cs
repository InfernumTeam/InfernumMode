using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using InfernumMode.Tiles;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public enum GolemAttackState
    {
        ArmBullets,
        FistSpin,
        SpikeTrapWaves,
        HeatRay,
        SpinLaser,
        Slingshot,

        LandingState,
        SummonDelay,
        BIGSHOT,
        BadTime,
    }

    public class GolemBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Golem;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public static int ArenaWidth = 115;
        public static int ArenaHeight = 105;
        public const float ConstAttackCooldown = 90f;
        public const int AttacksNotToPool = 4; // The last X states in GolemAttackState should not be selected as attacks during the fight

        public override bool PreAI(NPC npc)
        {
            ref float AITimer = ref npc.ai[0];
            ref float AttackState = ref npc.ai[1];
            ref float AttackTimer = ref npc.ai[2];
            ref float FightStarted = ref npc.ai[3];

            ref float LeftFistNPC = ref npc.Infernum().ExtraAI[0];
            ref float RightFistNPC = ref npc.Infernum().ExtraAI[1];
            ref float AttachedHeadNPC = ref npc.Infernum().ExtraAI[2];
            ref float FreeHeadNPC = ref npc.Infernum().ExtraAI[3];
            ref float HeadState = ref npc.Infernum().ExtraAI[4];
            ref float EnrageState = ref npc.Infernum().ExtraAI[5];
            ref float ReturnFromEnrageState = ref npc.Infernum().ExtraAI[6];
            ref float AttackCooldown = ref npc.Infernum().ExtraAI[7];
            ref float PreviousAttackState = ref npc.Infernum().ExtraAI[8];
            // ref float DarknessRatio = ref npc.Infernum().ExtraAI[9]; (later in code)
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
                npc.Opacity = 0f;
                AttackState = Utilities.IsAprilFirst() ? (Main.rand.NextBool() ? (float)GolemAttackState.BIGSHOT : (float)GolemAttackState.BadTime) : (float)GolemAttackState.SummonDelay;
                PreviousAttackState = (float)GolemAttackState.LandingState;
                CreateGolemArena(npc);
                leftHandCenterPos = new Vector2(npc.Left.X, npc.Left.Y);
                rightHandCenterPos = new Vector2(npc.Right.X, npc.Right.Y);
                attachedHeadCenterPos = new Vector2(npc.Center.X, npc.Top.Y);
                npc.netUpdate = true;

                // Set the whoAmI variable.
                //NPC.golemBoss = npc.whoAmI;

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

                int leftHand = NPC.NewNPC((int)leftHandCenterPos.X, (int)leftHandCenterPos.Y, ModContent.NPCType<GolemFistLeft>());
                Main.npc[leftHand].ai[0] = npc.whoAmI;
                Main.npc[leftHand].netUpdate = true;
                LeftFistNPC = leftHand;

                int rightHand = NPC.NewNPC((int)rightHandCenterPos.X, (int)rightHandCenterPos.Y, ModContent.NPCType<GolemFistRight>());
                Main.npc[rightHand].ai[0] = npc.whoAmI;
                Main.npc[rightHand].netUpdate = true;
                RightFistNPC = rightHand;

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
                npc.netUpdate = true;

                DeleteGolemArena();
                return false;
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

            if (!rightFist.active)
                rightFist.active = true;

            if (!leftFist.active)
                leftFist.active = true;

            freeHead.ai[1]++;
            if (freeHead.ai[1] >= 240f)
                freeHead.ai[1] = 0f;

            attachedHead.ai[1]++;
            if (attachedHead.ai[1] >= 240f)
                attachedHead.ai[1] = 0f;

            float LifeRatio = npc.life / npc.lifeMax;
            npc.dontTakeDamage = true;

            if (FightStarted == 0f)
            {
                if (npc.Bottom.Y > npc.Infernum().arenaRectangle.Bottom)
                {
                    npc.Bottom = new Vector2(npc.Center.X, npc.Infernum().arenaRectangle.Bottom);
                    npc.velocity = Vector2.Zero;
                }

                leftFist.Center = leftHandCenterPos;
                rightFist.Center = rightHandCenterPos;

                // Fade the screen to black
                // Starting by setting opacities to 0 and having golem fall invisibly
                if (AITimer == 1f || (AITimer > 1f && npc.velocity.Y != 0f))
                {
                    npc.Opacity = 0f;
                    leftFist.Opacity = 0f;
                    rightFist.Opacity = 0f;
                    freeHead.Opacity = 0f;
                    attachedHead.Opacity = 0f;
                    npc.damage = 0;
                    leftFist.damage = 0;
                    rightFist.damage = 0;
                    freeHead.damage = 0;
                    attachedHead.damage = 0;
                    attachedHead.dontTakeDamage = true;

                    npc.velocity.Y += 0.5f;
                    AITimer++;
                    return false;
                }

                // Start the epic once golem lands
                else if (PreviousAttackState == (float)GolemAttackState.LandingState)
                {
                    PreviousAttackState = AttackState;
                    AITimer = 2f;
                }

                ref float DarknessRatio = ref npc.Infernum().ExtraAI[9];
                float Timer = AITimer - 2f;

                // Fade in for the first 60 frames
                // Hold black for the second 60 frames
                if (Timer < 180f)
                    DarknessRatio = MathHelper.Clamp(Timer / 60f, 0f, 1f);

                // Fade out for the last 60 frames
                else if (Timer < 240f)
                {
                    DarknessRatio = 1f - MathHelper.Clamp((Timer - 180f) / 60f, 0f, 1f);
                    npc.Opacity = 1f;
                    freeHead.Opacity = 1f;
                    leftFist.Opacity = 1f;
                    rightFist.Opacity = 1f;
                    attachedHead.Opacity = 1f;
                }
                else
                {
                    DarknessRatio = 0f;
                    AITimer++;
                    FightStarted = 1f;
                    AttackCooldown = ConstAttackCooldown;
                    PreviousAttackState = (float)GolemAttackState.SummonDelay;
                    AttackState = (float)GolemAttackState.FistSpin;
                    npc.damage = npc.defDamage;
                    leftFist.damage = leftFist.defDamage;
                    rightFist.damage = rightFist.defDamage;
                    freeHead.damage = freeHead.defDamage;
                    attachedHead.damage = attachedHead.defDamage;
                    attachedHead.dontTakeDamage = false;
                    return false;
                }

                // Play the sound
                if (Timer == 120f)
                {
                    if (AttackState == (float)GolemAttackState.BadTime)
                        Main.PlaySound(SoundLoader.customSoundType, -1, -1, InfernumMode.Instance.GetSoundSlot(SoundType.Custom, "Sounds/Custom/BadTime"));
                    else
                        Main.PlaySound(SoundLoader.customSoundType, -1, -1, InfernumMode.Instance.GetSoundSlot(SoundType.Custom, "Sounds/Custom/[BIG SHOT]"));
                }

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
                if (attachedHead.Distance(freeHead.Center + freeHead.velocity) > attachedHead.Distance(freeHead.Center) && attachedHead.Distance(freeHead.Center) < 20f)
                {
                    freeHead.defense = freeHead.defDefense;
                    SwapHeads(npc);
                    freeHead.velocity = Vector2.Zero;
                    ReturnFromEnrageState = 0f;
                    return false;
                }
                else if (attachedHead.Distance(freeHead.Center + freeHead.velocity) > attachedHead.Distance(freeHead.Center))
                {
                    freeHead.velocity *= 0.925f;
                    // Once stopped, approach the attached position
                    if (freeHead.velocity.Length() < 1)
                        freeHead.velocity = freeHead.DirectionTo(attachedHeadCenterPos);
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
                            float distance = 145f;
                            rightFist.Center = rightHandCenterPos + rotation.ToRotationVector2() * distance;
                            rightFist.rotation = rotation;
                            leftFist.Center = leftHandCenterPos + MathHelper.WrapAngle(rotation + MathHelper.Pi).ToRotationVector2() * distance;
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

                        if (AttackTimer > 240f)
                        {
                            AttackCooldown = ConstAttackCooldown;
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
                // Attack swapping
                freeHead.velocity *= 0.9f;
                if (freeHead.velocity.Length() < 0.25f)
                    freeHead.velocity = Vector2.Zero;

                float lerp = Utils.InverseLerp(ConstAttackCooldown, 0f, AttackCooldown, true);
                if (lerp <= 0.5f)
                {
                    lerp *= 2f;
                    if (PreviousAttackState == (float)GolemAttackState.FistSpin)
                    {
                        lerp = lerp >= 0.5f ? (float)Math.Sqrt(0.25D - Math.Pow(lerp - 1D, 2)) + 0.5f : (float)-Math.Sqrt(0.25D - Math.Pow(lerp, 2)) + 0.5f;
                        float distance = MathHelper.Lerp(160f, 0f, lerp);
                        rightFist.Center = rightHandCenterPos + Vector2.UnitX * distance;
                        leftFist.Center = leftHandCenterPos - Vector2.UnitX * distance;
                    }
                }
                else
                {
                    lerp = lerp * 2f - 1f;
                    if (AttackState == (float)GolemAttackState.FistSpin)
                    {
                        lerp = lerp >= 0.5f ? (float)Math.Sqrt(0.25D - Math.Pow(lerp - 1D, 2)) + 0.5f : (float)-Math.Sqrt(0.25D - Math.Pow(lerp, 2)) + 0.5f;
                        float distance = MathHelper.Lerp(0f, 160f, lerp);
                        rightFist.Center = rightHandCenterPos + Vector2.UnitX * distance;
                        leftFist.Center = leftHandCenterPos - Vector2.UnitX * distance;
                    }
                }

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
            ref float PreviousAttackState = ref npc.Infernum().ExtraAI[8];

            /*GolemAttackState NextAttack = (GolemAttackState)Main.rand.Next(0, Enum.GetNames(typeof(GolemAttackState)).Length - AttacksNotToPool);
            while ((float)NextAttack == AttackState)
                NextAttack = (GolemAttackState)Main.rand.Next(0, Enum.GetNames(typeof(GolemAttackState)).Length - AttacksNotToPool);*/
            GolemAttackState NextAttack = GolemAttackState.FistSpin;

            PreviousAttackState = AttackState;
            AttackState = (float)NextAttack;
            AttackTimer = 0f;
        }

        public static void DespawnNPC(int NPCID)
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

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Golem/GolemBody");
            Texture2D glowMask = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Golem/BodyGlow");
            Rectangle rect = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 drawPos = npc.Center - Main.screenPosition;
            drawPos += new Vector2(4, -12);
            Main.spriteBatch.Draw(texture, drawPos, rect, lightColor * npc.Opacity, npc.rotation, rect.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowMask, drawPos, rect, Color.White * npc.Opacity, npc.rotation, rect.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            return false;
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
            npc.Center = npc.Infernum().arenaRectangle.Center.ToVector2();

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
