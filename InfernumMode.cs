using CalamityMod.Events;
using InfernumMode.Balancing;
using InfernumMode.BossIntroScreens;
using InfernumMode.BossRush;
using InfernumMode.ILEditingStuff;
using InfernumMode.Items;
using InfernumMode.OverridingSystem;
using InfernumMode.Skies;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.Particles.Metaballs.FusableParticleManager;

namespace InfernumMode
{
    public class InfernumMode : Mod
    {
        internal static InfernumMode Instance = null;

        internal static Mod CalamityMod = null;

        internal static Mod FargosMutantMod = null;

        internal static Mod FargowiltasSouls = null;

        internal static Mod PhaseIndicator = null;

        internal static bool CanUseCustomAIs => (!BossRushEvent.BossRushActive || BossRushApplies) && WorldSaveSystem.InfernumMode;

        internal static bool BossRushApplies => true;

        internal static readonly Color HiveMindSkyColor = new(53, 42, 82);

        public static float BlackFade
        {
            get;
            set;
        } = 0f;

        public static float DraedonThemeTimer
        {
            get;
            set;
        } = 0f;

        public static float ProvidenceArenaTimer
        {
            get;
            set;
        }

        public static bool EmodeIsActive
        {
            get
            {
                if (FargowiltasSouls is null)
                    return false;
                return (bool)FargowiltasSouls?.Call("Emode");
            }
        }

        public override void Load()
        {
            Instance = this;
            CalamityMod = ModLoader.GetMod("CalamityMod");
            ModLoader.TryGetMod("Fargowiltas", out FargosMutantMod);
            ModLoader.TryGetMod("FargowiltasSouls", out FargowiltasSouls);
            ModLoader.TryGetMod("PhaseIndicator", out PhaseIndicator);

            BalancingChangesManager.Load();
            HookManager.Load();
            // Manually invoke the attribute constructors to get the marked methods cached.
            foreach (var type in typeof(InfernumMode).Assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(Utilities.UniversalBindingFlags))
                    method.GetCustomAttributes(false);
            }

            IntroScreenManager.Load();
            NPCBehaviorOverride.LoadAll();
            ProjectileBehaviorOverride.LoadAll();

