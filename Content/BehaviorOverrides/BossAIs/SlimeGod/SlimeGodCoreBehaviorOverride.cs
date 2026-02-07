using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.UI;
using InfernumMode.Assets.BossTextures;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.SlimeGod.SlimeGodComboAttackManager;
using CrimulanSGBig = CalamityMod.NPCs.SlimeGod.CrimulanPaladin;
using EbonianSGBig = CalamityMod.NPCs.SlimeGod.EbonianPaladin;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SlimeGod
{
    public class SlimeGodCoreBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SlimeGodCore>();

        #region Enumerations
        public enum SlimeGodCoreAttackType
        {
            HoverAndDoNothing,
            DoAbsolutelyNothing,
            PhaseTransitionAnimation,
            SpinBursts,
            HorizontalCharges,
            VerticalHoverBursts
        }
        #endregion

        #region Loading
        public override void Load()
        {
            GlobalNPCOverrides.OnKillEvent += MakeSplitSlimesCreateDeathStuff;
        }

        private void MakeSplitSlimesCreateDeathStuff(NPC npc)
        {
            bool bigSlimeGod = npc.type == ModContent.NPCType<EbonianSGBig>() || npc.type == ModContent.NPCType<CrimulanSGBig>();
            if (bigSlimeGod && Registered(npc.type))
            {
                for (int i = 0; i < 12; i++)
                {
                    int slime = NPC.NewNPC(npc.GetSource_Death(), (int)npc.Center.X, (int)npc.Center.Y, npc.type, ModContent.NPCType<SplitBigSlimeAnimation>());
                    Main.npc[slime].velocity = Main.rand.NextVector2Circular(8f, 8f);
                }

                SelectNextAttackSpecific(LeaderOfFight);
            }
        }
        #endregion Loading

        #region AI

        public static bool AnyLargeSlimes => NPC.AnyNPCs(ModContent.NPCType<CrimulanSGBig>()) || NPC.AnyNPCs(ModContent.NPCType<EbonianSGBig>());

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 44;
            npc.height = 44;
            npc.scale = CoreBaseScale;
            npc.defense = 6;
            npc.Opacity = 0.8f;
        }

        public override void BossHeadSlot(NPC npc, ref int index)
        {
            index = ModContent.GetModBossHeadSlot(BossTextureRegistry.SlimeGodCoreMapIcon);
        }

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // These debuffs are not fun.
            if (target.HasBuff(BuffID.VortexDebuff))
                target.ClearBuff(BuffID.VortexDebuff);
            if (target.HasBuff(BuffID.Cursed))
                target.ClearBuff(BuffID.Cursed);

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float backglowInterpolant = ref npc.localAI[0];

            // Summon the big slime.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[3] == 0f)
            {
                int slimeGodID = WorldGen.crimson ? ModContent.NPCType<CrimulanSGBig>() : ModContent.NPCType<EbonianSGBig>();
                int fuck = NPC.NewNPC(npc.GetSource_FromAI(), (int)target.Center.X - 500, (int)target.Center.Y - 750, slimeGodID);
                Main.npc[fuck].velocity = Vector2.UnitY * 8f;
                npc.scale = CoreBaseScale;
                npc.localAI[3] = 1f;
            }

            // Reset damage.
            npc.damage = npc.defDamage;

            // Don't take damage if any large slimes are present.
            npc.dontTakeDamage = AnyLargeSlimes;

            // Disappear if the target is gone.
            npc.timeLeft = 3600;
            if (!target.active || target.dead || !npc.WithinRange(target.Center, 8400f))
            {
                npc.TargetClosest();
                target = Main.player[npc.target];
                if (!target.active || target.dead || !npc.WithinRange(target.Center, 8400f))
                    npc.active = false;
            }

            // Set the universal whoAmI variable.
            CalamityGlobalNPC.slimeGod = npc.whoAmI;

            // Update the boss HP bar.
            BossHealthBarManager.BossExclusionList.Remove(npc.type);

            npc.boss = FightState == SlimeGodFightState.CorePhase;
            npc.Calamity().CanHaveBossHealthBar = npc.boss;
            npc.Calamity().ShouldCloseHPBar = !npc.boss;

            if (npc.ai[0] <= 1f)
            {
                bool transitionToNextAttack = FightState == SlimeGodFightState.CorePhase;
                if (npc.ai[0] != (int)SlimeGodCoreAttackType.HoverAndDoNothing)
                    transitionToNextAttack |= attackTimer >= 480f;

                // The 2 check is because it takes a frame for the global NPC indices to be populated, meaning the fight state technically has a delay.
                if (Main.netMode != NetmodeID.MultiplayerClient && transitionToNextAttack && attackTimer >= 2f)
                    SelectNextAttack(npc);
            }

            switch ((SlimeGodCoreAttackType)(int)attackState)
            {
                case SlimeGodCoreAttackType.HoverAndDoNothing:
                    DoBehavior_HoverAndDoNothing(npc, target);
                    break;
                case SlimeGodCoreAttackType.PhaseTransitionAnimation:
                    DoBehavior_PhaseTransitionAnimation(npc, ref attackTimer, ref backglowInterpolant);
                    break;
                case SlimeGodCoreAttackType.SpinBursts:
                    DoBehavior_SpinBursts(npc, target, ref attackTimer);
                    break;
                case SlimeGodCoreAttackType.HorizontalCharges:
                    DoBehavior_HorizontalCharges(npc, target, ref attackTimer);
                    break;
                case SlimeGodCoreAttackType.VerticalHoverBursts:
                    DoBehavior_VerticalHoverBursts(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_HoverAndDoNothing(NPC npc, Player target)
        {
            // Disable contact damage.
            npc.damage = 0;

            // Hover above the target.
            Vector2 destination = target.Center - Vector2.UnitY * 420f;
            if (!npc.WithinRange(destination, 90f))
            {
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 13.5f, 0.85f);
                npc.rotation = npc.velocity.X * 0.15f;
            }
            else
            {
                if (npc.velocity.Length() > 4.5f)
                    npc.velocity *= 0.97f;
                npc.rotation += npc.velocity.X * 0.04f;
            }
        }

        public static void DoBehavior_PhaseTransitionAnimation(NPC npc, ref float attackTimer, ref float backglowInterpolant)
        {
            int dustAnimationTime = 155;

            // Wait until all split slimes are gone.
            if (NPC.AnyNPCs(ModContent.NPCType<SplitBigSlimeAnimation>()))
                attackTimer--;

            // Disable contact damage.
            npc.damage = 0;

            // Decide the backglow interpolant.
            backglowInterpolant = Utils.GetLerpValue(dustAnimationTime / 2f - 20f, dustAnimationTime - 1f, attackTimer, true);

            // Spin 2 win.
            float spinSpeed = Utils.Remap(attackTimer, 15f, dustAnimationTime - 15f, 0f, Pi / 24f);
            npc.rotation += spinSpeed;
            npc.velocity *= 0.95f;

            // Destroy any and all stray projectiles.
            if (attackTimer == 2f)
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<DeceleratingEbonianGlob>(), ModContent.ProjectileType<DeceleratingCrimulanGlob>(), ModContent.ProjectileType<GroundSlimeGlob>());

            // Move the camera to the core and draw in slime from outside sources.
            if (npc.WithinRange(Main.LocalPlayer.Center, 2000f) && attackTimer < 150f)
            {
                Main.LocalPlayer.Infernum_Camera().ScreenFocusPosition = npc.Center;
                Main.LocalPlayer.Infernum_Camera().ScreenFocusInterpolant = Utils.GetLerpValue(0f, 24f, attackTimer, true);
                Main.LocalPlayer.Infernum_Camera().ScreenFocusInterpolant *= Utils.GetLerpValue(dustAnimationTime, dustAnimationTime - 10f, attackTimer, true);

                float offsetAngle = Pow(attackTimer / dustAnimationTime, 4.2f) * Pi;
                float dustOffsetRadius = Lerp(24f, 300f, attackTimer / dustAnimationTime);
                for (int i = 0; i < 6; i++)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        Vector2 dustSpawnOffset = -Vector2.UnitY.RotatedBy(offsetAngle + TwoPi * i / 6f) * dustOffsetRadius + Main.rand.NextVector2Circular(20f, 20f);
                        Vector2 dustSpawnVelocity = dustSpawnOffset * -0.1f;
                        Dust slime = Dust.NewDustPerfect(npc.Center + dustSpawnOffset, DustID.Crimslime, dustSpawnVelocity);
                        slime.color = Color.Lerp(Color.Red, Color.Purple, i / 2f);
                        slime.color = Color.Lerp(slime.color, Color.Gray, 0.525f);
                        slime.color.A = 100;
                        slime.alpha = 100;
                        slime.scale = Main.rand.NextFloat(1.185f, 1.5f);
                        slime.fadeIn = 0.5f;
                        slime.noGravity = true;
                    }
                }
            }

            // Explode into slime before transitioning to the next attack.
            if (attackTimer == dustAnimationTime - 16f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/SlimeGodPossession"), npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 slimeVelocity = Main.rand.NextVector2Circular(12f, 12f);
                        Utilities.NewProjectileBetter(npc.Center, slimeVelocity, ModContent.ProjectileType<GroundSlimeGlob>(), 0, 0f);
                    }
                }
            }

            if (attackTimer >= dustAnimationTime + 30f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SpinBursts(NPC npc, Player target, ref float attackTimer)
        {
            int spinTime = 180;
            int chargeTime = 54;
            int slowdownTime = 30;
            int burstShootRate = 35;
            int blobsInBurst = 8;
            float blobShootSpeed = 6.5f;
            ref float spinAngleOffset = ref npc.Infernum().ExtraAI[0];

            // Initialize the spin angle to make it randomized.
            if (attackTimer == 1f)
            {
                spinAngleOffset = Main.rand.NextFloat(TwoPi);
                npc.netUpdate = true;
            }

            // Spin 2 win.
            if (attackTimer < spinTime)
            {
                // Disable contact damage.
                npc.damage = 0;

                Vector2 destination = target.Center + spinAngleOffset.ToRotationVector2() * 420f;
                npc.Center = npc.Center.MoveTowards(destination, 32f);

                spinAngleOffset += TwoPi * Utils.GetLerpValue(170f, 150f, attackTimer, true) / 90f;
                npc.rotation += spinAngleOffset * 0.3f;

                if (attackTimer % burstShootRate == burstShootRate - 1f && !npc.WithinRange(target.Center, 275f))
                {
                    SoundEngine.PlaySound(SoundID.Item171, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float offsetAngle = Main.rand.NextFloat(TwoPi);
                        for (int i = 0; i < blobsInBurst; i++)
                        {
                            int globID = Main.rand.NextBool() ? ModContent.ProjectileType<DeceleratingCrimulanGlob>() : ModContent.ProjectileType<DeceleratingEbonianGlob>();
                            Vector2 blobShootVelocity = (TwoPi * i / blobsInBurst + offsetAngle).ToRotationVector2() * blobShootSpeed;
                            Utilities.NewProjectileBetter(npc.Center, blobShootVelocity, globID, SlimeGlobDamage, 0f);
                        }
                    }
                }
            }

            // Do the charge. This also releases bursts of slime.
            if (attackTimer == spinTime)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * 19.75f;

                SoundEngine.PlaySound(SoundID.Item171, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int globID = Main.rand.NextBool() ? ModContent.ProjectileType<DeceleratingCrimulanGlob>() : ModContent.ProjectileType<DeceleratingEbonianGlob>();
                        float shootOffsetAngle = Lerp(-0.4f, 0.4f, i / 2f);
                        Vector2 blobShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(shootOffsetAngle) * blobShootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, blobShootVelocity, globID, SlimeGlobDamage, 0f);
                    }
                }
                npc.netUpdate = true;
            }

            if (attackTimer > spinTime)
                npc.rotation += npc.velocity.X * 0.05f;

            if (attackTimer > spinTime + chargeTime)
                npc.velocity *= 0.98f;

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= spinTime + chargeTime + slowdownTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HorizontalCharges(NPC npc, Player target, ref float attackTimer)
        {
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Line up for the charge.
            if (attackSubstate == 0f)
            {
                float verticalOffsetLeniance = 65f;
                float flySpeed = 14.5f;
                float flyInertia = 4f;
                float horizontalOffset = 720f;
                if (BossRushEvent.BossRushActive)
                    flySpeed *= 2.15f;

                Vector2 destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * horizontalOffset;

                // Fly towards the destination beside the player.
                npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(destination) * flySpeed) / flyInertia;

                // If within a good approximation of the player's position, prepare charging.
                if (Math.Abs(npc.Center.X - target.Center.X) > horizontalOffset - 50f && Math.Abs(npc.Center.Y - target.Center.Y) < verticalOffsetLeniance)
                {
                    attackSubstate = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Prepare for the charge.
            if (attackSubstate == 1f)
            {
                int chargeDelay = 30;
                float flySpeed = 14.5f;
                float flyInertia = 8f;
                if (BossRushEvent.BossRushActive)
                    flySpeed *= 2.15f;

                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity) / flyInertia;

                if (attackTimer >= chargeDelay)
                {
                    attackTimer = 0f;
                    attackSubstate = 2f;
                    npc.velocity = chargeVelocity;
                    if (Main.rand.NextBool(3))
                        npc.velocity *= 1.5f;

                    npc.netUpdate = true;
                }
            }

            // Do the actual charge.
            if (attackSubstate == 2f)
            {
                if (npc.velocity.Length() < 20f)
                    npc.velocity *= 1.04f;

                // Release abyss balls upward.
                if (attackTimer % 8f == 7f)
                {
                    SoundEngine.PlaySound(SoundID.Item171, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY * 6f, ModContent.ProjectileType<DeceleratingEbonianGlob>(), SlimeGlobDamage, 0f);
                        Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY * 6f, ModContent.ProjectileType<DeceleratingCrimulanGlob>(), SlimeGlobDamage, 0f);
                    }
                }

                npc.rotation += (npc.velocity.X > 0f).ToDirectionInt() * 0.15f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 64f)
                {
                    attackTimer = 0f;
                    SelectNextAttack(npc);
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_VerticalHoverBursts(NPC npc, Player target, ref float attackTimer)
        {
            int blobShootRate = 42;
            int blobsPerBurst = 5;
            int shootDelay = 90;
            int shootTime = 360;
            float blobShootSpeed = 7f;

            // Hover above the target.
            Vector2 destination = target.Center - Vector2.UnitY * 400f;
            if (!npc.WithinRange(destination, 155f))
            {
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 14.5f, 0.925f);
                npc.rotation = npc.velocity.X * 0.15f;
            }
            else
            {
                if (npc.velocity.Length() > 4.5f)
                    npc.velocity *= 0.97f;
                npc.velocity.Y *= 0.9f;
                npc.rotation += npc.velocity.X * 0.04f;
            }

            // Shoot bursts of blobs.
            if (attackTimer >= shootDelay && attackTimer % blobShootRate == blobShootRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item171, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < blobsPerBurst; i++)
                    {
                        int globID = Main.rand.NextBool() ? ModContent.ProjectileType<DeceleratingCrimulanGlob>() : ModContent.ProjectileType<DeceleratingEbonianGlob>();
                        float shootOffsetAngle = Lerp(-0.75f, 0.75f, i / (blobsPerBurst - 1f));
                        Vector2 blobShootVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 20f).RotatedBy(shootOffsetAngle) * blobShootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, blobShootVelocity, globID, SlimeGlobDamage, 0f);
                    }
                }
            }

            if (attackTimer >= shootTime + shootDelay)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.TargetClosest();

            float oldLocalState = npc.ai[0];
            ref float localState = ref npc.ai[0];

            int tries = 0;
            WeightedRandom<SlimeGodCoreAttackType> newStatePicker = new(Main.rand);

            if (FightState != SlimeGodFightState.CorePhase)
                newStatePicker.Add(SlimeGodCoreAttackType.HoverAndDoNothing);
            else
            {
                newStatePicker.Add(SlimeGodCoreAttackType.SpinBursts);
                newStatePicker.Add(SlimeGodCoreAttackType.HorizontalCharges);
                newStatePicker.Add(SlimeGodCoreAttackType.VerticalHoverBursts);
            }

            do
            {
                localState = (int)newStatePicker.Get();
                tries++;
            }
            while (localState == oldLocalState && tries < 1000);

            // Do the final phase animation if the previous attack was just an idle hover.
            if (npc.Infernum().ExtraAI[7] == 0f && FightState == SlimeGodFightState.CorePhase)
            {
                localState = (int)SlimeGodCoreAttackType.PhaseTransitionAnimation;
                npc.Infernum().ExtraAI[7] = 1f;
            }

            // Do a spin after the final phase animation.
            else if (npc.Infernum().ExtraAI[7] == 1f)
            {
                localState = (int)SlimeGodCoreAttackType.SpinBursts;
                npc.Infernum().ExtraAI[7] = 2f;
            }
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI

        #region Drawing

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            NPCID.Sets.TrailCacheLength[npc.type] = 8;

            ref float afterimageCount = ref npc.Infernum().ExtraAI[6];

            Texture2D slimeGodTexture = BossTextureRegistry.SlimeGodCore.Value;
            Vector2 origin = npc.frame.Size() * 0.5f;
            void DrawCoreInstance(Color color, Vector2 drawPosition, int direction, bool backglow)
            {
                SpriteEffects spriteEffects = direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                color = npc.GetAlpha(color);

                if (npc.localAI[0] > 0f && backglow)
                {
                    float drawOffsetFactor = npc.localAI[0] * 5f;
                    Color backimageColor = lightColor.MultiplyRGB(Color.Red) * npc.localAI[0] * 0.4f;
                    backimageColor.A = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 drawOffset = (TwoPi * i / 8f).ToRotationVector2() * drawOffsetFactor;
                        Main.spriteBatch.Draw(slimeGodTexture, drawPosition + drawOffset - Main.screenPosition, npc.frame, backimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                    }
                }

                Main.spriteBatch.Draw(slimeGodTexture, drawPosition - Main.screenPosition, npc.frame, color, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            }

            int idealAfterimageCount = 5;
            if (afterimageCount != idealAfterimageCount)
                afterimageCount += Math.Sign(idealAfterimageCount - afterimageCount);

            // Draw afterimages.
            if (afterimageCount > 0)
            {
                if (npc.oldPos.Length < afterimageCount + 1)
                {
                    npc.oldPos = new Vector2[(int)afterimageCount + 1];
                    npc.oldRot = new float[(int)afterimageCount + 1];
                }

                for (int i = (int)afterimageCount; i >= 1; i--)
                {
                    Color afterimageColor = lightColor.MultiplyRGB(Color.White) * Pow(1f - i / (float)afterimageCount, 3f);
                    DrawCoreInstance(afterimageColor, npc.oldPos[i] + npc.Size * 0.5f, npc.spriteDirection, false);
                }
            }
            DrawCoreInstance(lightColor, npc.Center, npc.spriteDirection, true);
            return false;
        }
        #endregion Drawing

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.SlimeGodTip1";
            yield return n =>
            {
                if (FightState == SlimeGodFightState.CorePhase)
                    return "Mods.InfernumMode.PetDialog.SlimeGodTip2";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
