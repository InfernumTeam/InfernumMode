using CalamityMod;
using CalamityMod.NPCs.AdultEidolonWyrm;
using InfernumMode.OverridingSystem;
using InfernumMode.Projectiles;
using InfernumMode.Sounds;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum AEWAttackType
        {
            // Spawn animation states.
            SnatchTerminus,
            ThreateninglyHoverNearPlayer,

            // Neutral attacks.
            SplitFormCharges,
        }

        public override int NPCOverrideType => ModContent.NPCType<AdultEidolonWyrmHead>();

        #region AI
        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio,
            Phase4LifeRatio,
            Phase5LifeRatio
        };

        public const float Phase2LifeRatio = 0.8f;

        public const float Phase3LifeRatio = 0.6f;

        public const float Phase4LifeRatio = 0.35f;

        public const float Phase5LifeRatio = 0.1f;

        public const int EyeGlowOpacityIndex = 5;

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.08f, 0f, 1f);

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float initializedFlag = ref npc.ai[2];
            ref float eyeGlowOpacity = ref npc.Infernum().ExtraAI[EyeGlowOpacityIndex];

            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                CreateSegments(npc, 125, ModContent.NPCType<AdultEidolonWyrmBody>(), ModContent.NPCType<AdultEidolonWyrmBodyAlt>(), ModContent.NPCType<AdultEidolonWyrmTail>());
                initializedFlag = 1f;
                npc.netUpdate = true;
            }
            
            // If there still was no valid target, swim away.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                DoBehavior_Despawn(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            // Disable obnoxious water mechanics so that the player can fight the boss without interruption.
            target.breath = target.breathMax;
            target.ignoreWater = true;
            target.wingTime = target.wingTimeMax;
            AbyssWaterColorSystem.WaterBlacknessInterpolant = 0f;

            // This is necessary to allow the boss effects buff to be shown.
            npc.Calamity().KillTime = 1;

            switch ((AEWAttackType)attackType)
            {
                case AEWAttackType.SnatchTerminus:
                    DoBehavior_SnatchTerminus(npc);
                    break;
                case AEWAttackType.ThreateninglyHoverNearPlayer:
                    DoBehavior_ThreateninglyHoverNearPlayer(npc, target, ref eyeGlowOpacity, ref attackTimer);
                    break;
                case AEWAttackType.SplitFormCharges:
                    DoBehavior_SplitFormCharges(npc, target, ref attackTimer);
                    break;
            }

            // Determine rotation based on the current velocity.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Increment the attack timer.
            attackTimer++;

            return false;
        }
        #endregion AI

        #region Specific Behaviors

        public static void DoBehavior_Despawn(NPC npc)
        {
            npc.velocity.X *= 0.985f;
            if (npc.velocity.Y < 33f)
                npc.velocity.Y += 0.6f;

            if (npc.timeLeft > 210)
                npc.timeLeft = 210;
        }

        public static void DoBehavior_SnatchTerminus(NPC npc)
        {
            float chargeSpeed = 41f;
            List<Projectile> terminusInstances = Utilities.AllProjectilesByID(ModContent.ProjectileType<TerminusAnimationProj>()).ToList();

            // Transition to the next attack if there are no more Terminus instances.
            if (terminusInstances.Count <= 0)
            {
                SelectNextAttack(npc);
                return;
            }

            Projectile target = terminusInstances.First();

            // Fly very, very quickly towards the Terminus.
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * chargeSpeed, 0.16f);

            // Delete the Terminus instance if it's being touched.
            // On the next frame the AEW will transition to the next attack, assuming there isn't another Terminus instance for some weird reason.
            if (npc.WithinRange(target.Center, 90f))
            {
                SoundEngine.PlaySound(SoundID.DD2_SkyDragonsFuryShot with { Volume = 1.3f }, target.Center);
                target.Kill();
            }
        }

        public static void DoBehavior_ThreateninglyHoverNearPlayer(NPC npc, Player target, ref float eyeGlowOpacity, ref float attackTimer)
        {
            int roarDelay = 60;
            int eyeGlowFadeinTime = 105;
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 450f, -360f);
            ref float hasReachedDestination = ref npc.Infernum().ExtraAI[0];

            // Attempt to hover to the top left/right of the target at first.
            if (hasReachedDestination == 0f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 32f, 0.084f);
                if (npc.WithinRange(hoverDestination, 96f))
                {
                    hasReachedDestination = 1f;
                    npc.netUpdate = true;
                }

                // Don't let the attack timer increment.
                attackTimer = -1f;

                return;
            }

            // Roar after a short delay.
            if (attackTimer == roarDelay)
                SoundEngine.PlaySound(InfernumSoundRegistry.AEWThreatenRoar);

            // Slow down and look at the target threateningly before attacking.
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 3f, 0.071f);

            // Make the eye glowmask gradually fade in.
            eyeGlowOpacity = Utils.GetLerpValue(0f, eyeGlowFadeinTime, attackTimer, true);
        }

        public static void DoBehavior_SplitFormCharges(NPC npc, Player target, ref float attackTimer)
        {
            
        }

        #endregion Specific Behaviors

        #region AI Utility Methods
        public static void CreateSegments(NPC npc, int wormLength, int bodyType1, int bodyType2, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                {
                    int bodyID = i % 2 == 0 ? bodyType1 : bodyType2;
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, bodyID, npc.whoAmI + 1);
                }
                else
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI + 1);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = npc.whoAmI;
                Main.npc[nextIndex].ai[1] = previousIndex;

                if (i >= 1)
                    Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        internal static void SelectNextAttack(NPC npc)
        {
            AEWAttackType currentAttack = (AEWAttackType)npc.ai[0];
            AEWAttackType nextAttack = currentAttack;

            if (currentAttack == AEWAttackType.SnatchTerminus)
                nextAttack = AEWAttackType.ThreateninglyHoverNearPlayer;
            else
            {
                nextAttack = AEWAttackType.SplitFormCharges;
            }

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.ai[0] = (int)nextAttack;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI Utility Methods

        #region Draw Effects
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            npc.frame = new(0, 0, 254, 138);

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/AdultEidolonWyrm/AdultEidolonWyrmHeadGlow").Value;
            Texture2D eyeTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/AdultEidolonWyrm/AEWEyes").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Color eyeColor = Color.Cyan * npc.Opacity * npc.Infernum().ExtraAI[EyeGlowOpacityIndex];

            Main.EntitySpriteDraw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0);
            Main.EntitySpriteDraw(glowmaskTexture, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0);
            ScreenSaturationBlurSystem.ThingsToDrawOnTopOfBlur.Add(new(eyeTexture, drawPosition, npc.frame, eyeColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0));

            // Hacky way of ensuring that PostDraw doesn't do anything.
            npc.frame = Rectangle.Empty;

            return false;
        }
        #endregion Draw Effects

        #region Tips

        #endregion Tips
    }
}
