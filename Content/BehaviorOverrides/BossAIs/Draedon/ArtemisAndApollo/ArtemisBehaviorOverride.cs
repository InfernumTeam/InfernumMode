using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Artemis;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo.ApolloBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ArtemisBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<Artemis>();

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            ExoMechManagement.Phase3LifeRatio,
            ExoMechManagement.Phase4LifeRatio
        };

        #region Netcode Syncs

        public override void SendExtraData(NPC npc, ModPacket writer) => writer.Write(npc.Opacity);

        public override void ReceiveExtraData(NPC npc, BinaryReader reader) => npc.Opacity = reader.ReadSingle();

        #endregion Netcode Syncs

        #region AI
        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 204;
            npc.height = 226;
            npc.scale = 1f;
            npc.Opacity = 0f;
            npc.defense = 100;
            npc.DR_NERD(0.25f);
        }

        // Most attacks are present in Apollo's AI, since Apollo is supposed to be a "manager" for the twins, handling
        // things like NPC summoning and such, while Artemis primarily inherits attributes from Apollo.
        public override bool PreAI(NPC npc)
        {
            // Despawn if Apollo is not present.
            if (!Main.npc.IndexInRange(npc.realLife) || !Main.npc[npc.realLife].active)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    npc.timeLeft -= 100;
                    if (npc.timeLeft < 100)
                    {
                        npc.life = 0;
                        npc.HitEffect();
                        npc.active = false;
                    }
                    return false;
                }

                npc.life = 0;
                npc.active = false;
                return false;
            }

            // Define the life ratio.
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Define the whoAmI variable.
            CalamityGlobalNPC.draedonExoMechTwinRed = npc.whoAmI;

            // Define the Apollo NPC instance.
            NPC apollo = Main.npc[npc.realLife];

            // Define attack variables.
            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(apollo);
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hoverSide = ref npc.ai[2];
            ref float phaseTransitionAnimationTime = ref npc.ai[3];
            ref float frame = ref npc.localAI[0];
            ref float hasDoneInitializations = ref npc.localAI[1];
            ref float hasSummonedComplementMech = ref npc.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex];
            ref float complementMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];
            ref float wasNotInitialSummon = ref npc.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex];
            ref float finalMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];
            ref float finalPhaseAnimationTime = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];
            ref float sideSwitchAttackDelay = ref npc.Infernum().ExtraAI[ExoMechManagement.Twins_SideSwitchDelayIndex];
            ref float deathAnimationTimer = ref npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex];

            if (Main.netMode != NetmodeID.MultiplayerClient && hasDoneInitializations == 0f)
            {
                complementMechIndex = -1f;
                finalMechIndex = -1f;
                hasDoneInitializations = 1f;
                npc.netUpdate = true;
            }

            // Reset things and use variables from Apollo.
            npc.damage = 0;
            npc.defDamage = 520;
            npc.dontTakeDamage = apollo.dontTakeDamage;
            npc.target = apollo.target;
            npc.life = apollo.life;
            npc.lifeMax = apollo.lifeMax;
            npc.Calamity().DR = apollo.Calamity().DR;
            npc.Calamity().unbreakableDR = apollo.Calamity().unbreakableDR;

            // Inherit a bunch of things from Apollo, the "manager" of the twins' AI.
            TwinsAttackType apolloAttackType = (TwinsAttackType)(int)apollo.ai[0];
            if (apolloAttackType is not TwinsAttackType.ArtemisLaserRay and not TwinsAttackType.ApolloPlasmaCharges)
            {
                attackState = (int)apollo.ai[0];
                attackTimer = apollo.ai[1];
            }
            hoverSide = -apollo.ai[2];
            phaseTransitionAnimationTime = apollo.ai[3];
            hasSummonedComplementMech = apollo.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex];
            complementMechIndex = apollo.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];
            wasNotInitialSummon = apollo.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex];
            finalMechIndex = apollo.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];
            finalPhaseAnimationTime = apollo.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];
            sideSwitchAttackDelay = apollo.Infernum().ExtraAI[18];
            deathAnimationTimer = apollo.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex];
            npc.Calamity().newAI[0] = (int)Artemis.Phase.Charge;

            // Get a target.
            Player target = Main.player[npc.target];

            // Become more resistant to damage as necessary.
            npc.takenDamageMultiplier = 1f;
            if (ExoMechManagement.ShouldHaveSecondComboPhaseResistance(npc))
                npc.takenDamageMultiplier *= 0.5f;

            // Become invincible and disappear if necessary.
            npc.Calamity().newAI[1] = 0f;
            if (ExoMechAIUtilities.ShouldExoMechVanish(npc))
            {
                npc.Opacity = Clamp(npc.Opacity - 0.08f, 0f, 1f);
                if (npc.Opacity <= 0f)
                    npc.Center = target.Center - Vector2.UnitY * 2700f;

                attackTimer = 0f;
                attackState = (int)TwinsAttackType.BasicShots;
                npc.Calamity().newAI[1] = (int)Artemis.SecondaryPhase.PassiveAndImmune;
                npc.ModNPC<Artemis>().ChargeFlash = 0f;
                npc.Calamity().ShouldCloseHPBar = true;
                npc.dontTakeDamage = true;
            }
            else
                npc.Opacity = Clamp(npc.Opacity + 0.08f, 0f, 1f);

            // Despawn if the target is gone.
            if (!target.active || target.dead)
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                    npc.active = false;
            }

            // Handle the second phase transition.
            if (phaseTransitionAnimationTime < Phase2TransitionTime && lifeRatio < ExoMechManagement.Phase3LifeRatio)
            {
                npc.dontTakeDamage = true;
                npc.ModNPC<Artemis>().ChargeFlash = 0f;
                DoBehavior_DoPhaseTransition(npc, target, ref frame, hoverSide, phaseTransitionAnimationTime);
                return false;
            }

            // Handle the final phase transition.
            if (finalPhaseAnimationTime < ExoMechManagement.FinalPhaseTransitionTime && ExoMechManagement.CurrentTwinsPhase >= 6 && !performingDeathAnimation)
            {
                npc.ModNPC<Artemis>().ChargeFlash = 0f;
                attackState = (int)TwinsAttackType.BasicShots;
                finalPhaseAnimationTime++;
                npc.dontTakeDamage = true;
                DoBehavior_DoFinalPhaseTransition(npc, target, ref frame, hoverSide, finalPhaseAnimationTime);
                return false;
            }

            // Perform specific attack behaviors.
            PerformSpecificAttackBehaviors(npc, target, performingDeathAnimation, attackState, hoverSide, ref apollo.Infernum().ExtraAI[ExoMechManagement.Twins_ComplementMechEnrageTimerIndex], ref frame, ref attackTimer, ref deathAnimationTimer);
            return false;
        }

        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            int frameX = (int)npc.localAI[0] / 9;
            int frameY = (int)npc.localAI[0] % 9;
            npc.frame = new Rectangle(npc.width * frameX, npc.height * frameY, npc.width, npc.height);
        }

        public static float FlameTrailWidthFunction(NPC npc, float completionRatio) => SmoothStep(21f, 8f, completionRatio) * npc.ModNPC<Artemis>().ChargeFlash;

        public static float FlameTrailWidthFunctionBig(NPC npc, float completionRatio) => SmoothStep(34f, 12f, completionRatio) * npc.ModNPC<Artemis>().ChargeFlash;

        public static float RibbonTrailWidthFunction(float completionRatio)
        {
            float baseWidth = Utils.GetLerpValue(1f, 0.54f, completionRatio, true) * 5f;
            float endTipWidth = CalamityUtils.Convert01To010(Utils.GetLerpValue(0.96f, 0.89f, completionRatio, true)) * 2.4f;
            return baseWidth + endTipWidth;
        }

        public static Color FlameTrailColorFunction(NPC npc, float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true);
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.27f);
            Color middleColor = Color.Lerp(Color.Orange, Color.Yellow, 0.31f);
            Color endColor = Color.OrangeRed;
            return CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * npc.ModNPC<Artemis>().ChargeFlash * trailOpacity;
        }

        public static Color FlameTrailColorFunctionBig(NPC npc, float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true) * 0.56f;
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.25f);
            Color middleColor = Color.Lerp(Color.Blue, Color.White, 0.35f);
            Color endColor = Color.Lerp(Color.DarkBlue, Color.White, 0.47f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * npc.ModNPC<Artemis>().ChargeFlash * trailOpacity;
            color.A = 0;
            return color;
        }

        public static Color RibbonTrailColorFunction(NPC npc, float completionRatio)
        {
            Color startingColor = new(34, 40, 48);
            Color endColor = new(219, 82, 28);
            return Color.Lerp(startingColor, endColor, Pow(completionRatio, 1.5f)) * npc.Opacity;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Declare the trail drawers if they have yet to be defined.
            if (npc.ModNPC<Artemis>().ChargeFlameTrail is null)
                npc.ModNPC<Artemis>().ChargeFlameTrail = new PrimitiveTrail(c => FlameTrailWidthFunction(npc, c), c => FlameTrailColorFunction(npc, c), null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            if (npc.ModNPC<Artemis>().ChargeFlameTrailBig is null)
                npc.ModNPC<Artemis>().ChargeFlameTrailBig = new PrimitiveTrail(c => FlameTrailWidthFunctionBig(npc, c), c => FlameTrailColorFunctionBig(npc, c), null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            if (npc.ModNPC<Artemis>().RibbonTrail is null)
                npc.ModNPC<Artemis>().RibbonTrail = new PrimitiveTrail(RibbonTrailWidthFunction, c => RibbonTrailColorFunction(npc, c));

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(InfernumTextureRegistry.StreakFaded);
            DrawExoTwin(npc, lightColor, npc.ModNPC<Artemis>().ChargeFlash, npc.ModNPC<Artemis>().RibbonTrail, npc.ModNPC<Artemis>().ChargeFlameTrail, npc.ModNPC<Artemis>().ChargeFlameTrailBig);
            return false;
        }
        #endregion Frames and Drawcode

        #region Death Effects
        public override bool CheckDead(NPC npc) => ExoMechManagement.HandleDeathEffects(npc);
        #endregion Death Effects
    }
}
