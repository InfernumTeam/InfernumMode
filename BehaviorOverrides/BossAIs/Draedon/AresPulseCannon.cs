using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityModClass = CalamityMod.CalamityMod;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class AresPulseCannon : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("XF-09 Ares Pulse Cannon");
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 5f;
            npc.damage = 100;
            npc.width = 170;
            npc.height = 120;
            npc.defense = 80;
            npc.DR_NERD(0.35f);
            npc.LifeMaxNERB(1250000, 1495000, 500000);
            double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            npc.lifeMax += (int)(npc.lifeMax * HPBoost);
            npc.aiStyle = -1;
            aiType = -1;
            npc.Opacity = 0f;
            npc.knockBackResist = 0f;
            npc.canGhostHeal = false;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath14;
            npc.netAlways = true;
            npc.boss = true;
            npc.hide = true;
            music = (InfernumMode.CalamityMod as CalamityModClass).GetMusicFromMusicMod("ExoMechs") ?? MusicID.Boss3;
        }

        public override void AI()
        {
            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];

            // Define the life ratio.
            npc.life = aresBody.life;
            npc.lifeMax = aresBody.lifeMax;

            // Shamelessly steal variables from Ares.
            npc.target = aresBody.target;
            npc.Opacity = aresBody.Opacity;
            npc.dontTakeDamage = aresBody.dontTakeDamage;
            int projectileDamageBoost = (int)aresBody.Infernum().ExtraAI[8];
            Player target = Main.player[npc.target];

            // Define attack variables.
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
            int shootTime = 150;
            int totalFlamesPerBurst = 2;
            float flameShootSpeed = 10f;
            float aimPredictiveness = 20f;

            // Nerf things while Ares' complement mech is present.
            if (ExoMechManagement.CurrentAresPhase == 4)
                flameShootSpeed *= 0.85f;

            if (ExoMechManagement.CurrentAresPhase >= 5)
            {
                shootTime += 60;
                totalFlamesPerBurst += 2;
                flameShootSpeed *= 1.25f;
            }
            if (ExoMechManagement.CurrentAresPhase >= 6)
            {
                shootTime += 30;
                totalFlamesPerBurst++;
            }

            // Get very pissed off if Ares is enraged.
            if (aresBody.Infernum().ExtraAI[13] == 1f)
                totalFlamesPerBurst += 5;

            int shootRate = shootTime / totalFlamesPerBurst;
            ref float attackTimer = ref npc.ai[0];
            ref float chargeDelay = ref npc.ai[1];

            // Initialize delays and other timers.
            if (chargeDelay == 0f)
                chargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime;

            // Don't do anything if this arm should be disabled.
            if (currentlyDisabled && attackTimer >= chargeDelay)
                attackTimer = chargeDelay;

            // Hover near Ares.
            Vector2 hoverDestination = aresBody.Center + Vector2.UnitX * (aresBody.Infernum().ExtraAI[15] == 1f ? -1f : 1f) * 575f;
            AresBodyBehaviorOverride.DoHoverMovement(npc, hoverDestination, 45f, 90f);

            // Check to see if this arm should be used for special things in a combo attack.
            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(npc))
            {
                float _ = 0f;
                ExoMechComboAttackContent.UseThanatosAresComboAttack(npc, ref aresBody.ai[1], ref _);
                return;
            }

            // Choose a direction and rotation.
            // Rotation is relative to predictiveness, unless disabled.
            Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
            Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 66f + Vector2.UnitY * 16f;
            float idealRotation = aimDirection.ToRotation();
            if (currentlyDisabled)
                idealRotation = MathHelper.Clamp(npc.velocity.X * -0.016f, -0.81f, 0.81f) + MathHelper.PiOver2;

            if (npc.spriteDirection == 1)
                idealRotation += MathHelper.Pi;
            if (idealRotation < 0f)
                idealRotation += MathHelper.TwoPi;
            if (idealRotation > MathHelper.TwoPi)
                idealRotation -= MathHelper.TwoPi;
            npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

            int direction = Math.Sign(target.Center.X - npc.Center.X);
            if (direction != 0)
            {
                npc.direction = direction;

                if (npc.spriteDirection != -npc.direction)
                    npc.rotation += MathHelper.Pi;

                npc.spriteDirection = -npc.direction;
            }

            // Create a dust telegraph before firing.
            if (attackTimer > chargeDelay * 0.7f && attackTimer < chargeDelay)
            {
                Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                Dust plasma = Dust.NewDustPerfect(dustSpawnPosition, 107);
                plasma.velocity = (endOfCannon - plasma.position) * 0.04f;
                plasma.scale = 1.25f;
                plasma.noGravity = true;
            }

            // Fire plasma.
            if (attackTimer >= chargeDelay && attackTimer % shootRate == shootRate - 1f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int fireballCount = ExoMechManagement.CurrentAresPhase >= 3 ? 2 : 1;

                    for (int i = 0; i < fireballCount; i++)
                    {
                        Vector2 flameShootVelocity = aimDirection * flameShootSpeed;
                        int fireballType = ModContent.ProjectileType<AresPlasmaFireball>();
                        if (ExoMechManagement.CurrentAresPhase >= 2)
                            fireballType = ModContent.ProjectileType<AresPlasmaFireball2>();
                        if (fireballCount > 1)
                        {
                            flameShootVelocity = flameShootVelocity.RotatedByRandom(0.34f);
                            if (i > 0)
                                flameShootVelocity *= Main.rand.NextFloat(0.6f, 0.9f);
                        }

                        Utilities.NewProjectileBetter(endOfCannon, flameShootVelocity, fireballType, projectileDamageBoost + 550, 0f);
                    }

                    npc.netUpdate = true;
                }
            }

            // Reset the attack timer after an attack cycle ends.
            if (attackTimer >= chargeDelay + shootTime)
            {
                attackTimer = 0f;
                npc.netUpdate = true;
            }
            attackTimer++;
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCProjectiles.Add(index);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            Texture2D texture = Main.npcTexture[npc.type];
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() * 0.5f;
            Color afterimageBaseColor = aresBody.Infernum().ExtraAI[13] == 1f ? Color.Red : Color.White;
            int numAfterimages = 5;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, spriteEffects, 0f);
                }
            }

            Vector2 center = npc.Center - Main.screenPosition;
            spriteBatch.Draw(texture, center, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/AresPulseCannonGlow");

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, spriteEffects, 0f);
                }
            }

            spriteBatch.Draw(texture, center, frame, afterimageBaseColor * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }

        public override bool CheckActive() => false;
    }
}
