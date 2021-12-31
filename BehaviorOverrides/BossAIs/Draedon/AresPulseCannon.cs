using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityModClass = CalamityMod.CalamityMod;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class AresPulseCannon : ModNPC
    {
        public ref float AttackTimer => ref npc.ai[0];
        public ref float ChargeDelay => ref npc.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("XF-09 Ares Pulse Cannon");
            Main.npcFrameCount[npc.type] = 12;
            NPCID.Sets.TrailingMode[npc.type] = 3;
            NPCID.Sets.TrailCacheLength[npc.type] = npc.oldPos.Length;
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
            if (CalamityGlobalNPC.draedonExoMechPrime < 0)
            {
                npc.active = false;
                return;
            }

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

            // Disable HP bars.
            npc.Calamity().ShouldCloseHPBar = true;

            // Define attack variables.
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
            int shootTime = 150;
            int totalFlamesPerBurst = 3;
            float blastShootSpeed = 7.5f;
            float aimPredictiveness = 15f;

            // Nerf things while Ares' complement mech is present.
            if (ExoMechManagement.CurrentAresPhase == 4)
                blastShootSpeed *= 0.85f;

            if (ExoMechManagement.CurrentAresPhase >= 5)
            {
                shootTime += 60;
                totalFlamesPerBurst += 2;
                blastShootSpeed *= 1.25f;
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

            // Initialize delays and other timers.
            if (ChargeDelay == 0f)
                ChargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime;

            // Don't do anything if this arm should be disabled.
            if (currentlyDisabled && AttackTimer >= ChargeDelay)
                AttackTimer = ChargeDelay;

            // Hover near Ares.
            bool doingHoverCharge = aresBody.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.HoverCharge;
            float horizontalOffset = doingHoverCharge ? 380f : 575f;
            float verticalOffset = doingHoverCharge ? 150f : 0f;
            Vector2 hoverDestination = aresBody.Center + new Vector2((aresBody.Infernum().ExtraAI[15] == 1f ? -1f : 1f) * horizontalOffset, verticalOffset);
            AresBodyBehaviorOverride.DoHoverMovement(npc, hoverDestination, 65f, 115f);
            npc.Infernum().ExtraAI[0] = MathHelper.Clamp(npc.Infernum().ExtraAI[0] + doingHoverCharge.ToDirectionInt(), 0f, 15f);

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
            if (doingHoverCharge)
                idealRotation = aresBody.velocity.ToRotation() - MathHelper.PiOver2;

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
            if (AttackTimer > ChargeDelay * 0.7f && AttackTimer < ChargeDelay)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(30f, 30f);
                    Dust.NewDustPerfect(endOfCannon + offset, 234, Main.rand.NextVector2Circular(5f, 5f), 0, default, 1.35f).noGravity = true;
                    Dust.NewDustPerfect(endOfCannon - offset, 234, Main.rand.NextVector2Circular(5f, 5f), 0, default, 1.35f).noGravity = true;
                }
            }

            // Fire a pulse blast.
            if (AttackTimer >= ChargeDelay && AttackTimer % shootRate == shootRate - 1f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulseRifleFire"), npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 blastShootVelocity = aimDirection * blastShootSpeed;
                    Vector2 blastSpawnPosition = endOfCannon + blastShootVelocity * 8.4f;
                    Utilities.NewProjectileBetter(blastSpawnPosition, blastShootVelocity, ModContent.ProjectileType<AresPulseBlast>(), projectileDamageBoost + 500, 0f);

                    npc.netUpdate = true;
                }
            }

            // Reset the attack timer after an attack cycle ends.
            if (AttackTimer >= ChargeDelay + shootTime)
            {
                AttackTimer = 0f;
                npc.netUpdate = true;
            }
            AttackTimer++;
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCProjectiles.Add(index);
        }

        public override void FindFrame(int frameHeight)
        {
            int currentFrame = (int)Math.Round(MathHelper.Lerp(0f, 35f, npc.ai[0] / npc.ai[1]));

            if (npc.ai[0] > npc.ai[1])
            {
                npc.frameCounter++;
                if (npc.frameCounter >= 66f)
                    npc.frameCounter = 0D;
                currentFrame = (int)Math.Round(MathHelper.Lerp(36f, 47f, (float)npc.frameCounter / 66f));
            }
            else
                npc.frameCounter = 0D;

            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(npc))
                currentFrame = (int)Math.Round(MathHelper.Lerp(0f, 35f, npc.ai[0] % 72f / 72f));

            npc.frame = new Rectangle(currentFrame / 12 * 150, currentFrame % 12 * 148, 150, 148);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (npc.Infernum().OptionalPrimitiveDrawer is null)
            {
                npc.Infernum().OptionalPrimitiveDrawer = new PrimitiveTrailCopy(completionRatio => AresBodyBehaviorOverride.FlameTrailWidthFunctionBig(npc, completionRatio),
                    completionRatio => AresBodyBehaviorOverride.FlameTrailColorFunctionBig(npc, completionRatio),
                    null, true, GameShaders.Misc["Infernum:TwinsFlameTrail"]);
            }

            for (int i = 0; i < 2; i++)
            {
                if (npc.Infernum().ExtraAI[0] > 0f)
                    npc.Infernum().OptionalPrimitiveDrawer.Draw(npc.oldPos, npc.Size * 0.5f - Main.screenPosition, 54);
            }

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
