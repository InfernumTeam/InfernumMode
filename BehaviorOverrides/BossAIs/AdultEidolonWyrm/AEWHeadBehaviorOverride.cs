using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Abyss;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    /*
    public class AEWHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum AEWAttackType
        {
            AbyssalCrash,
            HadalSpirits,
            PsychicBlasts,
            UndynesTail
        }

        public static List<AEWAttackType[]> Phase1AttackCycles = new List<AEWAttackType[]>()
        {
            new AEWAttackType[] { AEWAttackType.HadalSpirits, AEWAttackType.PsychicBlasts, AEWAttackType.UndynesTail },
            new AEWAttackType[] { AEWAttackType.UndynesTail, AEWAttackType.AbyssalCrash },
            new AEWAttackType[] { AEWAttackType.UndynesTail, AEWAttackType.PsychicBlasts, AEWAttackType.HadalSpirits, AEWAttackType.AbyssalCrash },
        };

        public static List<AEWAttackType[]> CurrentAttackCycles => Phase1AttackCycles;

        public override int NPCOverrideType => ModContent.NPCType<EidolonWyrmHeadHuge>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public static Color CalculateEyeColor(NPC npc)
        {
            float hue = npc.Infernum().ExtraAI[5] / CurrentAttackCycles.Count % 1f;
            return Main.hslToRgb(hue, 1f, 0.55f);
        }

        public override bool PreAI(NPC npc)
        {
            // Use the default AI if SCal and Draedon are not both dead.
            if (!CalamityWorld.downedExoMechs || !CalamityWorld.downedSCal)
                return true;

            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Disappear if the target is gone.
            if (!target.active || target.dead)
            {
                npc.TargetClosest();
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                {
                    npc.active = false;
                    return false;
                }
            }

            // Set the whoAmI variable.
            CalamityGlobalNPC.adultEidolonWyrmHead = npc.whoAmI;

            // Do enrage checks.
            bool enraged = ArenaSpawnAndEnrageCheck(npc, target);
            npc.Calamity().CurrentlyEnraged = enraged;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            float generalDamageFactor = enraged ? 40f : 1f;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hasInitialized = ref npc.localAI[0];
            ref float etherealnessFactor = ref npc.localAI[1];

            // Do initializations.
            if (hasInitialized == 0f)
            {
                npc.Opacity = 1f;

                int Previous = npc.whoAmI;
                for (int i = 0; i < 41; i++)
                {
                    int lol;
                    if (i >= 0 && i < 40)
                    {
                        if (i % 2 == 0)
                            lol = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<EidolonWyrmBodyHuge>(), npc.whoAmI + 1);
                        else
                            lol = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<EidolonWyrmBodyAltHuge>(), npc.whoAmI + 1);
                    }
                    else
                        lol = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<EidolonWyrmTailHuge>(), npc.whoAmI + 1);

                    Main.npc[lol].realLife = npc.whoAmI;
                    Main.npc[lol].ai[2] = npc.whoAmI;
                    Main.npc[lol].ai[1] = Previous;

                    if (i > 0)
                        Main.npc[Previous].ai[0] = lol;
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, lol, 0f, 0f, 0f, 0);
                    Previous = lol;
                    Main.npc[Previous].ai[3] = i / 2;
                }
                hasInitialized = 1f;
            }

            // Make the etherealness effect naturally dissipate.
            etherealnessFactor = MathHelper.Clamp(etherealnessFactor - 0.025f, 0f, 1f);

            // Reset damage and other things.
            npc.damage = (int)(npc.defDamage * generalDamageFactor);

            switch ((AEWAttackType)(int)attackType)
            {
                case AEWAttackType.AbyssalCrash:
                    DoBehavior_AbyssalCrash(npc, target, generalDamageFactor, ref attackTimer);
                    break;
                case AEWAttackType.HadalSpirits:
                    DoBehavior_HadalSpirits(npc, target, lifeRatio, generalDamageFactor, ref etherealnessFactor, ref attackTimer);
                    break;
                case AEWAttackType.PsychicBlasts:
                    DoBehavior_PsychicBlasts(npc, target, lifeRatio, generalDamageFactor, ref etherealnessFactor, ref attackTimer);
                    break;
                case AEWAttackType.UndynesTail:
                    DoBehavior_UndynesTail(npc, target, lifeRatio, generalDamageFactor, ref etherealnessFactor, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void DoBehavior_AbyssalCrash(NPC npc, Player target, float generalDamageFactor, ref float attackTimer)
        {
            // Define attack variables.
            int waterShootRate = 45;
            int waterPerBurst = 3;
            int attackChangeDelay = 90;
            int attackTime = 480;

            // Periodically release streams of abyssal water.
            bool readyToShootJet = attackTimer % waterShootRate == waterShootRate - 1f && attackTimer < attackTime - attackChangeDelay;
            if (!npc.WithinRange(target.Center, 240f) && readyToShootJet)
            {
                Main.PlaySound(SoundID.Item73, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int jetDamage = (int)(generalDamageFactor * 640f);
                    for (int i = 0; i < waterPerBurst; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.17f, 0.17f, i / (float)(waterPerBurst - 1f));
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * 10f;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<AbyssalWaterJet>(), jetDamage, 0f);
                    }
                }
            }

            // Do movement.
            DoDefaultSwimMovement(npc, target);

            // Decide rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer > attackTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HadalSpirits(NPC npc, Player target, float lifeRatio, float generalDamageFactor, ref float etherealnessFactor, ref float attackTimer)
        {
            int attackShootDelay = 60;
            int attackShootRate = (int)MathHelper.Lerp(15f, 10f, 1f - lifeRatio);
            int homingSpiritShootRate = attackShootRate * 4;
            int attackChangeDelay = 90;
            int attackTime = 600;
            int psychicBlastCircleShootRate = 90;
            ref float spiritSpawnOffsetAngle = ref npc.Infernum().ExtraAI[0];

            // Initialize the spawn offset angle.
            if (spiritSpawnOffsetAngle == 0f)
            {
                spiritSpawnOffsetAngle = Main.rand.NextFloatDirection() * 0.36f;
                npc.netUpdate = true;
            }

            // Reset damage to 0.
            npc.damage = 0;

            // Do movement.
            DoDefaultSwimMovement(npc, target, 0.625f);

            // Decide rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Decide the etherealness factor.
            etherealnessFactor = Utils.InverseLerp(0f, 60f, attackTimer, true) * Utils.InverseLerp(attackTime, attackTime - attackChangeDelay, attackTimer, true);

            if (attackTimer > attackTime)
                SelectNextAttack(npc);

            if (attackTimer > attackTime - attackChangeDelay)
                return;

            // Periodically release blasts of psychic energy.
            if (attackTimer % psychicBlastCircleShootRate == psychicBlastCircleShootRate - 1f)
            {
                int blastDamage = (int)(generalDamageFactor * 640f);
                for (int i = 0; i < 10; i++)
                {
                    Vector2 blastShootVelocity = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 15f;
                    Projectile.NewProjectile(npc.Center, blastShootVelocity, ModContent.ProjectileType<PsionicRay>(), blastDamage, 0f);
                }
            }

            // Release souls below the target.
            int spiritDamage = (int)(generalDamageFactor * 640f);
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > attackShootDelay && attackTimer % attackShootRate == attackShootRate - 1f)
            {
                Vector2 spiritSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 800f, 1080f);
                Vector2 spiritVelocity = -Vector2.UnitY.RotatedBy(spiritSpawnOffsetAngle) * 17f;
                Utilities.NewProjectileBetter(spiritSpawnPosition, spiritVelocity, ModContent.ProjectileType<HadalSpirit>(), spiritDamage, 0f);
            }

            // Release homing souls around the target.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > attackShootDelay && attackTimer % homingSpiritShootRate == homingSpiritShootRate - 1f)
            {
                Vector2 spiritSpawnPosition = target.Center + Main.rand.NextVector2Circular(1200f, 1200f);
                Vector2 spiritVelocity = (target.Center - spiritSpawnPosition).SafeNormalize(Vector2.UnitY) * 12f;
                Utilities.NewProjectileBetter(spiritSpawnPosition, spiritVelocity, ModContent.ProjectileType<HomingHadalSpirit>(), spiritDamage, 0f);
            }
        }

        public static void DoBehavior_PsychicBlasts(NPC npc, Player target, float lifeRatio, float generalDamageFactor, ref float etherealnessFactor, ref float attackTimer)
        {
            int attackShootDelay = 60;
            int orbCreationRate = (int)MathHelper.Lerp(8f, 4f, 1f - lifeRatio);
            int attackChangeDelay = 90;
            int attackTime = 900;

            // Reset damage to 0.
            npc.damage = 0;

            // Do movement.
            DoDefaultSwimMovement(npc, target, 0.7f);

            // Decide rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Decide the etherealness factor.
            etherealnessFactor = Utils.InverseLerp(0f, 60f, attackTimer, true) * Utils.InverseLerp(attackTime, attackTime - attackChangeDelay, attackTimer, true);

            if (attackTimer > attackTime)
                SelectNextAttack(npc);

            if (attackTimer > attackTime - attackChangeDelay)
                return;

            // Release psychic fields around the head.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > attackShootDelay && attackTimer % orbCreationRate == orbCreationRate - 1f)
            {
                int fieldBoltDamage = (int)(generalDamageFactor * 640f);
                Vector2 fieldSpawnPosition = npc.Center + Main.rand.NextVector2Circular(660f, 150f).RotatedBy(npc.rotation);
                Utilities.NewProjectileBetter(fieldSpawnPosition, Vector2.Zero, ModContent.ProjectileType<PsychicEnergyField>(), fieldBoltDamage, 0f);
            }
        }

        public static void DoBehavior_UndynesTail(NPC npc, Player target, float lifeRatio, float generalDamageFactor, ref float etherealnessFactor, ref float attackTimer)
        {
            int totalCharges = 8;
            int redirectTime = 45;
            int chargeTime = 48;
            float chargeSpeed = MathHelper.Lerp(48f, 65f, 1f - lifeRatio);

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            // Decide the etherealness factor.
            etherealnessFactor = MathHelper.Lerp(etherealnessFactor, npc.Opacity, MathHelper.Lerp(0.25f, 0.08f, npc.Opacity));

            // Decide rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            switch ((int)attackSubstate)
            {
                // Line up in preparation for the charge.
                case 0:
                    Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 500f, -350f);
                    Vector2 idealHoverVelocity = npc.SafeDirectionTo(hoverDestination) * 45f;
                    npc.velocity = npc.velocity.RotateTowards(idealHoverVelocity.ToRotation(), 0.045f).MoveTowards(idealHoverVelocity, 10f);

                    // Fade out.
                    npc.Opacity = Utils.InverseLerp(redirectTime * 0.5f, redirectTime * 0.5f - 12f, attackTimer, true);

                    // Disable conct damage.
                    npc.damage = 0;

                    // Begin the charge.
                    if (attackTimer > redirectTime * 0.5f && (npc.WithinRange(hoverDestination, 60f) || attackTimer > redirectTime))
                    {
                        chargeDirection = npc.AngleTo(target.Center + target.velocity * 15f);
                        attackSubstate = 1f;
                        attackTimer = 0f;
                        npc.velocity = npc.velocity.AngleDirectionLerp(chargeDirection.ToRotationVector2(), 0.33f).SafeNormalize(Vector2.Zero) * chargeSpeed * 0.6f;
                        npc.netUpdate = true;
                    }
                    break;

                // Move into the charge.
                case 1:
                    float newSpeed = MathHelper.Lerp(npc.velocity.Length(), chargeSpeed, 0.18f);
                    npc.velocity = npc.velocity.RotateTowards(chargeDirection, MathHelper.Pi / 3f, true) * newSpeed;

                    // Fade in.
                    npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.05f, 0f, 1f);

                    // Release water spears from the tail of the wyrm.
                    int tail = NPC.FindFirstNPC(ModContent.NPCType<EidolonWyrmTailHuge>());
                    if (tail != -1 && !Main.npc[tail].WithinRange(target.Center, 200f) && attackTimer % 4f == 3f)
                    {
                        Main.PlaySound(SoundID.Item66, Main.npc[tail].Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int spearDamage = (int)(generalDamageFactor * 640f);
                            Vector2 spearShootVelocity = Main.rand.NextVector2CircularEdge(27f, 27f);
                            Utilities.NewProjectileBetter(Main.npc[tail].Center, spearShootVelocity, ModContent.ProjectileType<HomingWaterSpear>(), spearDamage, 0f);
                        }
                    }
                    
                    if (attackTimer > chargeTime)
                    {
                        chargeDirection = 0f;
                        attackSubstate = 0f;
                        attackTimer = 0f;
                        chargeCounter++;
                        npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 24f, 0.4f);
                        npc.netUpdate = true;

                        if (chargeCounter >= totalCharges)
                            SelectNextAttack(npc);
                    }
                    break;
            }
        }

        public static void DoDefaultSwimMovement(NPC npc, Player target, float generalSpeedFactor = 1f)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float idealFlySpeed = MathHelper.Lerp(27f, 34f, 1f - lifeRatio) * generalSpeedFactor;
            float flyAcceleration = MathHelper.Lerp(0.03f, 0.0425f, 1f - lifeRatio) * generalSpeedFactor;
            float newSpeed = MathHelper.Lerp(npc.velocity.Length(), idealFlySpeed, 0.08f);
            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * idealFlySpeed;

            // Fly towards the target.
            if (!npc.WithinRange(target.Center, 240f))
            {
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), flyAcceleration, true) * newSpeed;
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, flyAcceleration * 25f);
            }

            // Accelerate if close to the target.
            else
                npc.velocity = (npc.velocity * 1.025f).ClampMagnitude(10f, 50f);
        }

        public static void SelectNextAttack(NPC npc)
        {
            ref float attackCycleType = ref npc.Infernum().ExtraAI[5];
            ref float attackCycleIndex = ref npc.Infernum().ExtraAI[6];
            List<AEWAttackType[]> attackCycles = Phase1AttackCycles;
            int oldAttackCycle = (int)attackCycleType;

            attackCycleIndex++;

            // Shift the attack cycle once the current one's end has been reached.
            if (attackCycleIndex >= attackCycles[(int)attackCycleType].Length)
            {
                attackCycleIndex = 0f;
                do
                    attackCycleType = Main.rand.Next(attackCycles.Count);
                while (attackCycleType == oldAttackCycle);
            }

            npc.ai[0] = (int)attackCycles[(int)attackCycleType][(int)attackCycleIndex];
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        public static bool ArenaSpawnAndEnrageCheck(NPC npc, Player player)
        {
            ref float enraged01Flag = ref npc.ai[2];
            ref float spawnedArena01Flag = ref npc.ai[3];

            // Create the arena, but not as a multiplayer client.
            // In single player, the arena gets created and never gets synced because it's single player.
            if (spawnedArena01Flag == 0f)
            {
                spawnedArena01Flag = 1f;
                enraged01Flag = 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int width = 9600;
                    npc.Infernum().arenaRectangle.X = (int)(player.Center.X - width * 0.5f);
                    npc.Infernum().arenaRectangle.Y = (int)(player.Center.Y - 160000f);
                    npc.Infernum().arenaRectangle.Width = width;
                    npc.Infernum().arenaRectangle.Height = 320000;
                    Vector2 spawnPosition = player.Center + new Vector2(width * 0.5f, 100f);
                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<TornadoBorder>(), 10000, 0f, Main.myPlayer, 0f, 0f);
                    spawnPosition = player.Center + new Vector2(width * -0.5f, 100f);
                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<TornadoBorder>(), 10000, 0f, Main.myPlayer, 0f, 0f);
                }

                // Force Yharon to send a sync packet so that the arena gets sent immediately
                npc.netUpdate = true;
            }
            // Enrage code doesn't run on frame 1 so that Yharon won't be enraged for 1 frame in multiplayer
            else
            {
                var arena = npc.Infernum().arenaRectangle;
                enraged01Flag = (!player.Hitbox.Intersects(arena)).ToInt();
                if (enraged01Flag == 1f)
                    return true;
            }
            return false;
        }

        public static void DrawSegment(SpriteBatch spriteBatch, Color lightColor, NPC npc)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float etherealnessFactor = npc.localAI[1];
            if (npc.realLife >= 0)
                etherealnessFactor = Main.npc[npc.realLife].localAI[1];
            float opacity = MathHelper.Lerp(1f, 0.75f, etherealnessFactor) * npc.Opacity;
            Color color = Color.Lerp(lightColor, Main.hslToRgb(Main.GlobalTime * 0.7f % 1f, 1f, 0.74f), etherealnessFactor * 0.85f);
            color.A = (byte)(int)(255 - etherealnessFactor * 84f);

            if (etherealnessFactor > 0f)
            {
                float etherealOffsetPulse = etherealnessFactor * 16f;

                for (int i = 0; i < 32; i++)
                {
                    Color baseColor = Main.hslToRgb((Main.GlobalTime * 1.7f + i / 32f) % 1f, 1f, 0.8f);
                    Color etherealAfterimageColor = Color.Lerp(lightColor, baseColor, etherealnessFactor * 0.85f) * 0.24f;
                    etherealAfterimageColor.A = (byte)(int)(255 - etherealnessFactor * 255f);
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 32f).ToRotationVector2() * etherealOffsetPulse;
                    spriteBatch.Draw(texture, drawPosition + drawOffset, npc.frame, etherealAfterimageColor * opacity, npc.rotation, origin, npc.scale, 0, 0f);
                }
            }

            for (int i = 0; i < (int)Math.Round(1f + etherealnessFactor); i++)
                spriteBatch.Draw(texture, drawPosition, npc.frame, color * opacity, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            DrawSegment(spriteBatch, lightColor, npc);
            Texture2D eyeTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/AdultEidolonWyrm/EidolonWyrmEyes");
            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * 1.5f;
            Vector2 origin = eyeTexture.Size() * 0.5f;
            Color eyeColor = npc.GetAlpha(CalculateEyeColor(npc));

            spriteBatch.Draw(eyeTexture, drawPosition, npc.frame, eyeColor, npc.rotation, origin, npc.scale, 0, 0f);
            return false;
        }
    }
    */
}
