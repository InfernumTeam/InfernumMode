using InfernumMode.Content.Skies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Assets.Effects
{
    public static class InfernumEffectsRegistry
    {
        #region Texture Shaders
        public static MiscShaderData AEWPsychicEnergyShader => GameShaders.Misc["Infernum:AEWPsychicEnergy"];
        public static MiscShaderData AresLightningVertexShader => GameShaders.Misc["Infernum:AresLightningArc"];
        public static MiscShaderData ArtemisLaserVertexShader => GameShaders.Misc["Infernum:ArtemisLaser"];
        public static MiscShaderData BasicTintShader => GameShaders.Misc["Infernum:BasicTint"];
        public static MiscShaderData BrainPsychicVertexShader => GameShaders.Misc["Infernum:BrainPsychic"];
        public static MiscShaderData CircleCutoutShader => GameShaders.Misc["Infernum:CircleCutout"];
        public static MiscShaderData CircleCutout2Shader => GameShaders.Misc["Infernum:CircleCutout2"];
        public static MiscShaderData CloudVertexShader => GameShaders.Misc["Infernum:CloudShader"];
        public static MiscShaderData CultistDeathVertexShader => GameShaders.Misc["Infernum:CultistDeath"];
        public static MiscShaderData CyclicHueShader => GameShaders.Misc["Infernum:CyclicHueShader"];
        public static MiscShaderData DukeTornadoVertexShader => GameShaders.Misc["Infernum:DukeTornado"];
        public static MiscShaderData DarkFlamePillarVertexShader => GameShaders.Misc["Infernum:DarkFlamePillar"];
        public static MiscShaderData FireVertexShader => GameShaders.Misc["Infernum:Fire"];
        public static MiscShaderData GaussianBlurShader => GameShaders.Misc["Infernum:GaussianBlur"];
        public static MiscShaderData GradientWingShader => GameShaders.Misc["Infernum:GradientWingShader"];
        public static MiscShaderData GenericLaserVertexShader => GameShaders.Misc["Infernum:GenericLaserShader"];
        public static MiscShaderData HologramShader => GameShaders.Misc["Infernum:Hologram"];
        public static MiscShaderData LinearTransformationVertexShader => GameShaders.Misc["Infernum:LinearTransformation"];
        public static MiscShaderData NecroplasmicRoarShader => GameShaders.Misc["Infernum:NecroplasmicRoar"];
        public static MiscShaderData MechsIntroLetterShader => GameShaders.Misc["Infernum:MechsIntro"];
        public static MiscShaderData MoonLordBGDistortionShader => GameShaders.Misc["Infernum:MoonLordBGDistortion"];
        public static MiscShaderData PolterghastEctoplasmVertexShader => GameShaders.Misc["Infernum:PolterghastEctoplasm"];
        public static MiscShaderData PrismaticRayVertexShader => GameShaders.Misc["Infernum:PrismaticRay"];
        public static MiscShaderData PristineArmorShader => GameShaders.Misc["Infernum:PristineArmorShader"];
        public static MiscShaderData ProviLaserVertexShader => GameShaders.Misc["Infernum:ProviLaserShader"];
        public static MiscShaderData RealityTearVertexShader => GameShaders.Misc["Infernum:RealityTear"];
        public static MiscShaderData RealityTear2Shader => GameShaders.Misc["Infernum:RealityTear2"];
        public static MiscShaderData SCalIntroLetterShader => GameShaders.Misc["Infernum:SCalIntro"];
        public static MiscShaderData SideStreakVertexShader => GameShaders.Misc["Infernum:SideStreak"];
        public static MiscShaderData TwinsFlameTrailVertexShader => GameShaders.Misc["Infernum:TwinsFlameTrail"];
        public static MiscShaderData WoFGeyserVertexShader => GameShaders.Misc["Infernum:WoFGeyserTexture"];
        public static MiscShaderData WoFTentacleVertexShader => GameShaders.Misc["Infernum:WoFTentacleTexture"];
        public static MiscShaderData YharonBurnShader => GameShaders.Misc["Infernum:YharonBurn"];
        #endregion

        #region Screen Shaders
        public static Filter AresScreenShader => Filters.Scene["InfernumMode:Ares"];
        public static Filter DeusScreenShader => Filters.Scene["InfernumMode:Deus"];
        public static Filter DragonfollyScreenShader => Filters.Scene["InfernumMode:Dragonfolly"];
        public static Filter DoGScreenShader => Filters.Scene["InfernumMode:DoG"];
        public static Filter EoLScreenShader => Filters.Scene["InfernumMode:EmpressOfLight"];
        public static Filter HiveMindScreenShader => Filters.Scene["InfernumMode:HiveMind"];
        public static Filter MadnessScreenShader => Filters.Scene["InfernumMode:Madness"];
        public static Filter NightProviScreenShader => Filters.Scene["InfernumMode:NightProvidence"];
        public static Filter OldDukeScreenShader => Filters.Scene["InfernumMode:OldDuke"];
        public static Filter PerforatorsScreenShader => Filters.Scene["InfernumMode:Perforators"];
        public static Filter SCalScreenShader => Filters.Scene["InfernumMode:SCal"];
        public static Filter ScreenDistortionScreenShader => Filters.Scene["InfernumMode:ScreenDistortion"];
        public static Filter ScreenSaturationBlurScreenShader => Filters.Scene["InfernumMode:ScreenSaturationBlur"];
        public static Filter ScreenShakeScreenShader => Filters.Scene["InfernumMode:ScreenShake"];
        public static Filter TwinsScreenShader => Filters.Scene["InfernumMode:Twins"];
        #endregion

        #region Methods
        public static void LoadEffects()
        {
            var assets = InfernumMode.Instance.Assets;

            LoadRegularShaders(assets);

            LoadScreenShaders(assets);
        }

        public static void LoadRegularShaders(AssetRepository assets)
        {
            Ref<Effect> gaussianBlur = new(assets.Request<Effect>("Assets/Effects/GaussianBlur", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["InfernumMode:GaussianBlur"] = new MiscShaderData(gaussianBlur, "ScreenPass");

            Ref<Effect> cloudShader = new(assets.Request<Effect>("Assets/Effects/CloudShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:CloudShader"] = new MiscShaderData(cloudShader, "TrailPass");

            Ref<Effect> genericLaserShader = new(assets.Request<Effect>("Assets/Effects/GenericLaserShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:GenericLaserShader"] = new MiscShaderData(genericLaserShader, "TrailPass");

            Ref<Effect> proviLaserShader = new(assets.Request<Effect>("Assets/Effects/ProviLaserShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:ProviLaserShader"] = new MiscShaderData(proviLaserShader, "TrailPass");

            Ref<Effect> basicTintShader = new(assets.Request<Effect>("Assets/Effects/BasicTint", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:BasicTint"] = new MiscShaderData(basicTintShader, "BasicTint");

            Ref<Effect> aewPsychicEnergyShader = new(assets.Request<Effect>("Assets/Effects/AEWPsychicDistortionShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:AEWPsychicEnergy"] = new MiscShaderData(aewPsychicEnergyShader, "DistortionPass");

            Ref<Effect> gradientShader = new(assets.Request<Effect>("Assets/Effects/GradientWingShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:GradientWingShader"] = new MiscShaderData(gradientShader, "GradientPass");

            Ref<Effect> cyclicHueShader = new(assets.Request<Effect>("Assets/Effects/CyclicHueShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:CyclicHueShader"] = new MiscShaderData(cyclicHueShader, "OutlineShader");

            Ref<Effect> pristineArmorShader = new(assets.Request<Effect>("Assets/Effects/PristineArmorShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:PristineArmorShader"] = new MiscShaderData(pristineArmorShader, "PristinePass");

            Ref<Effect> dukeTornadoShader = new(assets.Request<Effect>("Assets/Effects/DukeTornado", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:DukeTornado"] = new MiscShaderData(dukeTornadoShader, "TrailPass");

            Ref<Effect> tentacleFleshShader = new(assets.Request<Effect>("Assets/Effects/TentacleTexture", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:WoFTentacleTexture"] = new MiscShaderData(tentacleFleshShader, "TrailPass");

            Ref<Effect> bloodGeyserShader = new(assets.Request<Effect>("Assets/Effects/BloodGeyser", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:WoFGeyserTexture"] = new MiscShaderData(bloodGeyserShader, "TrailPass");

            Ref<Effect> shadowflameShader = new(assets.Request<Effect>("Assets/Effects/Shadowflame", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:Fire"] = new MiscShaderData(shadowflameShader, "TrailPass");

            Ref<Effect> brainPsychicShader = new(assets.Request<Effect>("Assets/Effects/BrainPsychicShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:BrainPsychic"] = new MiscShaderData(brainPsychicShader, "TrailPass");

            Ref<Effect> cultistDeathAnimationShader = new(assets.Request<Effect>("Assets/Effects/CultistDeathAnimation", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:CultistDeath"] = new MiscShaderData(cultistDeathAnimationShader, "DeathPass");

            Ref<Effect> flameTrailShader = new(assets.Request<Effect>("Assets/Effects/TwinsFlameTail", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:TwinsFlameTrail"] = new MiscShaderData(flameTrailShader, "TrailPass");

            Ref<Effect> aresLightningArcShader = new(assets.Request<Effect>("Assets/Effects/AresLightningArcShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:AresLightningArc"] = new MiscShaderData(aresLightningArcShader, "TrailPass");

            Ref<Effect> ghostlyShader = new(assets.Request<Effect>("Assets/Effects/EidolicWailRingShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:PolterghastEctoplasm"] = new MiscShaderData(ghostlyShader, "BurstPass");

            ghostlyShader = new Ref<Effect>(assets.Request<Effect>("Assets/Effects/NecroplasmicRoarShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:NecroplasmicRoar"] = new MiscShaderData(ghostlyShader, "BurstPass");

            Ref<Effect> backgroundShader = new(assets.Request<Effect>("Assets/Effects/MoonLordBGDistortionShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:MoonLordBGDistortion"] = new MiscShaderData(backgroundShader, "DistortionPass");

            Ref<Effect> introShader = new(assets.Request<Effect>("Assets/Effects/MechIntroLetterShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:MechsIntro"] = new MiscShaderData(introShader, "LetterPass");

            introShader = new Ref<Effect>(assets.Request<Effect>("Assets/Effects/SCalIntroLetterShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:SCalIntro"] = new MiscShaderData(introShader, "LetterPass");

            Ref<Effect> rayShader = new(assets.Request<Effect>("Assets/Effects/PrismaticRayShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:PrismaticRay"] = new MiscShaderData(rayShader, "TrailPass");

            Ref<Effect> darkFlamePillarShader = new(assets.Request<Effect>("Assets/Effects/DarkFlamePillarShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:DarkFlamePillar"] = new MiscShaderData(darkFlamePillarShader, "TrailPass");

            Ref<Effect> artemisLaserShader = new(assets.Request<Effect>("Assets/Effects/ArtemisLaserShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:ArtemisLaser"] = new MiscShaderData(artemisLaserShader, "TrailPass");

            Ref<Effect> realityTearShader = new(assets.Request<Effect>("Assets/Effects/RealityTearShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:RealityTear"] = new MiscShaderData(realityTearShader, "TrailPass");

            realityTearShader = new(assets.Request<Effect>("Assets/Effects/RealityTear2Shader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:RealityTear2"] = new MiscShaderData(realityTearShader, "TrailPass");

            Ref<Effect> hologramShader = new(assets.Request<Effect>("Assets/Effects/HologramShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:Hologram"] = new MiscShaderData(hologramShader, "HologramPass");

            Ref<Effect> matrixShader = new(assets.Request<Effect>("Assets/Effects/LocalLinearTransformationShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:LinearTransformation"] = new MiscShaderData(matrixShader, "TransformationPass");

            Ref<Effect> cutoutShader = new(assets.Request<Effect>("Assets/Effects/CircleCutoutShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:CircleCutout"] = new MiscShaderData(cutoutShader, "CutoutPass");

            cutoutShader = new(assets.Request<Effect>("Assets/Effects/CircleCutoutShader2", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:CircleCutout2"] = new MiscShaderData(cutoutShader, "CutoutPass");

            Ref<Effect> streakShader = new(assets.Request<Effect>("Assets/Effects/SideStreakTrail", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:SideStreak"] = new MiscShaderData(streakShader, "TrailPass");

            Ref<Effect> yharonBurnShader = new(assets.Request<Effect>("Assets/Effects/YharonBurnShader", AssetRequestMode.ImmediateLoad).Value);
            GameShaders.Misc["Infernum:YharonBurn"] = new MiscShaderData(yharonBurnShader, "BurnPass");
        }

        public static void LoadScreenShaders(AssetRepository assets)
        {
            // Ares (ultimate attack).
            Filters.Scene["InfernumMode:Ares"] = new Filter(new AresScreenShaderData("FilterMiniTower").UseColor(Color.Red).UseOpacity(0.5f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Ares"] = new AresSky();

            // Astrum Deus.
            Filters.Scene["InfernumMode:Deus"] = new Filter(new DeusScreenShaderData("FilterMiniTower").UseColor(Color.Lerp(Color.Purple, Color.Black, 0.75f)).UseOpacity(0.24f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Deus"] = new DeusSky();

            // Dragonfolly.
            Filters.Scene["InfernumMode:Dragonfolly"] = new Filter(new DragonfollyScreenShaderData("FilterMiniTower").UseColor(Color.Red).UseOpacity(0.6f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Dragonfolly"] = new DragonfollySky();

            // Devourer of Gods.
            Filters.Scene["InfernumMode:DoG"] = new Filter(new PerforatorScreenShaderData("FilterMiniTower").UseColor(0.4f, 0.1f, 1.0f).UseOpacity(0.5f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:DoG"] = new DoGSkyInfernum();

            // Empress of Light.
            Effect screenShader = assets.Request<Effect>("Assets/Effects/EmpressOfLightScreenShader", AssetRequestMode.ImmediateLoad).Value;
            Filters.Scene["InfernumMode:EmpressOfLight"] = new Filter(new EmpressOfLightScreenShaderData(screenShader, "ScreenPass"), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:EmpressOfLight"] = new EmpressOfLightSky();

            // Hive Mind.
            Filters.Scene["InfernumMode:HiveMind"] = new Filter(new HiveMindScreenShaderData("FilterMiniTower").UseColor(HiveMindSky.SkyColor).UseOpacity(0.6f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:HiveMind"] = new HiveMindSky();

            // Deerclops.
            Ref<Effect> madnessShader = new(assets.Request<Effect>("Assets/Effects/Madness", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["InfernumMode:Madness"] = new Filter(new MadnessScreenShaderData(madnessShader, "DyePass"), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Madness"] = new MadnessSky();

            // Night Providence.
            Filters.Scene["InfernumMode:NightProvidence"] = new Filter(new NightProvidenceShaderData("FilterMiniTower").UseOpacity(0.67f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:NightProvidence"] = new NightProvidenceSky();

            // Old Duke.
            Filters.Scene["InfernumMode:OldDuke"] = new Filter(new OldDukeScreenShaderData("FilterMiniTower").UseOpacity(0.6f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:OldDuke"] = new OldDukeSky();

            // Perforators (death animation).
            Filters.Scene["InfernumMode:Perforators"] = new Filter(new PerforatorScreenShaderData("FilterMiniTower").UseColor(new Color(255, 60, 30)).UseOpacity(0.445f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Perforators"] = new PerforatorSky();

            // Supreme Calamitas.
            Ref<Effect> scalScreenShader = new(assets.Request<Effect>("Assets/Effects/SCalFireBGShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["InfernumMode:SCal"] = new Filter(new SCalScreenShaderData(scalScreenShader, "DyePass").UseColor(0.3f, 0f, 0f).UseOpacity(0.5f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:SCal"] = new SCalSkyInfernum();

            // General screen shake distortion.
            Ref<Effect> screenShakeShader = new(assets.Request<Effect>("Assets/Effects/ScreenShakeShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["InfernumMode:ScreenShake"] = new Filter(new ScreenShaderData(screenShakeShader, "DyePass"), EffectPriority.VeryHigh);

            // Screen saturation blur system shader.
            Ref<Effect> screenSaturationBlurShader = new(assets.Request<Effect>("Assets/Effects/ScreenSaturationBlurShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["InfernumMode:ScreenSaturationBlur"] = new Filter(new ScreenSaturationBlurShaderData(screenSaturationBlurShader, "ScreenPass"), EffectPriority.VeryHigh);

            // Heat distortion effect.
            Ref<Effect> screenDistortionShader = new(assets.Request<Effect>("Assets/Effects/ScreenDistortionShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["InfernumMode:ScreenDistortion"] = new Filter(new ScreenShaderData(screenDistortionShader, "ScreenPass"), EffectPriority.VeryHigh);

            // Twins (desperation phase).
            Filters.Scene["InfernumMode:Twins"] = new Filter(new TwinsScreenShaderData("FilterMiniTower").UseColor(Color.Red).UseOpacity(0.5f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Twins"] = new TwinsSky();
        }
        #endregion
    }
}