            if (Main.netMode != NetmodeID.Server)
            {
                AddBossHeadTexture("InfernumMode/BehaviorOverrides/BossAIs/Cryogen/CryogenMapIcon", -1);
                AddBossHeadTexture("InfernumMode/BehaviorOverrides/BossAIs/Dreadnautilus/DreadnautilusMapIcon", -1);
                AddBossHeadTexture("InfernumMode/BehaviorOverrides/BossAIs/SupremeCalamitas/SepulcherMapIcon", -1);

                Ref<Effect> genericLaserShader = new(Assets.Request<Effect>("Effects/GenericLaserShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:GenericLaserShader"] = new MiscShaderData(genericLaserShader, "TrailPass");

                Ref<Effect> proviLaserShader = new(Assets.Request<Effect>("Effects/ProviLaserShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:ProviLaserShader"] = new MiscShaderData(proviLaserShader, "TrailPass");

                Ref<Effect> basicTintShader = new(Assets.Request<Effect>("Effects/BasicTint", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:BasicTint"] = new MiscShaderData(basicTintShader, "BasicTint");

                Ref<Effect> madnessShader = new(Assets.Request<Effect>("Effects/Madness", AssetRequestMode.ImmediateLoad).Value);
                Filters.Scene["InfernumMode:Madness"] = new Filter(new MadnessScreenShaderData(madnessShader, "DyePass"), EffectPriority.VeryHigh);

                Ref<Effect> aewPsychicEnergyShader = new(Assets.Request<Effect>("Effects/AEWPsychicDistortionShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:AEWPsychicEnergy"] = new MiscShaderData(aewPsychicEnergyShader, "DistortionPass");

                Ref<Effect> gradientShader = new(Assets.Request<Effect>("Effects/GradientWingShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:GradientWingShader"] = new MiscShaderData(gradientShader, "GradientPass");

                Ref<Effect> cyclicHueShader = new(Assets.Request<Effect>("Effects/CyclicHueShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:CyclicHueShader"] = new MiscShaderData(cyclicHueShader, "OutlineShader");

                Ref<Effect> pristineArmorShader = new(Assets.Request<Effect>("Effects/PristineArmorShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:PristineArmorShader"] = new MiscShaderData(pristineArmorShader, "PristinePass");

                Ref<Effect> dukeTornadoShader = new(Assets.Request<Effect>("Effects/DukeTornado", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:DukeTornado"] = new MiscShaderData(dukeTornadoShader, "TrailPass");

                Ref<Effect> tentacleFleshShader = new(Assets.Request<Effect>("Effects/TentacleTexture", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:WoFTentacleTexture"] = new MiscShaderData(tentacleFleshShader, "TrailPass");

                Ref<Effect> bloodGeyserShader = new(Assets.Request<Effect>("Effects/BloodGeyser", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:WoFGeyserTexture"] = new MiscShaderData(bloodGeyserShader, "TrailPass");

                Ref<Effect> shadowflameShader = new(Assets.Request<Effect>("Effects/Shadowflame", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:Fire"] = new MiscShaderData(shadowflameShader, "TrailPass");

                Ref<Effect> brainPsychicShader = new(Assets.Request<Effect>("Effects/BrainPsychicShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:BrainPsychic"] = new MiscShaderData(brainPsychicShader, "TrailPass");

                Ref<Effect> cultistDeathAnimationShader = new(Assets.Request<Effect>("Effects/CultistDeathAnimation", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:CultistDeath"] = new MiscShaderData(cultistDeathAnimationShader, "DeathPass");

                Ref<Effect> flameTrailShader = new(Assets.Request<Effect>("Effects/TwinsFlameTail", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:TwinsFlameTrail"] = new MiscShaderData(flameTrailShader, "TrailPass");

                Ref<Effect> aresLightningArcShader = new(Assets.Request<Effect>("Effects/AresLightningArcShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:AresLightningArc"] = new MiscShaderData(aresLightningArcShader, "TrailPass");

                Ref<Effect> ghostlyShader = new(Assets.Request<Effect>("Effects/EidolicWailRingShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:PolterghastEctoplasm"] = new MiscShaderData(ghostlyShader, "BurstPass");

                ghostlyShader = new Ref<Effect>(Assets.Request<Effect>("Effects/NecroplasmicRoarShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:NecroplasmicRoar"] = new MiscShaderData(ghostlyShader, "BurstPass");

                Ref<Effect> backgroundShader = new(Assets.Request<Effect>("Effects/MoonLordBGDistortionShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:MoonLordBGDistortion"] = new MiscShaderData(backgroundShader, "DistortionPass");

                Ref<Effect> introShader = new(Assets.Request<Effect>("Effects/MechIntroLetterShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:MechsIntro"] = new MiscShaderData(introShader, "LetterPass");

                introShader = new Ref<Effect>(Assets.Request<Effect>("Effects/SCalIntroLetterShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:SCalIntro"] = new MiscShaderData(introShader, "LetterPass");

                Ref<Effect> rayShader = new(Assets.Request<Effect>("Effects/PrismaticRayShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:PrismaticRay"] = new MiscShaderData(rayShader, "TrailPass");

                Ref<Effect> darkFlamePillarShader = new(Assets.Request<Effect>("Effects/DarkFlamePillarShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:DarkFlamePillar"] = new MiscShaderData(darkFlamePillarShader, "TrailPass");

                Ref<Effect> artemisLaserShader = new(Assets.Request<Effect>("Effects/ArtemisLaserShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:ArtemisLaser"] = new MiscShaderData(artemisLaserShader, "TrailPass");

                Ref<Effect> realityTearShader = new(Assets.Request<Effect>("Effects/RealityTearShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:RealityTear"] = new MiscShaderData(realityTearShader, "TrailPass");

                realityTearShader = new(Assets.Request<Effect>("Effects/RealityTear2Shader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:RealityTear2"] = new MiscShaderData(realityTearShader, "TrailPass");

                Ref<Effect> hologramShader = new(Assets.Request<Effect>("Effects/HologramShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:Hologram"] = new MiscShaderData(hologramShader, "HologramPass");

                Ref<Effect> matrixShader = new(Assets.Request<Effect>("Effects/LocalLinearTransformationShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:LinearTransformation"] = new MiscShaderData(matrixShader, "TransformationPass");

                Ref<Effect> cutoutShader = new(Assets.Request<Effect>("Effects/CircleCutoutShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:CircleCutout"] = new MiscShaderData(cutoutShader, "CutoutPass");

                cutoutShader = new(Assets.Request<Effect>("Effects/CircleCutoutShader2", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:CircleCutout2"] = new MiscShaderData(cutoutShader, "CutoutPass");

                Ref<Effect> streakShader = new(Assets.Request<Effect>("Effects/SideStreakTrail", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:SideStreak"] = new MiscShaderData(streakShader, "TrailPass");

                Ref<Effect> yharonBurnShader = new(Assets.Request<Effect>("Effects/YharonBurnShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:YharonBurn"] = new MiscShaderData(yharonBurnShader, "BurnPass");
                
                Ref<Effect> teleportShader = new(Assets.Request<Effect>("Effects/TeleportShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["Infernum:Teleport"] = new MiscShaderData(teleportShader, "HologramPass");

                // Screen shaders.
                Effect screenShader = Assets.Request<Effect>("Effects/EmpressOfLightScreenShader", AssetRequestMode.ImmediateLoad).Value;
                Filters.Scene["InfernumMode:EmpressOfLight"] = new Filter(new EmpressOfLightScreenShaderData(screenShader, "ScreenPass"), EffectPriority.VeryHigh);
                SkyManager.Instance["InfernumMode:EmpressOfLight"] = new EmpressOfLightSky();

                Filters.Scene["InfernumMode:HiveMind"] = new Filter(new HiveMindScreenShaderData("FilterMiniTower").UseColor(HiveMindSkyColor).UseOpacity(0.6f), EffectPriority.VeryHigh);
                SkyManager.Instance["InfernumMode:HiveMind"] = new HiveMindSky();

                Filters.Scene["InfernumMode:Perforators"] = new Filter(new PerforatorScreenShaderData("FilterMiniTower").UseColor(new Color(255, 60, 30)).UseOpacity(0.445f), EffectPriority.VeryHigh);
                SkyManager.Instance["InfernumMode:Perforators"] = new PerforatorSky();

                Filters.Scene["InfernumMode:Dragonfolly"] = new Filter(new DragonfollyScreenShaderData("FilterMiniTower").UseColor(Color.Red).UseOpacity(0.6f), EffectPriority.VeryHigh);
                SkyManager.Instance["InfernumMode:Dragonfolly"] = new DragonfollySky();

                Filters.Scene["InfernumMode:Deus"] = new Filter(new DeusScreenShaderData("FilterMiniTower").UseColor(Color.Lerp(Color.Purple, Color.Black, 0.75f)).UseOpacity(0.24f), EffectPriority.VeryHigh);
                SkyManager.Instance["InfernumMode:Deus"] = new DeusSky();

                Filters.Scene["InfernumMode:NightProvidence"] = new Filter(new NightProvidenceShaderData("FilterMiniTower").UseOpacity(0.67f), EffectPriority.VeryHigh);
                SkyManager.Instance["InfernumMode:NightProvidence"] = new NightProvidenceSky();

                Filters.Scene["InfernumMode:OldDuke"] = new Filter(new OldDukeScreenShaderData("FilterMiniTower").UseOpacity(0.6f), EffectPriority.VeryHigh);
                SkyManager.Instance["InfernumMode:OldDuke"] = new OldDukeSky();

                Filters.Scene["InfernumMode:DoG"] = new Filter(new PerforatorScreenShaderData("FilterMiniTower").UseColor(0.4f, 0.1f, 1.0f).UseOpacity(0.5f), EffectPriority.VeryHigh);
                SkyManager.Instance["InfernumMode:DoG"] = new DoGSkyInfernum();

                Ref<Effect> scalScreenShader = new(Assets.Request<Effect>("Effects/SCalFireBGShader", AssetRequestMode.ImmediateLoad).Value);
                Filters.Scene["InfernumMode:SCal"] = new Filter(new SCalScreenShaderData(scalScreenShader, "DyePass").UseColor(0.3f, 0f, 0f).UseOpacity(0.5f), EffectPriority.VeryHigh);
                SkyManager.Instance["InfernumMode:SCal"] = new SCalSkyInfernum();
                
                Ref<Effect> screenShakeShader = new(Assets.Request<Effect>("Effects/ScreenShakeShader", AssetRequestMode.ImmediateLoad).Value);
                Filters.Scene["InfernumMode:ScreenShake"] = new Filter(new ScreenShaderData(screenShakeShader, "DyePass"), EffectPriority.VeryHigh);

                Ref<Effect> screenSaturationBlurShader = new(Assets.Request<Effect>("Effects/ScreenSaturationBlurShader", AssetRequestMode.ImmediateLoad).Value);
                Filters.Scene["InfernumMode:ScreenSaturationBlur"] = new Filter(new ScreenSaturationBlurShaderData(screenSaturationBlurShader, "ScreenPass"), EffectPriority.VeryHigh);
                
                SkyManager.Instance["InfernumMode:Madness"] = new MadnessSky();
            }

            if (BossRushApplies)
                BossRushChanges.Load();

            if (Main.netMode != NetmodeID.Server)
            {
                Main.QueueMainThreadAction(() =>
                {
                    CalamityMod.Call("LoadParticleInstances", this);
                    ParticleSets = new();
                    ParticleSetTypes = new();
                    HasBeenFormallyDefined = true;

                    FindParticleSetTypesInMod(CalamityMod, Main.screenWidth, Main.screenHeight);
                    foreach (Mod m in ExtraModsToLoadSetsFrom)
                        FindParticleSetTypesInMod(m, Main.screenWidth, Main.screenHeight);
                });
            }

            _ = new InfernumDifficulty();
        }

        public override void PostSetupContent()
        {
            NPCBehaviorOverride.LoadPhaseIndicaors();
            Utilities.UpdateMapIconList();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI) => NetcodeHandler.ReceivePacket(this, reader, whoAmI);

        public override void AddRecipes() => RecipeUpdates.Update();

        public override object Call(params object[] args)
        {
            return InfernumModCalls.Call(args);
        }

        public override void Unload()
        {
            IntroScreenManager.Unload();
            BalancingChangesManager.Unload();
            HookManager.Unload();
            Instance = null;
            CalamityMod = null;
        }
    }
}