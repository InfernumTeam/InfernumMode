using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BoC
{
    public class PlanteraBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Plantera;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        #region Enumerations
        internal enum PlanteraAttackState
        {
            BloomAwakening,
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            NPC.plantBoss = npc.whoAmI;

            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // Reset damage.
            npc.damage = npc.defDamage;

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead ||
                !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            npc.dontTakeDamage = !target.ZoneCrimson && !target.ZoneCorrupt;

            int hookCount = 3;
            bool enraged = target.Center.Y < Main.worldSurface * 16f;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hasCreatedHooksFlag = ref npc.localAI[0];
            ref float bulbHueInterpolant = ref npc.localAI[1];

            // Determine if should be invincible.
            npc.dontTakeDamage = enraged;

            // Summon weird leg tentacle hook things.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedHooksFlag == 0f)
            {
                for (int i = 0; i < hookCount; i++)
                    NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y + 4, NPCID.PlanterasHook, npc.whoAmI);

                hasCreatedHooksFlag = 1f;
            }

            // Used by hooks.
            npc.ai[3] = 1.25f;

            switch ((PlanteraAttackState)(int)attackType)
            {
                // The constitutes the first phase.
                case PlanteraAttackState.BloomAwakening:
                    DoAttack_BloomAwakening(npc, target, enraged, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        #region Specific Attacks
        internal static void DoDespawnEffects(NPC npc)
        {
            // Even if the player is dead it is still a valid index.
            float newSpeed = npc.velocity.Length() + 0.05f;
            Player oldTarget = Main.player[npc.target];

            npc.velocity = npc.SafeDirectionTo(oldTarget.Center) * -newSpeed;
            npc.damage = 0;

            if (npc.timeLeft > 60)
                npc.timeLeft = 60;

            if (!npc.WithinRange(oldTarget.Center, 4000f))
            {
                npc.life = 0;
                npc.checkDead();
                npc.active = false;
            }
        }

        internal static void DoAttack_BloomAwakening(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            npc.damage = 0;
            npc.rotation = npc.AngleFrom(target.Center) - MathHelper.PiOver2;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * 5f, 0.2f);

            // Cause petals to appear.
        }
        #endregion Specific Attacks

        #region AI Utility Methods

        internal const float Phase2LifeRatio = 0.8f;
        internal const float Phase3LifeRatio = 0.5f;
        internal static void GotoNextAttackState(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;

            PlanteraAttackState oldAttackType = (PlanteraAttackState)(int)npc.ai[0];
            PlanteraAttackState newAttackType = oldAttackType;
            switch (oldAttackType)
            {
                case PlanteraAttackState.BloomAwakening:
                    newAttackType = PlanteraAttackState.BloomAwakening;
                    break;
            }

            npc.ai[0] = (int)newAttackType;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        internal static bool DoTeleportFadeEffect(NPC npc, float time, Vector2 teleportDestination, int teleportFadeTime)
        {
            // Fade out and teleport after a bit.
            if (time <= teleportFadeTime)
            {
                npc.Opacity = MathHelper.Lerp(1f, 0f, time / teleportFadeTime);

                // Teleport when completely transparent.
                if (Main.netMode != NetmodeID.MultiplayerClient && time == teleportFadeTime)
                {
                    npc.Center = teleportDestination;

                    // And bring creepers along with because their re-adjustment motion in the base game is unpredictable and unpleasant.
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].type != NPCID.Creeper || !Main.npc[i].active)
                            continue;

                        Main.npc[i].Center = npc.Center + Main.rand.NextVector2CircularEdge(3f, 3f);
                        Main.npc[i].netUpdate = true;
                    }
                    npc.netUpdate = true;
                }
                npc.velocity *= 0.94f;
                return false;
            }

            // Fade back in after teleporting.
            if (time > teleportFadeTime && time <= teleportFadeTime * 1.5f)
                npc.Opacity = MathHelper.Lerp(0f, 1f, Utils.InverseLerp(teleportFadeTime, teleportFadeTime * 1.5f, time, true));
            return true;
        }
        #endregion AI Utility Methods

        #endregion AI

        #region Drawing

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter += 1.0;
            if (npc.frameCounter > 6.0)
            {
                npc.frameCounter = 0.0;
                npc.frame.Y += frameHeight;
            }

            if (npc.life > npc.lifeMax * Phase2LifeRatio)
            {
                if (npc.frame.Y > frameHeight * 3)
                    npc.frame.Y = 0;
            }
            else
            {
                if (npc.frame.Y <= frameHeight * 3)
                    npc.frame.Y = frameHeight * 3;

                if (npc.frame.Y >= frameHeight * 6)
                    npc.frame.Y = frameHeight * 3;
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Plantera/PlanteraTexture");
            Texture2D bulbTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Plantera/PlanteraBulbTexture");
            Color bulbColor = npc.GetAlpha(Color.Lerp(new Color(143, 215, 29), new Color(225, 128, 206), npc.localAI[1]).MultiplyRGB(lightColor));
            Color baseColor = npc.GetAlpha(lightColor);
            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;

            spriteBatch.Draw(texture, drawPosition, npc.frame, baseColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(bulbTexture, drawPosition, npc.frame, bulbColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
        #endregion
    }
}
