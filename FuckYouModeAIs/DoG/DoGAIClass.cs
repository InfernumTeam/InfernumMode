using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.DoG
{
	public class DoGAIClass
    {
        internal const int cvDarkMatterDamage = 325;
        internal const int cvPortalDamage = 360;
        internal const int cvDarkBeamDamage = 700;
        internal const int cvExplosionDamage = 900;
        internal const int signusScytheDamage = 340;
        internal const int TotalTimeSpentInPhase = 750;
        internal const float velocityConstant = 10.35f;
        internal const float resolutionConstant = 1049088f;
        
        [OverrideAppliesTo("DevourerofGodsHead", typeof(DoGAIClass), "DoGP1Head", EntityOverrideContext.NPCAI)]
        public static bool DoGP1Head(NPC npc)
        {
            // npc.Infernum().ExtraAI[0] = Phase Switch Timer
            // npc.Infernum().ExtraAI[1] = 60% Life Boolean
            // npc.Infernum().ExtraAI[2] = Tail spawned boolean
            // npc.Infernum().ExtraAI[3] = Fire burst timer
            // npc.Infernum().ExtraAI[4] = Sentinel attack copy counter
            // npc.Infernum().ExtraAI[5] = Idle Timer
            // npc.Infernum().ExtraAI[6] = Fly Acceleration
            // npc.Infernum().ExtraAI[7] = Jaw Angle
            // npc.Infernum().ExtraAI[8] = Chomp time
            // npc.Infernum().ExtraAI[9] = Sentinel summon index
            // npc.Infernum().ExtraAI[10] = Death portal state
            // npc.Infernum().ExtraAI[11] = Death portal projectile index

            Main.player[npc.target].Calamity().normalityRelocator = false;
            Main.player[npc.target].Calamity().spectralVeil = false;

            ref float time = ref npc.Infernum().ExtraAI[5];
            ref float flyAcceleration = ref npc.Infernum().ExtraAI[6];
            ref float jawAngle = ref npc.Infernum().ExtraAI[7];
            ref float chompTime = ref npc.Infernum().ExtraAI[8];

            // Timer effect.
            time++;

            npc.Calamity().DR = 0.15f;

            // whoAmI variable
            CalamityGlobalNPC.DoGHead = npc.whoAmI;

            // Stop rain
            if (Main.raining)
                Main.raining = false;

            if (npc.Infernum().ExtraAI[10] > 0f)
            {
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), 28f, 0.065f);
                if (npc.Infernum().ExtraAI[10] == 1f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 spawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.UnitX) * 1600f;
                        npc.Infernum().ExtraAI[11] = Projectile.NewProjectile(spawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                        Main.projectile[(int)npc.Infernum().ExtraAI[11]].localAI[0] = 1f;
                    }

                    int headType = ModContent.NPCType<DevourerofGodsHead>();
                    int bodyType = ModContent.NPCType<DevourerofGodsBody>();
                    int tailType = ModContent.NPCType<DevourerofGodsTail>();

                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active && (Main.npc[i].type == headType || Main.npc[i].type == bodyType || Main.npc[i].type == tailType))
                        {
                            Main.npc[i].Opacity = 1f;
                        }
                    }

                    npc.Opacity = 1f;
                    npc.Infernum().ExtraAI[10] = 2f;
                }

                if (Main.projectile[(int)npc.Infernum().ExtraAI[11]].Hitbox.Intersects(npc.Hitbox))
                    npc.alpha = Utils.Clamp(npc.alpha + 140, 0, 255);
                return false;
            }

            npc.dontTakeDamage = false;
            // Fade effects.
            if (NPC.AnyNPCs(ModContent.NPCType<CeaselessVoid>()) ||
                NPC.AnyNPCs(ModContent.NPCType<StormWeaverHeadNaked>()) ||
                NPC.AnyNPCs(ModContent.NPCType<CalamityMod.NPCs.Signus.Signus>()))
            {
                npc.dontTakeDamage = true;
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 0f, 0.25f);
            }
            else
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 1f, 0.25f);

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Light
            Lighting.AddLight((int)((npc.position.X + npc.width / 2) / 16f), (int)((npc.position.Y + npc.height / 2) / 16f), 0.2f, 0.05f, 0.2f);

            // Worm variable
            if (npc.ai[3] > 0f)
                npc.realLife = (int)npc.ai[3];

            // Target
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead)
                npc.TargetClosest(true);

            // Edgy boss text
            if (lifeRatio < 0.4f)
            {
                if (npc.Infernum().ExtraAI[1] == 0f)
                {
                    string key = "Mods.CalamityMod.EdgyBossText";
                    Color messageColor = Color.Cyan;
                    if (Main.netMode == NetmodeID.SinglePlayer)
                        Main.NewText(Language.GetTextValue(key), messageColor);
                    else if (Main.netMode == NetmodeID.Server)
                        NetMessage.BroadcastChatMessage(NetworkText.FromKey(key), messageColor);

                    npc.Infernum().ExtraAI[1] = 1f;
                }
            }

            npc.damage = npc.dontTakeDamage ? 0 : 4998;
            // Spawn segments
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (npc.Infernum().ExtraAI[2] == 0f && npc.ai[0] == 0f)
                {
                    int Previous = npc.whoAmI;
                    const int minLength = 80;
                    const int maxLength = 81;
                    for (int segmentSpawn = 0; segmentSpawn < maxLength; segmentSpawn++)
                    {
                        int segment;
                        if (segmentSpawn >= 0 && segmentSpawn < minLength)
                            segment = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), InfernumMode.CalamityMod.NPCType("DevourerofGodsBody"), npc.whoAmI);
                        else
                            segment = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), InfernumMode.CalamityMod.NPCType("DevourerofGodsTail"), npc.whoAmI);

                        Main.npc[segment].realLife = npc.whoAmI;
                        Main.npc[segment].ai[2] = npc.whoAmI;
                        Main.npc[segment].ai[1] = Previous;
                        Main.npc[Previous].ai[0] = segment;
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, segment, 0f, 0f, 0f, 0);
                        Previous = segment;
                    }
                    npc.Infernum().ExtraAI[2] = 1f;
                }
            }

            // Perform sentinel attacks as necessary.
            DoPhase1SentinelAttacks(npc);

            // Chomping after attempting to eat the player.
            bool chomping = npc.dontTakeDamage ? false : DoChomp(npc, ref chompTime, ref jawAngle);

            // Despawn.
            if (Main.player[npc.target].dead)
            {
                npc.velocity.Y -= 0.8f;
                if (npc.position.Y < Main.topWorld + 16f)
                    npc.velocity.Y -= 1f;

                if ((double)npc.position.Y < Main.topWorld + 16f)
                {
                    for (int a = 0; a < Main.maxNPCs; a++)
                    {
                        if (Main.npc[a].type == InfernumMode.CalamityMod.NPCType("DevourerofGodsHead") ||
                            Main.npc[a].type == InfernumMode.CalamityMod.NPCType("DevourerofGodsBody") ||
                            Main.npc[a].type == InfernumMode.CalamityMod.NPCType("DevourerofGodsTail"))
                            Main.npc[a].active = false;
                    }
                }
            }
            else
                DoAggressiveFlyMovement(npc, chomping, ref jawAngle, ref chompTime, ref time, ref flyAcceleration);

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (lifeRatio < 0.6f && npc.alpha <= 0)
            {
                npc.Infernum().ExtraAI[3] += 1f;
                if (npc.Infernum().ExtraAI[3] >= 600 &&
                    npc.Infernum().ExtraAI[3] % 60 == 0f &&
                    Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(npc.Center, npc.velocity.SafeNormalize(Vector2.UnitY) * 16f, ModContent.ProjectileType<HomingDoGBurst>(), 81, 0f, Main.myPlayer, 0f, 0f);
                    }
                }
                if (npc.Infernum().ExtraAI[3] > 720)
                {
                    npc.Infernum().ExtraAI[3] = 0f;
                }
            }
            return false;
        }

        public static void DoPhase1SentinelAttacks(NPC npc)
        {
            // Ceasless Void Effect (Augmented Laser Portal)
            if (npc.Infernum().ExtraAI[4] > 0f &&
                npc.Infernum().ExtraAI[4] <= 900f &&
                npc.alpha <= 0)
            {
                if (npc.Infernum().ExtraAI[4] % 210 == 0f)
                {
                    Vector2 spawnPosition = new Vector2(Main.rand.NextFloat(600f, 1200f) * Main.rand.NextBool().ToDirectionInt(),
                                                        Main.rand.NextFloat(700, 1100f) * Main.rand.NextBool().ToDirectionInt());
                    Projectile.NewProjectile(Main.player[npc.target].Center +
                        spawnPosition,
                        Vector2.Zero,
                        ModContent.ProjectileType<DoGBeamPortalN>(),
                        0, 0f);
                }
            }
            // Storm Weaver Effect (Lightning Barrage)
            if (npc.Infernum().ExtraAI[4] > 900f &&
                npc.Infernum().ExtraAI[4] <= 900f * 2f &&
                npc.alpha <= 0)
            {
                if (npc.Infernum().ExtraAI[4] <= 900 + 60 * 4)
                {
                    // Aimed at player
                    if (npc.Infernum().ExtraAI[4] % 60 == 0f &&
                        npc.Infernum().ExtraAI[4] < 900 + 60 * 2)
                    {
                        Projectile.NewProjectile(Main.player[npc.target].Center +
                            new Vector2(Main.rand.NextFloat(-290f, 290f), -1200f),
                            Vector2.Zero,
                            ModContent.ProjectileType<Lightning>(),
                            85, 0f, npc.target);
                    }
                    // Random arcs
                    else if (npc.Infernum().ExtraAI[4] % 60 == 0f &&
                             npc.Infernum().ExtraAI[4] > 900 + 60 * 2)
                    {
                        Projectile.NewProjectile(Main.player[npc.target].Center +
                            new Vector2(Main.rand.NextFloat(-290f, 290f), -1200f),
                            Vector2.Zero,
                            ModContent.ProjectileType<Lightning>(),
                            85, 0f, npc.target, 1f);
                    }
                }
            }
            // Signus Effect (Cosmic Minefield)
            if (npc.Infernum().ExtraAI[4] > 900f * 2f &&
                npc.Infernum().ExtraAI[4] <= 900f * 3f &&
                npc.alpha <= 0)
            {
                if (npc.Infernum().ExtraAI[4] % 160 == 0f)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 spawnOffset = new Vector2(Main.rand.NextFloat(400f, 900f), Main.rand.NextFloat(400f, 900f));
                        spawnOffset.X *= Main.rand.NextBool().ToDirectionInt();
                        spawnOffset.Y *= Main.rand.NextBool().ToDirectionInt();
                        Projectile.NewProjectile(Main.player[npc.target].Center + spawnOffset, Vector2.Zero, ModContent.ProjectileType<DoGMine>(), 0, 0f, npc.target, 1f);
                    }
                }
            }
        }

        public static bool DoChomp(NPC npc, ref float chompTime, ref float jawAngle)
        {
            bool chomping = chompTime > 0f;
            float idealChompAngle = MathHelper.ToRadians(-9f);
            if (chomping)
            {
                chompTime--;

                if (jawAngle != idealChompAngle)
                {
                    jawAngle = jawAngle.AngleTowards(idealChompAngle, 0.12f);

                    if (Math.Abs(jawAngle - idealChompAngle) < 0.001f)
                    {
                        for (int i = 0; i < 26; i++)
                        {
                            Dust electricity = Dust.NewDustPerfect(npc.Center - Vector2.UnitY.RotatedBy(npc.rotation) * 52f, 229);
                            electricity.velocity = ((MathHelper.TwoPi / 26f * i).ToRotationVector2() * new Vector2(7f, 4f)).RotatedBy(npc.rotation) + npc.velocity * 1.5f;
                            electricity.noGravity = true;
                            electricity.scale = 1.8f;
                        }
                        jawAngle = idealChompAngle;
                    }
                }
            }
            return chomping;
        }

        public static void DoAggressiveFlyMovement(NPC npc, bool chomping, ref float jawAngle, ref float chompTime, ref float time, ref float flyAcceleration)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float idealFlyAcceleration = MathHelper.Lerp(0.028f, 0.016f, lifeRatio);
            float idealFlySpeed = MathHelper.Lerp(12.9f, 7f, lifeRatio);
            float idealMouthOpeningAngle = MathHelper.ToRadians(34f);
            Vector2 destination = Main.player[npc.target].Center;

            float distanceFromDestination = npc.Distance(destination);
            if (distanceFromDestination > 650f)
            {
                destination += (time % 60f / 60f * MathHelper.TwoPi).ToRotationVector2() * 120f;
                distanceFromDestination = npc.Distance(destination);
                idealFlyAcceleration *= 2.5f;
            }

            // Charge if the player is far away.
            // Don't do this at the start of the fight though. Doing so might lead to an unfair
            // charge.
            if (distanceFromDestination > 2700f && time > 120f)
            {
                idealFlyAcceleration = MathHelper.Min(6f, flyAcceleration + 1f);
                idealFlySpeed = 37f;
            }

            idealFlyAcceleration = MathHelper.SmoothStep(idealFlyAcceleration, 1.85f, Utils.InverseLerp(600f, 200f, distanceFromDestination, true));
            flyAcceleration = MathHelper.Lerp(flyAcceleration, idealFlyAcceleration, 0.3f);

            float directionToPlayerOrthogonality = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.DirectionTo(destination));
            if (distanceFromDestination > 100f)
            {
                float speed = npc.velocity.Length();
                if (speed < 15f)
                    speed += 0.08f;

                if (speed > 21f)
                    speed -= 0.08f;

                if (directionToPlayerOrthogonality < 0.85f && directionToPlayerOrthogonality > 0.5f)
                    speed += 0.24f;

                if (directionToPlayerOrthogonality < 0.5f && directionToPlayerOrthogonality > -0.7f)
                    speed -= 0.1f;

                speed = MathHelper.Clamp(speed, 10f, 27f);

                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), flyAcceleration, true) * speed;
            }

            // Jaw opening when near player.
            if (!chomping)
            {
                if ((npc.Distance(Main.player[npc.target].Center) < 230f && directionToPlayerOrthogonality > 0.67f) ||
                    (npc.Distance(Main.player[npc.target].Center) < 450f && directionToPlayerOrthogonality > 0.92f))
                {
                    jawAngle = jawAngle.AngleTowards(idealMouthOpeningAngle, 0.028f);
                    if (distanceFromDestination * 0.5f < 56f)
                    {
                        if (chompTime == 0f)
                        {
                            chompTime = 18f;
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.NPCHit, "Sounds/NPCHit/OtherworldlyHit"), npc.Center);
                        }
                    }
                }
                else
                {
                    jawAngle = jawAngle.AngleTowards(0f, 0.07f);
                }
            }

            // Lunge if near the player, and prepare to chomp.
            if (distanceFromDestination * 0.5f < 110f && directionToPlayerOrthogonality > 0.45f && npc.velocity.Length() < idealFlySpeed * 1.5f)
            {
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * npc.velocity.Length() * 1.95f;
                jawAngle = jawAngle.AngleLerp(idealMouthOpeningAngle, 0.55f);
                if (chompTime == 0f)
                {
                    chompTime = 18f;
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.NPCHit, "Sounds/NPCHit/OtherworldlyHit"), npc.Center);
                }
            }
        }

        public static void DoAggressiveTargetWrapping(NPC npc, bool chomping, ref float jawAngle, ref float chompTime, ref float time, ref float flyAcceleration)
        {
            float radius = 800f + (float)Math.Cos(time % 30f / 30f * MathHelper.TwoPi) * 100f;

            // Define the destination as a circle around the player, in hopes of wrapping around them.
            Vector2 destination = Main.player[npc.target].Center + (time % 90f / 90f * MathHelper.TwoPi).ToRotationVector2() * radius;
            float distanceFromDestination = npc.Distance(destination);

            // Move more quickly the farther away the worm is from its destination.
            float flySpeed = MathHelper.Lerp(23f, 37f, Utils.InverseLerp(30f, 1700f, distanceFromDestination, true));

            // Move less quickly at the end of the wrap to prevent DoG from
            // having a lot of built up momentum when going to the charge attack.
            float momentumLoss = Utils.InverseLerp(TotalTimeSpentInPhase * 2f - 60f, TotalTimeSpentInPhase * 2f, time % (TotalTimeSpentInPhase * 2f), true);
            flySpeed *= MathHelper.Lerp(momentumLoss, 1f, 0.45f);

            // If close to the player, cease typical movement and try to eat them.
            if (npc.DistanceSQ(Main.player[npc.target].Center) < 160f * 160f)
                DoAggressiveFlyMovement(npc, chomping, ref jawAngle, ref chompTime, ref time, ref flyAcceleration);

            // Otherwise, fly smoothly towards the destination.
            else if (npc.DistanceSQ(destination) > 180f * 180f)
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), flyAcceleration, true) * flySpeed;
        }

        [OverrideAppliesTo("DevourerofGodsBody", typeof(DoGAIClass), "DoGSegmentAI", EntityOverrideContext.NPCAI)]
        [OverrideAppliesTo("DevourerofGodsTail", typeof(DoGAIClass), "DoGSegmentAI", EntityOverrideContext.NPCAI)]
        [OverrideAppliesTo("DevourerofGodsBodyS", typeof(DoGAIClass), "DoGSegmentAI", EntityOverrideContext.NPCAI)]
        [OverrideAppliesTo("DevourerofGodsTailS", typeof(DoGAIClass), "DoGSegmentAI", EntityOverrideContext.NPCAI)]
        public static bool DoGSegmentAI(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            NPC head = Main.npc[(int)npc.ai[2]];
            if (!aheadSegment.active)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
            }

            // Inherit various attributes from the head segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.scale = aheadSegment.scale;

            if (head.type == ModContent.NPCType<DevourerofGodsHeadS>() && head.Infernum().ExtraAI[1] == 0f && head.Infernum().ExtraAI[3] >= 1200f)
            {
                if (npc.Hitbox.Intersects(Main.projectile[(int)head.Infernum().ExtraAI[18]].Hitbox))
                {
                    npc.alpha += 70;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }
            }

            else if (head.type == ModContent.NPCType<DevourerofGodsHeadS>() && head.Infernum().ExtraAI[13] > 381f)
            {
                if (npc.Hitbox.Intersects(Main.projectile[(int)head.Infernum().ExtraAI[14]].Hitbox))
                {
                    npc.alpha += 70;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }
            }
            else if (head.type == ModContent.NPCType<DevourerofGodsHead>() && head.Infernum().ExtraAI[11] > 0f)
            {
                if (npc.Hitbox.Intersects(Main.projectile[(int)head.Infernum().ExtraAI[11]].Hitbox))
                {
                    npc.alpha += 140;
                    if (npc.alpha > 255)
                    {
                        npc.alpha = 255;

                        int headType = ModContent.NPCType<DevourerofGodsHead>();
                        int bodyType = ModContent.NPCType<DevourerofGodsBody>();
                        int tailType = ModContent.NPCType<DevourerofGodsTail>();
                        if (npc.type == tailType)
                        {
                            for (int i = 0; i < Main.maxNPCs; i++)
                            {
                                if (Main.npc[i].active && (Main.npc[i].type == headType || Main.npc[i].type == bodyType || Main.npc[i].type == tailType))
                                {
                                    Main.npc[i].active = false;
                                    Main.npc[i].netUpdate = true;
                                }
                            }
                        }

                        CalamityWorld.DoGSecondStageCountdown = 305;

                        if (Main.netMode == NetmodeID.Server)
                        {
                            var netMessage = InfernumMode.CalamityMod.GetPacket();
                            netMessage.Write((byte)CalamityModMessageType.DoGCountdownSync);
                            netMessage.Write(CalamityWorld.DoGSecondStageCountdown);
                            netMessage.Send();
                        }
                    }
                }
            }
            else
                npc.Opacity = aheadSegment.Opacity;

            if (npc.type == ModContent.NPCType<DevourerofGodsBodyS>())
                typeof(DevourerofGodsBodyS).GetField("invinceTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc.modNPC, 0);
            npc.dontTakeDamage = aheadSegment.dontTakeDamage;
            npc.damage = npc.dontTakeDamage ? 0 : npc.defDamage;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.05f);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();
            return false;
        }


        [OverrideAppliesTo("DevourerofGodsHead", typeof(DoGAIClass), "DoGP1PreDraw", EntityOverrideContext.NPCPreDraw)]
        public static bool DoGP1PreDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float jawRotation = npc.Infernum().ExtraAI[7];

            Texture2D headTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/DevourerofGodsHead");
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 headTextureOrigin = Main.npcTexture[npc.type].Size() * 0.5f;
            drawPosition -= headTexture.Size() * npc.scale * 0.5f;
            drawPosition += headTextureOrigin * npc.scale + new Vector2(0f, 4f + npc.gfxOffY);

            Texture2D jawTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/DevourerofGodsJaw");
            Vector2 jawOrigin = jawTexture.Size() * 0.5f;

            for (int i = -1; i <= 1; i += 2)
            {
                float jawBaseOffset = 30f;
                SpriteEffects jawSpriteEffect = spriteEffects;
                if (i == 1)
                {
                    jawSpriteEffect |= SpriteEffects.FlipHorizontally;
                    jawBaseOffset *= -1f;
                }
                Vector2 jawPosition = drawPosition;
                jawPosition += Vector2.UnitX.RotatedBy(npc.rotation + jawRotation * i) * i * (jawBaseOffset + (float)Math.Sin(jawRotation) * 20f);
                jawPosition -= Vector2.UnitY.RotatedBy(npc.rotation) * (26f + (float)Math.Sin(jawRotation) * 30f);
                spriteBatch.Draw(jawTexture, jawPosition, npc.frame, drawColor, npc.rotation + jawRotation * i, jawOrigin, npc.scale, jawSpriteEffect, 0f);
            }

            spriteBatch.Draw(headTexture, drawPosition, npc.frame, npc.GetAlpha(drawColor), npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);

            Texture2D glowmaskTexture = ModContent.GetTexture("CalamityMod/NPCs/DevourerofGods/DevourerofGodsHeadGlow");

            spriteBatch.Draw(glowmaskTexture, drawPosition, npc.frame, Color.Lerp(Color.White, Color.Fuchsia, 0.5f), npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);

            glowmaskTexture = ModContent.GetTexture("CalamityMod/NPCs/DevourerofGods/DevourerofGodsHeadGlow2");
            spriteBatch.Draw(glowmaskTexture, drawPosition, npc.frame, Color.Lerp(Color.White, Color.Cyan, 0.5f), npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);

            return false;
        }
    }
}
