using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Leviathan;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

using LeviathanNPC = CalamityMod.NPCs.Leviathan.Leviathan;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Leviathan
{
    public class LeviathanBehaviorOverride : NPCBehaviorOverride
    {
        public enum LeviathanAttackType
        {
            // Alone attacks.
            VomitBlasts,
            HorizontalCharges,
            MeteorBelch,

            // Alone and enraged attacks.
            AberrationCharges
        }

        public override int NPCOverrideType => ModContent.NPCType<LeviathanNPC>();

        public override int? NPCTypeToDeferToForTips => ModContent.NPCType<Anahita>();

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 900;
            npc.height = 450;
            npc.scale = 1f;
            npc.Opacity = 0f;
            npc.defense = 40;
            npc.DR_NERD(0.35f);
        }

        public override bool PreAI(NPC npc)
        {
            // Stay within the world you stupid fucking fish I swear to god.
            npc.position.X = Clamp(npc.position.X, 360f, Main.maxTilesX * 16f - 360f);

            // Select a target and reset damage and invulnerability.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];
            npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();
            npc.damage = npc.defDamage;
            npc.defense = 12;
            npc.dontTakeDamage = false;

            // Send natural despawns into the sun.
            npc.timeLeft = 3600;

            // Despawn.
            if (target.dead || !target.active)
            {
                npc.TargetClosest();
                if (target.dead || !target.active)
                {
                    npc.active = false;
                    return false;
                }
            }

            // Set the whoAmI variable.
            CalamityGlobalNPC.leviathan = npc.whoAmI;

            // Inherit attributes from the leader.
            LeviathanComboAttackManager.InheritAttributesFromLeader(npc);

            ref float attackTimer = ref npc.ai[1];
            ref float frameState = ref npc.localAI[0];
            ref float spawnAnimationTime = ref npc.ai[2];

            // Adjust Calamity's version of the spawn animation timer, for sky darkening purposes.
            npc.Calamity().newAI[3] = spawnAnimationTime;

            // Reset things.
            frameState = 0f;
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            // Don't take damage if the target leaves the ocean.
            bool outOfOcean = target.position.X > AnahitaBehaviorOverride.OceanDistanceLeniancy &&
                target.position.X < Main.maxTilesX * 16 - AnahitaBehaviorOverride.OceanDistanceLeniancy && !BossRushEvent.BossRushActive;
            if (outOfOcean)
            {
                npc.dontTakeDamage = true;
                npc.Calamity().CurrentlyEnraged = true;
            }

            bool enraged = LeviathanComboAttackManager.FightState == LeviAnahitaFightState.AloneEnraged;
            if (enraged)
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.LeviathanFinalPhaseTip");

            Vector2 mouthPosition = npc.Center + new Vector2(npc.spriteDirection * 380f, -45f);

            // Do spawn animation stuff.
            if (spawnAnimationTime <= 180f)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;

                float minSpawnVelocity = 0.4f;
                float maxSpawnVelocity = 4f;
                float velocityY = maxSpawnVelocity - Lerp(minSpawnVelocity, maxSpawnVelocity, spawnAnimationTime / 180f);
                npc.velocity = Vector2.UnitY * -velocityY;
                npc.Opacity = Clamp(spawnAnimationTime / 180f, 0f, 1f);
                spawnAnimationTime++;
                return false;
            }

            switch ((int)npc.ai[0])
            {
                case (int)LeviathanAttackType.VomitBlasts:
                    npc.damage = 0;
                    DoBehavior_VomitBlasts(npc, target, enraged, mouthPosition, ref attackTimer);
                    break;
                case (int)LeviathanAttackType.HorizontalCharges:
                    DoBehavior_HorizontalCharges(npc, target, enraged, ref attackTimer);
                    break;
                case (int)LeviathanAttackType.MeteorBelch:
                    npc.damage = 0;
                    DoBehavior_MeteorBelch(npc, target, enraged, mouthPosition, ref attackTimer);
                    break;
                case (int)LeviathanAttackType.AberrationCharges:
                    DoBehavior_AberrationCharges(npc, target, ref attackTimer);
                    break;
            }
            LeviathanComboAttackManager.DoComboAttacks(npc, target, ref attackTimer);

            attackTimer++;
            return false;
        }

        public static void DoBehavior_VomitBlasts(NPC npc, Player target, bool enraged, Vector2 mouthPosition, ref float attackTimer)
        {
            int shootDelay = 75;
            int shootRate = 32;
            int shootTime = 325;
            int attackTransitionDelay = 60;
            int vomitShootCount = 7;
            float vomitShootSpeed = 14.75f;
            if (enraged)
            {
                shootRate -= 5;
                vomitShootSpeed += 2.7f;
            }

            // Determine direction.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            // Roar before firing.
            if (attackTimer == shootDelay / 2)
                SoundEngine.PlaySound(LeviathanNPC.RoarMeteorSound, npc.Center);

            // Hover to the side of the target.
            Vector2 hoverDestination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * 1200f;
            float hoverSpeed = BossRushEvent.BossRushActive ? 29f : 19f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 60f);

            // Shoot bursts of vomit at the target.
            bool canShoot = attackTimer >= shootDelay && attackTimer <= shootDelay + shootTime;
            if (canShoot && attackTimer % shootRate == shootRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item45, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < vomitShootCount; i++)
                    {
                        float offsetAngle = Lerp(-0.67f, 0.67f, i / (float)(vomitShootCount - 1f));
                        Vector2 shootVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY).RotatedBy(offsetAngle) * vomitShootSpeed;
                        Utilities.NewProjectileBetter(mouthPosition, shootVelocity, ModContent.ProjectileType<LeviathanVomit>(), LeviathanComboAttackManager.LeviathanVomitDamage, 0f);
                    }
                }
            }

            // Handle frame stuff.
            npc.localAI[0] = canShoot.ToInt();
            if (canShoot)
                npc.frameCounter -= 0.5;

            if (attackTimer >= shootDelay + shootTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HorizontalCharges(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            int slowdownTime = 32;
            int redirectTime = 35;
            int chargeTime = 30;
            int chargeCount = 2;
            float chargeSpeed = 35.5f;
            if (enraged)
            {
                chargeSpeed += 3.2f;
                chargeTime -= 5;
                chargeCount++;
            }

            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            if (attackTimer < redirectTime)
            {
                npc.damage = 0;

                Vector2 destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * 1000f;

                npc.Center = Vector2.Lerp(npc.Center, new Vector2(npc.Center.X, destination.Y), 0.075f);

                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 14f, 0.27f);
                npc.velocity.Y = Lerp(npc.velocity.Y, npc.SafeDirectionTo(destination).Y * 30f, 0.18f);
                npc.spriteDirection = npc.direction;

                // Roar before charging.
                if (attackTimer == 1f && chargeCounter == 0f)
                    SoundEngine.PlaySound(LeviathanNPC.RoarChargeSound, npc.Center);
            }

            // Initiate the charge.
            if (attackTimer == redirectTime)
            {
                if (BossRushEvent.BossRushActive)
                    chargeSpeed *= 1.3f;
                npc.velocity = Vector2.UnitX * npc.direction * chargeSpeed;
                if (!npc.WithinRange(target.Center, 1100f))
                    npc.velocity *= 1.45f;
            }

            // Slow down after charging.
            if (attackTimer >= redirectTime + chargeTime)
                npc.velocity *= 0.95f;

            if (attackTimer >= redirectTime + chargeTime + slowdownTime)
            {
                attackTimer = 0f;
                chargeCounter++;
                if (chargeCounter >= chargeCount)
                    SelectNextAttack(npc);
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_MeteorBelch(NPC npc, Player target, bool enraged, Vector2 mouthPosition, ref float attackTimer)
        {
            int shootDelay = 75;
            int shootRate = 67;
            int shootTime = 185;
            int attackTransitionDelay = 60;
            float meteorShootSpeed = 16f;
            if (enraged)
            {
                shootRate -= 12;
                meteorShootSpeed += 3f;
            }

            // Determine direction.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            // Roar before firing.
            if (attackTimer == shootDelay / 2)
                SoundEngine.PlaySound(LeviathanNPC.RoarMeteorSound, npc.Center);

            // Hover to the side of the target.
            Vector2 hoverDestination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * 1110f;
            float hoverSpeed = BossRushEvent.BossRushActive ? 29f : 19f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 60f);

            // Shoot bursts of vomit at the target.
            bool canShoot = attackTimer >= shootDelay && attackTimer <= shootDelay + shootTime;
            if (canShoot && attackTimer % shootRate == shootRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item45, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 shootVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY) * meteorShootSpeed;
                    Utilities.NewProjectileBetter(mouthPosition, shootVelocity, ModContent.ProjectileType<LeviathanMeteor>(), LeviathanComboAttackManager.LeviathanMeteorDamage, 0f);
                }
            }

            // Handle frame stuff.
            npc.localAI[0] = canShoot.ToInt();
            if (canShoot)
                npc.frameCounter -= 0.5;

            if (attackTimer >= shootDelay + shootTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_AberrationCharges(NPC npc, Player target, ref float attackTimer)
        {
            int shootDelay = 75;
            int aberrationSpawnRate = 16;
            int aberrationSpawnCount = 6;
            ref float verticalSpawnOffset = ref npc.Infernum().ExtraAI[0];

            // Determine direction.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            // Roar before firing.
            if (attackTimer == shootDelay / 2)
                SoundEngine.PlaySound(LeviathanNPC.RoarMeteorSound, npc.Center);

            // Hover to the side of the target.
            Vector2 hoverDestination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * 1110f;
            float hoverSpeed = BossRushEvent.BossRushActive ? 29f : 19f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 60f);

            // Shoot bursts of vomit at the target.
            bool canShoot = attackTimer >= shootDelay && attackTimer <= shootDelay + aberrationSpawnRate * aberrationSpawnCount;
            if (canShoot && attackTimer % aberrationSpawnRate == aberrationSpawnRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath19, npc.position);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    verticalSpawnOffset += 60f;
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 aberrationSpawnPosition = target.Center + new Vector2((i == 0f).ToDirectionInt() * 1050f, verticalSpawnOffset - 900f);
                        Vector2 aberrationVelocity = (target.Center - aberrationSpawnPosition).SafeNormalize(Vector2.UnitY) * 13.5f;
                        Utilities.NewProjectileBetter(aberrationSpawnPosition, aberrationVelocity, ModContent.ProjectileType<AquaticAberrationProj>(), LeviathanComboAttackManager.AquaticAberrationDamage, 0f);
                    }
                }
            }

            // Handle frame stuff.
            npc.localAI[0] = canShoot.ToInt();
            if (canShoot)
                npc.frameCounter -= 0.5;

            if (attackTimer >= shootDelay + aberrationSpawnRate * aberrationSpawnCount + 90f)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[3]++;

            bool enraged = LeviathanComboAttackManager.FightState == LeviAnahitaFightState.AloneEnraged;
            LeviathanAttackType[] patternToUse = new LeviathanAttackType[]
            {
                LeviathanAttackType.VomitBlasts,
                LeviathanAttackType.HorizontalCharges,
                LeviathanAttackType.MeteorBelch,
                LeviathanAttackType.HorizontalCharges,
                enraged ? LeviathanAttackType.AberrationCharges : LeviathanAttackType.MeteorBelch,
            };
            LeviathanAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];

            // Go to the next AI state.
            npc.ai[0] = (int)nextAttackType;
            LeviathanComboAttackManager.SelectNextAttackSpecific(npc);

            // Reset the attack timer.
            npc.ai[1] = 0f;

            // Reset the misc ai slots.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            int timeBetweenFrames = 6;
            npc.frameCounter++;
            if (npc.frameCounter >= timeBetweenFrames * 6)
                npc.frameCounter = 0;

            int frame = (int)(npc.frameCounter / timeBetweenFrames);
            npc.frame.Width = 1011;
            npc.frame.Height = 486;
            npc.frame.X = frame / 3 * npc.frame.Width;
            npc.frame.Y = frame % 3 * npc.frame.Height;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float horizontalAfterimageInterpolant = npc.localAI[1];
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            switch ((int)npc.localAI[0])
            {
                case 0:
                    texture = TextureAssets.Npc[npc.type].Value;
                    break;
                case 1:
                    texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Leviathan/LeviathanAttack").Value;
                    break;
            }

            Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
            Color baseColor = npc.GetAlpha(lightColor) * (1f - horizontalAfterimageInterpolant);
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 origin = npc.frame.Size() * 0.5f;
            Main.spriteBatch.Draw(texture, baseDrawPosition, npc.frame, baseColor, npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }
    }
}
