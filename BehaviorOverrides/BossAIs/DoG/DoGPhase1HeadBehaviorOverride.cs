using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
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

using DoGHead = CalamityMod.NPCs.DevourerofGods.DevourerofGodsHead;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGPhase1HeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DoGHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public const float Phase2LifeRatio = 0.75f;

        #region AI
        public override bool PreAI(NPC npc)
        {
            Main.player[npc.target].Calamity().normalityRelocator = false;
            Main.player[npc.target].Calamity().spectralVeil = false;

            ref float attackTimer = ref npc.Infernum().ExtraAI[5];
            ref float flyAcceleration = ref npc.Infernum().ExtraAI[6];
            ref float jawAngle = ref npc.Infernum().ExtraAI[7];
            ref float chompTime = ref npc.Infernum().ExtraAI[8];
            ref float inPhase2 = ref npc.Infernum().ExtraAI[33];
            ref float uncoilTimer = ref npc.Infernum().ExtraAI[35];

            // Timer effect.
            attackTimer++;

            // Adjust scale.
            npc.scale = 1.2f;

            // Adjust DR and defense.
            npc.defense = 0;
            npc.Calamity().DR = 0f;
            npc.takenDamageMultiplier = 2f;

            // Declare this NPC as the occupant of the DoG whoAmI index.
            CalamityGlobalNPC.DoGHead = npc.whoAmI;

            // Stop rain.
            if (Main.raining)
                Main.raining = false;

            if (inPhase2 == 1f)
            {
                typeof(DoGHead).GetField("phase2Started", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc.modNPC, true);
                return DoGPhase2HeadBehaviorOverride.Phase2AI(npc);
            }

            if (npc.Infernum().ExtraAI[10] > 0f)
            {
                npc.Calamity().CanHaveBossHealthBar = false;
                npc.velocity = npc.velocity.ClampMagnitude(32f, 60f);
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), 50f, 0.1f);
                npc.damage = 0;

                if (npc.Infernum().ExtraAI[10] == 1f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 spawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.UnitX) * 1600f;
                        npc.Infernum().ExtraAI[11] = Projectile.NewProjectile(spawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                        Main.projectile[(int)npc.Infernum().ExtraAI[11]].localAI[0] = 1f;
                        Main.projectile[(int)npc.Infernum().ExtraAI[11]].localAI[1] = 280f;
                    }

                    int headType = ModContent.NPCType<DoGHead>();
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

                if (Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    npc.TargetClosest();
                    if (Main.player[npc.target].dead || !Main.player[npc.target].active)
                        npc.active = false;
                }

                return false;
            }

            npc.dontTakeDamage = false;
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
                        Main.npc[segment].Infernum().ExtraAI[34] = 80f - segmentSpawn;
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, segment, 0f, 0f, 0f, 0);
                        Previous = segment;
                    }
                    npc.Infernum().ExtraAI[11] = -1f;
                    npc.Infernum().ExtraAI[2] = 1f;
                }
            }

            // Perform sentinel attacks as necessary.
            DoPhase1SentinelAttacks(npc);

            // Chomping after attempting to eat the player.
            bool chomping = !npc.dontTakeDamage && DoChomp(npc, ref chompTime, ref jawAngle);

            // Prevent the Godslayer Inferno debuff from being a problem.
            if (Main.player[npc.target].HasBuff(ModContent.BuffType<GodSlayerInferno>()))
                Main.player[npc.target].ClearBuff(ModContent.BuffType<GodSlayerInferno>());

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
            {
                if (uncoilTimer < 45f)
                {
                    uncoilTimer++;
                    npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 27f, 0.125f);
                }
                else
                    DoAggressiveFlyMovement(npc, chomping, ref jawAngle, ref chompTime, ref attackTimer, ref flyAcceleration);
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
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
                    if (npc.Infernum().ExtraAI[4] % 60 == 0f &&
                        npc.Infernum().ExtraAI[4] < 900 + 60 * 2)
                    {
                        int fuck = Projectile.NewProjectile(Main.player[npc.target].Center +
                            new Vector2(Main.rand.NextFloat(-290f, 290f), -1200f),
                            Vector2.Zero,
                            ModContent.ProjectileType<Lightning>(),
                            85, 0f, npc.target);
                        if (Main.projectile.IndexInRange(fuck))
                            Main.projectile[fuck].ai[0] = 1f;
                    }

                    else if (npc.Infernum().ExtraAI[4] % 60 == 0f &&
                             npc.Infernum().ExtraAI[4] > 900 + 60 * 2)
                    {
                        int fuck = Projectile.NewProjectile(Main.player[npc.target].Center +
                            new Vector2(Main.rand.NextFloat(-290f, 290f), -1200f),
                            Vector2.Zero,
                            ModContent.ProjectileType<Lightning>(),
                            85, 0f, npc.target);
                        if (Main.projectile.IndexInRange(fuck))
                            Main.projectile[fuck].ai[0] = 1f;
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
            float idealFlyAcceleration = MathHelper.Lerp(0.045f, 0.032f, lifeRatio);
            float idealFlySpeed = MathHelper.Lerp(19f, 14.4f, lifeRatio);
            float idealMouthOpeningAngle = MathHelper.ToRadians(34f);
            float flySpeedFactor = 1f + lifeRatio * 0.425f;

            Vector2 destination = Main.player[npc.target].Center;

            float distanceFromDestination = npc.Distance(destination);
            if (distanceFromDestination > 650f)
            {
                destination += (time % 60f / 60f * MathHelper.TwoPi).ToRotationVector2() * 120f;
                distanceFromDestination = npc.Distance(destination);
                idealFlyAcceleration *= 2.5f;
                flySpeedFactor = 1.5f;
            }
            float swimOffsetAngle = (float)Math.Sin(MathHelper.TwoPi * time / 160f) * Utils.InverseLerp(460f, 600f, distanceFromDestination, true) * 0.41f;

            // Charge if the player is far away.
            // Don't do this at the start of the fight though. Doing so might lead to an unfair
            // charge.
            if (distanceFromDestination > 2700f && time > 120f)
            {
                idealFlyAcceleration = MathHelper.Min(6f, flyAcceleration + 1f);
                idealFlySpeed = 37f;
            }

            flyAcceleration = MathHelper.Lerp(flyAcceleration, idealFlyAcceleration, 0.3f);

            float directionToPlayerOrthogonality = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(destination));
            if (distanceFromDestination > 100f)
            {
                float speed = npc.velocity.Length();
                if (speed < 17f)
                    speed += 0.08f;

                if (speed > 24.5f)
                    speed -= 0.08f;

                if (directionToPlayerOrthogonality < 0.85f && directionToPlayerOrthogonality > 0.5f)
                    speed += 0.24f;

                if (directionToPlayerOrthogonality < 0.5f && directionToPlayerOrthogonality > -0.7f)
                    speed -= 0.1f;

                speed = MathHelper.Clamp(speed, flySpeedFactor * 14f, flySpeedFactor * 32f);

                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination) + swimOffsetAngle, flyAcceleration, true) * speed;
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * speed, flyAcceleration * 25f);
            }

            // Jaw opening when near player.
            if (!chomping)
            {
                if ((npc.Distance(Main.player[npc.target].Center) < 310f && directionToPlayerOrthogonality > 0.67f) ||
                    (npc.Distance(Main.player[npc.target].Center) < 490f && directionToPlayerOrthogonality > 0.92f))
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

        #endregion AI

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (npc.Infernum().ExtraAI[33] == 1f)
                return DoGPhase2HeadBehaviorOverride.PreDraw(npc, spriteBatch, lightColor);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float jawRotation = npc.Infernum().ExtraAI[7];

            Texture2D headTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1Head");
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 headTextureOrigin = headTexture.Size() * 0.5f;

            Texture2D jawTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1Jaw");
            Vector2 jawOrigin = jawTexture.Size() * 0.5f;

            for (int i = -1; i <= 1; i += 2)
            {
                float jawBaseOffset = 20f;
                SpriteEffects jawSpriteEffect = spriteEffects;
                if (i == 1)
                {
                    jawSpriteEffect |= SpriteEffects.FlipHorizontally;
                    jawBaseOffset *= -1f;
                }
                Vector2 jawPosition = drawPosition;
                jawPosition += Vector2.UnitX.RotatedBy(npc.rotation + jawRotation * i) * (18f + i * (34f + jawBaseOffset + (float)Math.Sin(jawRotation) * 20f));
                jawPosition -= Vector2.UnitY.RotatedBy(npc.rotation) * (16f + (float)Math.Sin(jawRotation) * 20f);
                spriteBatch.Draw(jawTexture, jawPosition, null, lightColor, npc.rotation + jawRotation * i, jawOrigin, npc.scale, jawSpriteEffect, 0f);
            }

            Rectangle headFrame = headTexture.Frame();
            spriteBatch.Draw(headTexture, drawPosition, headFrame, npc.GetAlpha(lightColor), npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);

            Texture2D glowmaskTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1HeadGlow");
            spriteBatch.Draw(glowmaskTexture, drawPosition, headFrame, Color.White, npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion Drawing
    }
}
