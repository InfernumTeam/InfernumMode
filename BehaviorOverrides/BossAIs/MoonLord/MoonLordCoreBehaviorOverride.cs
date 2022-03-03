using CalamityMod;
using InfernumMode.BossIntroScreens;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordCoreBehaviorOverride : NPCBehaviorOverride
    {
        public enum MoonLordAttackState
        {
            SpawnEffects = 0,
            DeathEffects = 2,
            PhantasmalSphereHandWaves = 10,
            PhantasmalBoltEyeBursts,
            PhantasmalFlareBursts,
            PhantasmalDeathrays,
            PhantasmalRush,
            PhantasmalDance,
            PhantasmalBarrage
        }

        public const int ArenaWidth = 200;
        public const int ArenaHeight = 150;
        public const float BaseFlySpeedFactor = 6f;
        public static readonly Color OverallTint = new Color(7, 81, 81);

        public static bool IsEnraged
        {
            get
            {
                int moonLordIndex = NPC.FindFirstNPC(NPCID.MoonLordCore);
                if (moonLordIndex < 0)
                    return false;

                NPC moonLord = Main.npc[moonLordIndex];
                Player target = Main.player[moonLord.target];
                Rectangle arena = moonLord.Infernum().arenaRectangle;
                arena.Inflate(-8, -8);
                return !target.Hitbox.Intersects(arena);
            }
        }

        public static int CurrentActiveArms
        {
            get
            {
                int activeArms = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.type == NPCID.MoonLordHand && n.active && n.ai[0] != -2f)
                        activeArms++;
                }
                return activeArms;
            }
        }

        public static bool EyeIsActive
        {
            get
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.type == NPCID.MoonLordHead && n.active && n.ai[0] != -2f)
                        return true;
                }
                return false;
            }
        }

        public override int NPCOverrideType => NPCID.MoonLordCore;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            // Stop rain.
            CalamityMod.CalamityMod.StopRain();

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float wasNotEnraged = ref npc.ai[2];
            ref float forcefullySwitchAttack = ref npc.Infernum().ExtraAI[5];
            ref float deathAttackTimer = ref npc.Infernum().ExtraAI[6];

            // Player variable.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Reset things.
            npc.dontTakeDamage = NPC.CountNPCS(NPCID.MoonLordFreeEye) < 3;

            // Life ratio.
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Start the AI and create the arena.
            if (npc.localAI[3] == 0f)
            {
                Player closest = Main.player[Player.FindClosest(npc.Center, 1, 1)];
                if (npc.Infernum().arenaRectangle == null)
                    npc.Infernum().arenaRectangle = default;

                Point closestTileCoords = closest.Center.ToTileCoordinates();
                npc.Infernum().arenaRectangle = new Rectangle((int)closest.position.X - ArenaWidth * 8, (int)closest.position.Y - ArenaHeight * 8 + 20, ArenaWidth * 16, ArenaHeight * 16);
                for (int i = closestTileCoords.X - ArenaWidth / 2; i <= closestTileCoords.X + ArenaWidth / 2; i++)
                {
                    for (int j = closestTileCoords.Y - ArenaHeight / 2; j <= closestTileCoords.Y + ArenaHeight / 2; j++)
                    {
                        // Create arena tiles.
                        if ((Math.Abs(closestTileCoords.X - i) == ArenaWidth / 2 || Math.Abs(closestTileCoords.Y - j) == ArenaHeight / 2) && !Main.tile[i, j].active())
                        {
                            Main.tile[i, j].type = (ushort)ModContent.TileType<Tiles.MoonlordArena>();
                            Main.tile[i, j].active(true);
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                            else
                                WorldGen.SquareTileFrame(i, j, true);
                        }
                    }
                }
                npc.localAI[3] = 1f;
                attackState = (int)MoonLordAttackState.SpawnEffects;
                npc.netUpdate = true;
            }

            // Define enragement status.
            npc.Calamity().CurrentlyEnraged = IsEnraged;
            if (wasNotEnraged != npc.Calamity().CurrentlyEnraged.ToInt() && IsEnraged)
            {
                for (int i = 92; i < 98; i++)
                {
                    var fuckYou = new Terraria.Audio.LegacySoundStyle(SoundID.Zombie, i);
                    var roar = Main.PlaySound(fuckYou, target.Center);
                    if (roar != null)
                    {
                        roar.Volume = MathHelper.Clamp(roar.Volume * 1.85f, 0f, 1f);
                        roar.Pitch = 0.35f;
                    }
                }
            }
            wasNotEnraged = npc.Calamity().CurrentlyEnraged.ToInt();

            MoonLordAttackState currentAttack = (MoonLordAttackState)(int)attackState;
            switch (currentAttack)
            {
                case MoonLordAttackState.SpawnEffects:
                    DoBehavior_SpawnEffects(npc, ref attackTimer);
                    break;
                case MoonLordAttackState.DeathEffects:
                    DoBehavior_DeathEffects(npc, ref deathAttackTimer);
                    break;
                default:
                    if ((currentAttack == MoonLordAttackState.PhantasmalFlareBursts ||
                        currentAttack == MoonLordAttackState.PhantasmalSphereHandWaves) && CurrentActiveArms <= 0)
                    {
                        SelectNextAttack(npc);
                    }

                    DoBehavior_IdleHover(npc, target, ref attackTimer);
                    break;
            }
            attackTimer++;

            // Forcefully switch attacks if the mechanism variable for it is activated.
            // This is intended to be used by the arms/head directly and not inside in-class behavior states.
            if (forcefullySwitchAttack == 1f)
            {
                SelectNextAttack(npc);
                forcefullySwitchAttack = 0f;
            }
            
            // Update other limbs if the core is supposed to sync.
            if (npc.netUpdate)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    bool isBodyPart = n.type == NPCID.MoonLordHand || n.type == NPCID.MoonLordHead || n.type == NPCID.MoonLordFreeEye;
                    if (n.active && n.ai[3] == npc.whoAmI && isBodyPart)
                    {
                        n.netSpam = npc.netSpam;
                        n.netUpdate = true;
                    }
                }
            }

            return false;
        }

        public static void DoBehavior_SpawnEffects(NPC npc, ref float attackTimer)
        {
            // Don't do damage during spawn effects.
            npc.dontTakeDamage = true;

            // Roar after a bit of time has passed.
            if (attackTimer == 30f)
                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 92, 1f, 0f);

            if (Main.netMode != NetmodeID.Server && !IntroScreenManager.ScreenIsObstructed)
            {
                attackTimer = 30000f;
                npc.netUpdate = true;
            }

            // Create arms/head and go to the next attack state.
            if (attackTimer >= 30000f)
            {
                SelectNextAttack(npc);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int[] bodyPartIndices = new int[3];
                    for (int i = 0; i < 2; i++)
                    {
                        int handIndex = NPC.NewNPC((int)npc.Center.X + i * 800 - 400, (int)npc.Center.Y - 100, NPCID.MoonLordHand, npc.whoAmI);
                        Main.npc[handIndex].ai[2] = i;
                        Main.npc[handIndex].netUpdate = true;
                        bodyPartIndices[i] = handIndex;
                    }

                    int headIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y - 400, NPCID.MoonLordHead, npc.whoAmI);
                    Main.npc[headIndex].netUpdate = true;
                    bodyPartIndices[2] = headIndex;

                    // Mark the owner of the body parts.
                    for (int i = 0; i < 3; i++)
                        Main.npc[bodyPartIndices[i]].ai[3] = npc.whoAmI;

                    for (int i = 0; i < 3; i++)
                        npc.localAI[i] = bodyPartIndices[i];

                    // Reset hand AIs.
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].type == NPCID.MoonLordHand && Main.npc[i].active)
                        {
                            Main.npc[i].ai[0] = 0f;
                            Main.npc[i].netUpdate = true;
                        }
                    }
                }
            }
            npc.netSpam = 0;
            npc.netUpdate = true;
        }

        public static void DoBehavior_DeathEffects(NPC npc, ref float attackTimer)
        {
            npc.Calamity().ShouldCloseHPBar = true;
            npc.life = 1;
            npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 0.4f, 0.35f);
            npc.dontTakeDamage = true;

            // Create a flash before anything else.
            if (attackTimer < 60f)
            {
                if (attackTimer == 4f)
                    Main.PlaySound(SoundID.NPCDeath61, npc.Center);
                MoonlordDeathDrama.RequestLight(attackTimer / 60f, npc.Center);
            }
            else
            {
                if (attackTimer == 61f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int deathAnimation = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<MoonLordDeathAnimationHandler>(), 0, 0f);
                        if (Main.projectile.IndexInRange(deathAnimation))
                            Main.projectile[deathAnimation].ai[0] = npc.whoAmI;
                    }
                }

                // Create explosions periodically.
                float explosionCreationRate = MathHelper.Lerp(0.075f, 0.24f, Utils.InverseLerp(75f, 300f, attackTimer, true));
                if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextFloat() < explosionCreationRate)
                {
                    Vector2 explosionSpawnPosition = npc.Center + Main.rand.NextVector2Circular(200f, 450f);
                    Utilities.NewProjectileBetter(explosionSpawnPosition, Vector2.Zero, ModContent.ProjectileType<MoonLordDeathExplosion>(), 0, 0f);
                }
                MoonlordDeathDrama.RequestLight(Utils.InverseLerp(480f, 530f, attackTimer, true) * 8f, npc.Center);
            }

            if (attackTimer >= 550f)
            {
                // Release a bunch of blood particles everywhere.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        Vector2 bloodSpawnPosition = npc.Center + Main.rand.NextVector2Circular(120f, 250f);
                        Vector2 bloodShootVelocity = -Vector2.UnitY.RotatedByRandom(1.4f) * Main.rand.NextFloat(6f, 23f);
                        Utilities.NewProjectileBetter(bloodSpawnPosition, bloodShootVelocity, ModContent.ProjectileType<MoonLordDeathBloodBlob>(), 0, 0f);
                    }
                }

                MoonlordDeathDrama.ThrowPieces(npc.Center, npc.whoAmI);
                npc.life = 0;
                npc.NPCLoot();
                npc.active = false;
            }                

            attackTimer++;
        }

        public static void DoBehavior_IdleHover(NPC npc, Player target, ref float attackTimer)
        {
            float verticalOffset = MathHelper.Lerp(0f, 45f, (float)Math.Cos(attackTimer / 32f) * 0.5f + 0.5f);
            Vector2 hoverDestination = target.Center - Vector2.UnitY * verticalOffset;
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * BaseFlySpeedFactor;
            npc.SimpleFlyMovement(idealVelocity, BaseFlySpeedFactor / 20f);
            npc.velocity = npc.velocity.MoveTowards(idealVelocity, BaseFlySpeedFactor / 60f);
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[1] = 0f;

            int eyeCount = NPC.CountNPCS(NPCID.MoonLordFreeEye);
            MoonLordAttackState[] attackCycle = new MoonLordAttackState[]
            {
                !EyeIsActive && eyeCount >= 2 ? MoonLordAttackState.PhantasmalDance :MoonLordAttackState.PhantasmalBoltEyeBursts,
                MoonLordAttackState.PhantasmalSphereHandWaves,
                MoonLordAttackState.PhantasmalFlareBursts,
                !EyeIsActive && eyeCount >= 2 ? MoonLordAttackState.PhantasmalRush : MoonLordAttackState.PhantasmalDeathrays,
            };
            if (CurrentActiveArms <= 0 && npc.ai[0] != (int)MoonLordAttackState.SpawnEffects)
            {
                attackCycle = new MoonLordAttackState[]
                {
                    MoonLordAttackState.PhantasmalDance,
                    MoonLordAttackState.PhantasmalBoltEyeBursts,
                    MoonLordAttackState.PhantasmalDeathrays,
                    MoonLordAttackState.PhantasmalRush,
                };
            }
            if (eyeCount >= 3)
            {
                attackCycle = new MoonLordAttackState[]
                {
                    MoonLordAttackState.PhantasmalDance,
                    MoonLordAttackState.PhantasmalBoltEyeBursts,
                    MoonLordAttackState.PhantasmalRush,
                    MoonLordAttackState.PhantasmalBarrage,
                };
            }

            npc.ai[0] = (int)attackCycle[(int)npc.Infernum().ExtraAI[7] % attackCycle.Length];
            npc.Infernum().ExtraAI[7]++;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                bool isBodyPart = n.type == NPCID.MoonLordHand || n.type == NPCID.MoonLordHead || n.type == NPCID.MoonLordFreeEye;
                if (n.active && n.ai[3] == npc.whoAmI && isBodyPart)
                {
                    for (int j = 0; j < 5; j++)
                        n.Infernum().ExtraAI[i] = 0f;
                }
            }

            npc.netUpdate = true;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D coreTexture = Main.npcTexture[npc.type];
            Texture2D coreOutlineTexture = Main.extraTexture[16];
            Texture2D forearmTexture = Main.extraTexture[14];
            Texture2D bodyTexture = Main.extraTexture[13];
            Vector2 leftHalfOrigin = new Vector2(bodyTexture.Width, 278f);
            Vector2 rightHalfOrigin = new Vector2(0f, 278f);
            Vector2 center = npc.Center;
            Point coreTileCoords = (npc.Center + new Vector2(0f, -150f)).ToTileCoordinates();
            Color color = npc.GetAlpha(Color.Lerp(Lighting.GetColor(coreTileCoords.X, coreTileCoords.Y), Color.White, 0.3f));
            for (int a = 0; a < 2; a++)
            {
                int armIndex = -1;
                bool leftArm = a == 0;
                Vector2 directionThing = new Vector2((!leftArm).ToDirectionInt(), 1f);
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == NPCID.MoonLordHand && Main.npc[i].ai[2] == a && Main.npc[i].ai[3] == npc.whoAmI)
                    {
                        armIndex = i;
                        break;
                    }
                }
                if (armIndex != -1)
                {
                    Vector2 shoulderPosition = center + new Vector2(220f, -60f) * directionThing;
                    Vector2 shoulderOffset = (Main.npc[armIndex].Center + new Vector2(0f, 76f) - shoulderPosition) * 0.5f;
                    float rotationalOffset = (float)Math.Acos(MathHelper.Clamp(shoulderOffset.Length() / 340f, 0f, 1f)) * -directionThing.X;
                    float forearmRotation = shoulderOffset.ToRotation() - rotationalOffset - MathHelper.PiOver2;
                    SpriteEffects direction = (!leftArm) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    Vector2 forearmOrigin = new Vector2(76f, 66f);
                    if (!leftArm)
                        forearmOrigin.X = forearmTexture.Width - forearmOrigin.X;

                    spriteBatch.Draw(forearmTexture, shoulderPosition - Main.screenPosition, null, color, forearmRotation, forearmOrigin, 1f, direction, 0f);
                }
            }
            spriteBatch.Draw(bodyTexture, center - Main.screenPosition, null, color, 0f, leftHalfOrigin, 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(bodyTexture, center - Main.screenPosition, null, color, 0f, rightHalfOrigin, 1f, SpriteEffects.FlipHorizontally, 0f);
            spriteBatch.Draw(coreOutlineTexture, center - Main.screenPosition, null, color, 0f, new Vector2(112f, 101f), 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(coreTexture, center - Main.screenPosition, npc.frame, color, 0f, npc.frame.Size() / 2f, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
