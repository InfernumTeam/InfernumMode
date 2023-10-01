using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.Projectiles.Typeless;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.DoG.DoGPhase1HeadBehaviorOverride;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.DoG.DoGPhase2HeadBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DoG
{
    public class DoGPhase1BodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DevourerofGodsBody>();

        private static readonly FieldInfo invincibilityTimeField = typeof(DevourerofGodsBody).GetField("invinceTime", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo bodyPhase2StartedField = typeof(DevourerofGodsBody).GetField("phase2Started", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo tailPhase2StartedField = typeof(DevourerofGodsTail).GetField("phase2Started", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo bodyPhase2StartedField2 = typeof(DevourerofGodsBody).GetField("Phase2Started", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo tailPhase2StartedField2 = typeof(DevourerofGodsTail).GetField("Phase2Started", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void DoGSegmentAI(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            NPC head = Main.npc[(int)npc.ai[2]];
            npc.life = head.life;
            npc.lifeMax = head.lifeMax;
            npc.defense = BodySegmentDefense;
            npc.Calamity().DR = BodySegmentDR;
            if (!head.active || CalamityGlobalNPC.DoGHead < 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return;
            }

            // Why are you despawning? What the heck is wrong with worms?
            npc.timeLeft = 7200;

            if (head.Infernum().ExtraAI[DamageImmunityCountdownIndex] > 0f)
            {
                npc.Calamity().DR = 0.9999999f;
                npc.Calamity().unbreakableDR = true;
            }

            // FUCK YOU stupid debuffs! GO FUCK YOURSELF!
            KillUnbalancedDebuffs(npc);

            // Inherit various attributes from the head segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.scale = aheadSegment.scale;

            // Reset sizes if the head has transitioned to phase 2 but this segment has yet to inherit that property.
            if (npc.Infernum().ExtraAI[InPhase2FlagIndex] == 0f && InPhase2)
            {
                if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                {
                    npc.width = 120;
                    npc.height = 120;
                    npc.frame = new Rectangle(0, 0, 142, 126);
                    bodyPhase2StartedField?.SetValue(npc.ModNPC, true);
                    bodyPhase2StartedField2?.SetValue(npc.ModNPC, true);
                }
                else
                {
                    npc.width = 100;
                    npc.height = 100;
                    npc.frame = new Rectangle(0, 0, 106, 200);
                    tailPhase2StartedField?.SetValue(npc.ModNPC, true);
                    tailPhase2StartedField2?.SetValue(npc.ModNPC, true);
                }
            }

            // Inherit the phase 2 state from the head.
            npc.Infernum().ExtraAI[InPhase2FlagIndex] = head.Infernum().ExtraAI[InPhase2FlagIndex];

            float worldCheckFluff = 6001f;
            bool headOutOfWorld = head.Center.X < -worldCheckFluff || head.Center.X > Main.maxTilesX * 16f + worldCheckFluff ||
                head.Center.Y < -worldCheckFluff || head.Center.Y > Main.maxTilesY * 16f + worldCheckFluff;
            bool isTail = npc.type == ModContent.NPCType<DevourerofGodsTail>();
            Player target = Main.player[head.target];

            // Handle transitions once the tail enters the transition portal.
            if (!InPhase2 && (GeneralPortalIndex >= 0f || headOutOfWorld))
            {
                if (headOutOfWorld || npc.Hitbox.Intersects(Main.projectile[GeneralPortalIndex].Hitbox))
                {
                    npc.alpha += 140;
                    if (npc.alpha >= 255)
                    {
                        npc.alpha = 255;

                        if (isTail)
                        {
                            InPhase2 = true;
                            CurrentPhase2TransitionState = Phase2TransitionState.NotEnteringPhase2;
                        }
                    }
                }
            }
            else
            {
                // Do what the head says in regards to opacity inheritance if not doing phase two transition stuff.
                switch ((BodySegmentFadeType)(int)head.Infernum().ExtraAI[BodySegmentFadeTypeIndex])
                {
                    case BodySegmentFadeType.EnteringPortal:
                        if (GeneralPortalIndex >= 0f && npc.Hitbox.Intersects(Main.projectile[GeneralPortalIndex].Hitbox))
                            npc.Opacity = Clamp(npc.Opacity - 0.275f, 0f, 1f);

                        // Update the surprise portal attack state if the tail has entered the portal.
                        bool performTeleportTransition = npc.Opacity <= 0f || !npc.WithinRange(target.Center, 20000f);
                        if (isTail && SurprisePortalAttackState == PerpendicularPortalAttackState.EnteringPortal && performTeleportTransition)
                        {
                            SurprisePortalAttackState = PerpendicularPortalAttackState.Waiting;
                            foreach (Projectile portal in Utilities.AllProjectilesByID(ModContent.ProjectileType<DoGChargeGate>()))
                            {
                                portal.ModProjectile<DoGChargeGate>().Time = (int)portal.ModProjectile<DoGChargeGate>().Lifetime - 45;
                                portal.netUpdate = true;
                            }

                            npc.netUpdate = true;
                            head.netUpdate = true;
                        }

                        break;

                    case BodySegmentFadeType.InhertHeadOpacity:
                        npc.Opacity = head.Opacity;
                        break;

                    case BodySegmentFadeType.ApproachAheadSegmentOpacity:
                        if (aheadSegment.Opacity < 0.2f)
                            npc.Opacity = 0f;
                        if (aheadSegment.Opacity > npc.Opacity)
                        {
                            npc.Opacity = Lerp(npc.Opacity, aheadSegment.Opacity, 0.67f);
                            if (aheadSegment.Opacity >= 1f)
                                npc.Opacity = Lerp(npc.Opacity, aheadSegment.Opacity, 0.67f);
                        }
                        break;
                }
            }

            // Reset the invicibility time variable used in the vanilla AI.
            if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                invincibilityTimeField.SetValue(npc.ModNPC, 0);

            // Decide segment size stuff.
            Vector2 size = npc.Size;
            if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                size = Vector2.One * 102f;
            if (npc.type == ModContent.NPCType<DevourerofGodsTail>())
                size = InPhase2 ? Vector2.One * 84f : Vector2.One * 66f;

            if (npc.Size != size)
                npc.Size = size;

            npc.dontTakeDamage = head.dontTakeDamage || npc.Opacity < 0.1f;
            npc.damage = npc.dontTakeDamage || head.damage <= 0 ? 0 : npc.defDamage;

            // Always use max HP. This doesn't affect the worm as a whole, but it does prevent problems in the death animation where segments otherwise just disappear when killed.
            npc.life = npc.lifeMax;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
            {
                directionToNextSegment = directionToNextSegment.RotatedBy(WrapAngle(aheadSegment.rotation - npc.rotation) * 0.08f);
                directionToNextSegment = directionToNextSegment.MoveTowards((aheadSegment.rotation - npc.rotation).ToRotationVector2(), 1f);
                if (SurprisePortalAttackState != PerpendicularPortalAttackState.NotPerformingAttack)
                    npc.rotation = aheadSegment.rotation;
            }

            // Decide segment offset stuff.
            float segmentOffset = 100f;
            if (InPhase2)
            {
                if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                    segmentOffset = 80f;
                if (npc.type == ModContent.NPCType<DevourerofGodsTail>())
                    segmentOffset = 120f;
            }
            else
            {
                if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                    segmentOffset = 100f;
                if (npc.type == ModContent.NPCType<DevourerofGodsTail>())
                    segmentOffset = 98f;
            }
            npc.rotation = directionToNextSegment.ToRotation() + PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.scale * segmentOffset;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();
        }

        public static void KillUnbalancedDebuffs(NPC npc)
        {
            // Check out NPCDebuffs.cs as this function sets the debuff immunities for all enemies in Cal bar the ones described below.
            npc.SetDebuffImmunities();

            for (int i = 0; i < npc.buffImmune.Length; i++)
                npc.buffImmune[i] = true;

            // Most bosses and boss servants are not immune to Kami Flu.
            npc.buffImmune[ModContent.BuffType<KamiFlu>()] = false;

            // Nothing should be immune to Enraged.
            npc.buffImmune[ModContent.BuffType<Enraged>()] = false;
        }

        public override bool PreAI(NPC npc)
        {
            DoGSegmentAI(npc);
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (InPhase2)
            {
                npc.scale = 1f;

                float antimatterFade = FadeToAntimatterForm;
                Texture2D bodyTexture2 = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Body").Value;
                Texture2D glowmaskTexture2 = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2BodyGlow").Value;
                Texture2D bodyTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2BodyAntimatter").Value;
                Texture2D glowmaskTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2BodyGlowAntimatter").Value;
                Vector2 drawPosition2 = npc.Center - Main.screenPosition;
                Vector2 origin2 = bodyTexture2.Size() * 0.5f;

                // Draw back afterimages when the antimatter effect is ongoing.
                float backTexturePulse1 = (Main.GlobalTimeWrappedHourly * 0.43f + npc.whoAmI * 0.13f) % 1f;
                float backTexturePulse2 = (Main.GlobalTimeWrappedHourly * 0.31f + npc.whoAmI * 0.09f) % 1f;
                Color c1 = Color.Cyan;
                Color c2 = Color.Fuchsia;
                c1.A = 84;
                c2.A = 92;
                Main.spriteBatch.Draw(bodyTexture2Antimatter, drawPosition2, null, npc.GetAlpha(c1) * antimatterFade * (1f - backTexturePulse1) * 0.84f, npc.rotation, origin2, npc.scale * (1f + backTexturePulse1 * 0.4f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(bodyTexture2Antimatter, drawPosition2, null, npc.GetAlpha(c2) * antimatterFade * (1f - backTexturePulse2) * 0.6f, npc.rotation, origin2, npc.scale * (1f + backTexturePulse2 * 1.2f), SpriteEffects.None, 0f);

                Main.spriteBatch.Draw(bodyTexture2, drawPosition2, null, npc.GetAlpha(lightColor) * (1f - antimatterFade), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glowmaskTexture2, drawPosition2, null, npc.GetAlpha(Color.White) * (1f - antimatterFade), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(bodyTexture2Antimatter, drawPosition2, null, npc.GetAlpha(Color.White) * antimatterFade, npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glowmaskTexture2Antimatter, drawPosition2, null, npc.GetAlpha(Color.White) * antimatterFade, npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                return false;
            }

            Texture2D bodyTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP1Body").Value;
            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP1BodyGlowmask").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = bodyTexture.Size() * 0.5f;

            Main.spriteBatch.Draw(bodyTexture, drawPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowmaskTexture, drawPosition, null, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class DoGPhase1TailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DevourerofGodsTail>();

        public override bool PreAI(NPC npc)
        {
            DoGPhase1BodyBehaviorOverride.DoGSegmentAI(npc);
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (InPhase2)
            {
                npc.scale = 1f;

                float antimatterFade = FadeToAntimatterForm;
                Texture2D tailTexture2 = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Tail").Value;
                Texture2D glowmaskTexture2 = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2TailGlow").Value;
                Texture2D tailTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2TailAntimatter").Value;
                Texture2D glowmaskTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2TailGlowAntimatter").Value;
                Vector2 drawPosition2 = npc.Center - Main.screenPosition;
                Vector2 origin2 = tailTexture2.Size() * 0.5f;

                // Draw back afterimages when the antimatter effect is ongoing.
                float backTexturePulse1 = (Main.GlobalTimeWrappedHourly * 0.43f + npc.whoAmI * 0.13f) % 1f;
                float backTexturePulse2 = (Main.GlobalTimeWrappedHourly * 0.31f + npc.whoAmI * 0.09f) % 1f;
                Color c1 = Color.Cyan;
                Color c2 = Color.Fuchsia;
                c1.A = 84;
                c2.A = 92;
                Main.spriteBatch.Draw(tailTexture2Antimatter, drawPosition2, null, npc.GetAlpha(c1) * antimatterFade * (1f - backTexturePulse1) * 0.84f, npc.rotation, origin2, npc.scale * (1f + backTexturePulse1 * 0.4f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(tailTexture2Antimatter, drawPosition2, null, npc.GetAlpha(c2) * antimatterFade * (1f - backTexturePulse2) * 0.6f, npc.rotation, origin2, npc.scale * (1f + backTexturePulse2 * 1.2f), SpriteEffects.None, 0f);

                Main.spriteBatch.Draw(tailTexture2, drawPosition2, null, npc.GetAlpha(lightColor) * (1f - antimatterFade), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glowmaskTexture2, drawPosition2, null, npc.GetAlpha(Color.White) * (1f - antimatterFade), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(tailTexture2Antimatter, drawPosition2, null, npc.GetAlpha(Color.White) * antimatterFade, npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glowmaskTexture2Antimatter, drawPosition2, null, npc.GetAlpha(Color.White) * antimatterFade, npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                return false;
            }

            Texture2D tailTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP1Tail").Value;
            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP1TailGlowmask").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = tailTexture.Size() * 0.5f;

            Main.spriteBatch.Draw(tailTexture, drawPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowmaskTexture, drawPosition, null, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
