using InfernumMode.Content.Skies;
using InfernumMode.Core;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Assets.Effects
{
    public static class InfernumEffectsRegistry
    {
        #region Old
        #region Texture Shaders
        public static Asset<Effect> FluidSimulatorShader
        {
            get;
            internal set;
        }

        public static MiscShaderData AEWShadowFormShader => GameShaders.Misc["Infernum:AEWShadowForm"];
        public static MiscShaderData AreaBorderVertexShader => GameShaders.Misc["Infernum:AreaBorder"];
        public static MiscShaderData AresLightningVertexShader => GameShaders.Misc["Infernum:AresLightningArc"];
        public static MiscShaderData ArtemisLaserVertexShader => GameShaders.Misc["Infernum:ArtemisLaser"];
        public static MiscShaderData BackgroundDistortionShader => GameShaders.Misc["Infernum:BackgroundDistortion"];
        public static MiscShaderData BasicTintShader => GameShaders.Misc["Infernum:BasicTint"];
        public static MiscShaderData BrainPsychicVertexShader => GameShaders.Misc["Infernum:BrainPsychic"];
        public static MiscShaderData CeaselessVoidBackgroundShader => GameShaders.Misc["Infernum:CVBackground"];
        public static MiscShaderData CeaselessVoidCrackShader => GameShaders.Misc["Infernum:CVCrack"];
        public static MiscShaderData CeaselessVoidPortalShader => GameShaders.Misc["Infernum:CVPortal"];
        public static MiscShaderData CircleCutoutShader => GameShaders.Misc["Infernum:CircleCutout"];
        public static MiscShaderData CircleCutout2Shader => GameShaders.Misc["Infernum:CircleCutout2"];
        public static MiscShaderData CloudVertexShader => GameShaders.Misc["Infernum:CloudShader"];
        public static MiscShaderData CosmicBackgroundShader => GameShaders.Misc["Infernum:CosmicBackground"];
        public static MiscShaderData CultistDeathVertexShader => GameShaders.Misc["Infernum:CultistDeath"];
        public static MiscShaderData CultistShieldShader => GameShaders.Misc["Infernum:CultistShield"];
        public static MiscShaderData DarkFlamePillarVertexShader => GameShaders.Misc["Infernum:DarkFlamePillar"];
        public static MiscShaderData DoGDashIndicatorVertexShader => GameShaders.Misc["Infernum:DoGDashIndicatorShader"];
        public static MiscShaderData DukeTornadoVertexShader => GameShaders.Misc["Infernum:DukeTornado"];
        public static MiscShaderData FireVertexShader => GameShaders.Misc["Infernum:Fire"];
        public static MiscShaderData FishEyeShader => GameShaders.Misc["Infernum:Fisheye"];
        public static MiscShaderData FogShaderShader => GameShaders.Misc["Infernum:FogOverlay"];
        public static MiscShaderData GenericLaserVertexShader => GameShaders.Misc["Infernum:GenericLaserShader"];
        public static MiscShaderData GuardiansLaserVertexShader => GameShaders.Misc["Infernum:GuardiansLaserShader"];
        public static MiscShaderData KevinLightningShader => GameShaders.Misc["Infernum:KevinLightning"];
        public static MiscShaderData MechsIntroLetterShader => GameShaders.Misc["Infernum:MechsIntro"];
        public static MiscShaderData NoiseDisplacementShader => GameShaders.Misc["Infernum:NoiseDisplacement"];
        public static MiscShaderData PolterghastEctoplasmVertexShader => GameShaders.Misc["Infernum:PolterghastEctoplasm"];
        public static MiscShaderData PrismaticRayVertexShader => GameShaders.Misc["Infernum:PrismaticRay"];
        public static MiscShaderData ProfanedLavaVertexShader => GameShaders.Misc["Infernum:ProfanedLava"];
        public static MiscShaderData ProfanedPortalShader => GameShaders.Misc["Infernum:ProfanedPortal"];
        public static MiscShaderData ProviLaserVertexShader => GameShaders.Misc["Infernum:ProviLaserShader"];
        public static MiscShaderData PulsatingLaserVertexShader => GameShaders.Misc["Infernum:PulsatingLaserShader"];
        public static MiscShaderData RealityTear2Shader => GameShaders.Misc["Infernum:RealityTear2"];
        public static MiscShaderData SCalIntroLetterShader => GameShaders.Misc["Infernum:SCalIntro"];
        public static MiscShaderData SideStreakVertexShader => GameShaders.Misc["Infernum:SideStreak"];
        public static MiscShaderData SignusBackgroundShader => GameShaders.Misc["Infernum:SignusBackground"];
        public static MiscShaderData ScreenInversionMetaballShader => GameShaders.Misc["Infernum:ScreenInversionMetaball"];
        public static MiscShaderData ScrollingCodePrimShader => GameShaders.Misc["Infernum:ScrollingCode"];
        public static MiscShaderData TwinsFlameTrailVertexShader => GameShaders.Misc["Infernum:TwinsFlameTrail"];
        public static MiscShaderData UnderwaterRayShader => GameShaders.Misc["Infernum:UnderwaterRays"];
        public static MiscShaderData WaterVertexShader => InfernumConfig.Instance.ReducedGraphicsConfig ? DukeTornadoVertexShader : GameShaders.Misc["Infernum:WaterShader"];
        public static MiscShaderData WoFGeyserVertexShader => GameShaders.Misc["Infernum:WoFGeyserTexture"];
        public static MiscShaderData WoFTentacleVertexShader => GameShaders.Misc["Infernum:WoFTentacleTexture"];
        public static MiscShaderData YharonBurnShader => GameShaders.Misc["Infernum:YharonBurn"];
        public static MiscShaderData YharonInfernadoShader => GameShaders.Misc["Infernum:YharonInfernado"];
        #endregion

        #region Screen Shaders
        public static Filter AfterimageShader => Filters.Scene["InfernumMode:AfterimageShader"];
        public static Filter AresScreenShader => Filters.Scene["InfernumMode:Ares"];
        public static Filter BossBarShader => Filters.Scene["InfernumMode:BossBar"];
        public static Filter CalShadowScreenShader => Filters.Scene["InfernumMode:CalShadow"];
        public static Filter CreditShader => Filters.Scene["InfernumMode:Credits"];
        public static Filter CRTShader => Filters.Scene["InfernumMode:CRTShader"];
        public static Filter CrystalCrackShader => Filters.Scene["InfernumMode:CrystalCrackShader"];
        public static Filter DeusScreenShader => Filters.Scene["InfernumMode:Deus"];
        public static Filter DeusGasShader => Filters.Scene["InfernumMode:DeusGasShader"];
        public static Filter DoGPortalShader => Filters.Scene["InfernumMode:DoGPortalShader"];
        public static Filter DragonfollyScreenShader => Filters.Scene["InfernumMode:Dragonfolly"];
        public static Filter DoGScreenShader => Filters.Scene["InfernumMode:DoG"];
        public static Filter EoLScreenShader => Filters.Scene["InfernumMode:EmpressOfLight"];
        public static Filter FireballShader => Filters.Scene["Infernum:FireballShader"];
        public static Filter HiveMindScreenShader => Filters.Scene["InfernumMode:HiveMind"];
        public static Filter LightningOverlayShader => Filters.Scene["InfernumMode:LightningOverlay"];
        public static Filter MadnessScreenShader => Filters.Scene["InfernumMode:Madness"];
        public static Filter MovieBarShader => Filters.Scene["InfernumMode:MovieBarShader"];
        public static Filter NightProviScreenShader => Filters.Scene["InfernumMode:NightProvidence"];
        public static Filter OldDukeScreenShader => Filters.Scene["InfernumMode:OldDuke"];
        public static Filter PerforatorsScreenShader => Filters.Scene["InfernumMode:Perforators"];
        public static Filter PulseRingShader => Filters.Scene["InfernumMode:PulseRing"];
        public static Filter RaindropShader => Filters.Scene["InfernumMode:Raindrops"];
        public static Filter SandstormShader => Filters.Scene["InfernumMode:SandstormShader"];
        public static Filter SCalScreenShader => Filters.Scene["InfernumMode:SCal"];
        public static Filter ScreenDistortionScreenShader => Filters.Scene["InfernumMode:ScreenDistortion"];
        public static Filter ScreenBorderShader => Filters.Scene["InfernumMode:ScreenBorder"];
        public static Filter ScreenShakeScreenShader => Filters.Scene["InfernumMode:ScreenShake"];
        public static Filter ScreenShakeScreenShader2 => Filters.Scene["InfernumMode:ScreenShake2"];
        public static Filter SpriteBurnShader => Filters.Scene["InfernumMode:SpriteBurn"];
        public static Filter ShadowShader => Filters.Scene["InfernumMode:ShadowShader"];
        public static Filter TwinsScreenShader => Filters.Scene["InfernumMode:Twins"];
        public static Filter WaterOverlayShader => Filters.Scene["InfernumMode:WaterOverlayShader"];
        #endregion

        #region Methods
        public static void LoadEffects()
        {
            var assets = InfernumMode.Instance.Assets;

            LoadRegularShaders(assets);
            LoadScreenShaders(assets);

            // Significantly dampen the effects of Boss Rush's color-muting shader.
            Filters.Scene["CalamityMod:BossRush"].GetShader().UseOpacity(0.367f);
        }

        public static void LoadRegularShaders(AssetRepository assets)
        {
            FluidSimulatorShader = assets.Request<Effect>("Assets/Effects/FluidSimulator", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:DrawFluidResult"] = new MiscShaderData(FluidSimulatorShader, "DrawResultPass");
            GameShaders.Misc["Infernum:FluidUpdateVelocity"] = new MiscShaderData(FluidSimulatorShader, "VelocityUpdatePass");
            GameShaders.Misc["Infernum:FluidUpdateVelocityVorticity"] = new MiscShaderData(FluidSimulatorShader, "VelocityUpdateVorticityPass");
            GameShaders.Misc["Infernum:FluidAdvect"] = new MiscShaderData(FluidSimulatorShader, "AdvectPass");

            var aewShadowShader = assets.Request<Effect>("Assets/Effects/Overlays/AEWShadowShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:AEWShadowForm"] = new MiscShaderData(aewShadowShader, "BurnPass");

            var areaBorder = assets.Request<Effect>("Assets/Effects/Shapes/AreaBorderShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:AreaBorder"] = new MiscShaderData(areaBorder, "TrailPass");

            var aresEnergySlashShader = assets.Request<Effect>("Assets/Effects/Primitives/AresEnergySlashShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:AresEnergySlash"] = new MiscShaderData(aresEnergySlashShader, "TrailPass");

            var aresLightningArcShader = assets.Request<Effect>("Assets/Effects/Primitives/AresLightningArcShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:AresLightningArc"] = new MiscShaderData(aresLightningArcShader, "TrailPass");

            var artemisLaserShader = assets.Request<Effect>("Assets/Effects/Primitives/ArtemisLaserShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:ArtemisLaser"] = new MiscShaderData(artemisLaserShader, "TrailPass");

            var backgroundDistortionShader = assets.Request<Effect>("Assets/Effects/Overlays/BackgroundDistortionShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:BackgroundDistortion"] = new MiscShaderData(backgroundDistortionShader, "DistortionPass");

            var basicTintShader = assets.Request<Effect>("Assets/Effects/Overlays/BasicTint", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:BasicTint"] = new MiscShaderData(basicTintShader, "BasicTint");

            var brainPsychicShader = assets.Request<Effect>("Assets/Effects/Primitives/BrainPsychicShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:BrainPsychic"] = new MiscShaderData(brainPsychicShader, "TrailPass");

            var cvBGShader = assets.Request<Effect>("Assets/Effects/Overlays/CeaselessVoidBackgroundShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:CVBackground"] = new MiscShaderData(cvBGShader, "ScreenPass");

            var cvCrackShader = assets.Request<Effect>("Assets/Effects/Cutouts/CeaselessVoidCrackShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:CVCrack"] = new MiscShaderData(cvCrackShader, "CrackPass");

            var cvPortalShader = assets.Request<Effect>("Assets/Effects/Shapes/CeaselessVoidPortalShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:CVPortal"] = new MiscShaderData(cvPortalShader, "ScreenPass");

            var cutoutShader = assets.Request<Effect>("Assets/Effects/Shapes/CircleCutoutShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:CircleCutout"] = new MiscShaderData(cutoutShader, "CutoutPass");

            cutoutShader = assets.Request<Effect>("Assets/Effects/Shapes/CircleCutoutShader2", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:CircleCutout2"] = new MiscShaderData(cutoutShader, "CutoutPass");

            var cloudShader = assets.Request<Effect>("Assets/Effects/Primitives/CloudShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:CloudShader"] = new MiscShaderData(cloudShader, "TrailPass");

            var cosmicBGShader = assets.Request<Effect>("Assets/Effects/Overlays/CosmicBackgroundShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:CosmicBackground"] = new MiscShaderData(cosmicBGShader, "CosmicPass");

            var cultistDeathAnimationShader = assets.Request<Effect>("Assets/Effects/Cutouts/CultistDeathAnimation", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:CultistDeath"] = new MiscShaderData(cultistDeathAnimationShader, "DeathPass");

            var cultistShield = assets.Request<Effect>("Assets/Effects/Shapes/CultistForcefield", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:CultistShield"] = new MiscShaderData(cultistShield, "ShieldPass");

            var darkFlamePillarShader = assets.Request<Effect>("Assets/Effects/Primitives/DarkFlamePillarShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:DarkFlamePillar"] = new MiscShaderData(darkFlamePillarShader, "TrailPass");

            var dashIndicator = assets.Request<Effect>("Assets/Effects/Primitives/DoGDashIndicatorShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:DoGDashIndicatorShader"] = new MiscShaderData(dashIndicator, "TrailPass");

            var dukeTornadoShader = assets.Request<Effect>("Assets/Effects/Primitives/DukeTornado", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:DukeTornado"] = new MiscShaderData(dukeTornadoShader, "TrailPass");

            var shadowflameShader = assets.Request<Effect>("Assets/Effects/Primitives/Shadowflame", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:Fire"] = new MiscShaderData(shadowflameShader, "TrailPass");

            var fishEyeShader = assets.Request<Effect>("Assets/Effects/SpriteDistortions/FisheyeShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:Fisheye"] = new MiscShaderData(fishEyeShader, "FisheyePass");

            var fogShader = assets.Request<Effect>("Assets/Effects/Overlays/FogShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:FogOverlay"] = new MiscShaderData(fogShader, "FogPass");

            var genericLaserShader = assets.Request<Effect>("Assets/Effects/Primitives/GenericLaserShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:GenericLaserShader"] = new MiscShaderData(genericLaserShader, "TrailPass");

            var guardiansShader = assets.Request<Effect>("Assets/Effects/Primitives/GuardiansLaserShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:GuardiansLaserShader"] = new MiscShaderData(guardiansShader, "TrailPass");

            var kevinLightningShader = assets.Request<Effect>("Assets/Effects/Shapes/KevinLightningShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:KevinLightning"] = new MiscShaderData(kevinLightningShader, "UpdatePass");

            var introShader = assets.Request<Effect>("Assets/Effects/Shapes/MechIntroLetterShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:MechsIntro"] = new MiscShaderData(introShader, "LetterPass");

            var noiseDisplacementShader = assets.Request<Effect>("Assets/Effects/SpriteDistortions/NoiseDisplacement", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:NoiseDisplacement"] = new MiscShaderData(noiseDisplacementShader, "GlitchPass");

            var ghostlyShader = assets.Request<Effect>("Assets/Effects/Primitives/PolterghastEctoplasmShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:PolterghastEctoplasm"] = new MiscShaderData(ghostlyShader, "BurstPass");

            var rayShader = assets.Request<Effect>("Assets/Effects/Primitives/PrismaticRayShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:PrismaticRay"] = new MiscShaderData(rayShader, "TrailPass");

            var profanedLavaShader = assets.Request<Effect>("Assets/Effects/Primitives/ProfanedLava", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:ProfanedLava"] = new MiscShaderData(profanedLavaShader, "TrailPass");

            var profanedPortal = assets.Request<Effect>("Assets/Effects/Shapes/ProfanedPortalShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:ProfanedPortal"] = new MiscShaderData(profanedPortal, "PortalPass");

            var proviLaserShader = assets.Request<Effect>("Assets/Effects/Primitives/ProviLaserShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:ProviLaserShader"] = new MiscShaderData(proviLaserShader, "TrailPass");

            var pulsatingLaser = assets.Request<Effect>("Assets/Effects/Primitives/PulsatingLaser", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:PulsatingLaserShader"] = new MiscShaderData(pulsatingLaser, "TrailPass");

            var realityTearShader = assets.Request<Effect>("Assets/Effects/Primitives/RealityTearShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:RealityTear"] = new MiscShaderData(realityTearShader, "TrailPass");

            realityTearShader = assets.Request<Effect>("Assets/Effects/Shapes/RealityTear2Shader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:RealityTear2"] = new MiscShaderData(realityTearShader, "TrailPass");

            introShader = assets.Request<Effect>("Assets/Effects/Shapes/SCalIntroLetterShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:SCalIntro"] = new MiscShaderData(introShader, "LetterPass");

            var streakShader = assets.Request<Effect>("Assets/Effects/Primitives/SideStreakTrail", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:SideStreak"] = new MiscShaderData(streakShader, "TrailPass");

            var signusBGShader = assets.Request<Effect>("Assets/Effects/Overlays/SignusBackgroundShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:SignusBackground"] = new MiscShaderData(signusBGShader, "ScreenPass");

            var screenInversionShader = assets.Request<Effect>("Assets/Effects/Shapes/ScreenInversionMetaballShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:ScreenInversionMetaball"] = new MiscShaderData(screenInversionShader, "UpdatePass");

            var codeScrollShader = assets.Request<Effect>("Assets/Effects/Primitives/ScrollingCodePrimShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:ScrollingCode"] = new MiscShaderData(codeScrollShader, "TrailPass");

            var teleportShader = assets.Request<Effect>("Assets/Effects/SpriteDistortions/TeleportShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:Teleport"] = new MiscShaderData(teleportShader, "HologramPass");

            var flameTrailShader = assets.Request<Effect>("Assets/Effects/Primitives/TwinsFlameTail", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:TwinsFlameTrail"] = new MiscShaderData(flameTrailShader, "TrailPass");

            var atThisTimeOfYear = assets.Request<Effect>("Assets/Effects/Shapes/UnderwaterRayShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:UnderwaterRays"] = new MiscShaderData(atThisTimeOfYear, "RayPass");

            var waterShader = assets.Request<Effect>("Assets/Effects/Primitives/WaterShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:WaterShader"] = new MiscShaderData(waterShader, "WaterPass");

            var bloodGeyserShader = assets.Request<Effect>("Assets/Effects/Primitives/BloodGeyser", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:WoFGeyserTexture"] = new MiscShaderData(bloodGeyserShader, "TrailPass");

            var tentacleFleshShader = assets.Request<Effect>("Assets/Effects/Primitives/TentacleTexture", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:WoFTentacleTexture"] = new MiscShaderData(tentacleFleshShader, "TrailPass");

            var yharonBurnShader = assets.Request<Effect>("Assets/Effects/Overlays/YharonBurnShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:YharonBurn"] = new MiscShaderData(yharonBurnShader, "BurnPass");

            var yharonInfernadoShader = assets.Request<Effect>("Assets/Effects/Primitives/YharonInfernadoShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["Infernum:YharonInfernado"] = new MiscShaderData(yharonInfernadoShader, "TrailPass");
        }

        public static void LoadScreenShaders(AssetRepository assets)
        {
            var waterShader = assets.Request<Effect>("Assets/Effects/Overlays/WaterShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:WaterOverlayShader"] = new Filter(new(waterShader, "WaterPass"), EffectPriority.VeryHigh);

            // CRT shader
            var crtShader = assets.Request<Effect>("Assets/Effects/SpriteDistortions/CRTShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:CRTShader"] = new Filter(new(crtShader, "CRTPass"), EffectPriority.VeryHigh);

            // Sandstorm shader
            var sandstormShader = assets.Request<Effect>("Assets/Effects/Overlays/SandstormShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:SandstormShader"] = new Filter(new(sandstormShader, "SandstormPass"), EffectPriority.VeryHigh);

            // Afterimage shader
            var afterimageShader = assets.Request<Effect>("Assets/Effects/SpriteDistortions/AfterimageShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:AfterimageShader"] = new Filter(new(afterimageShader, "AfterimagePass"), EffectPriority.VeryHigh);

            // Movie bar shader.
            var movieBarShader = assets.Request<Effect>("Assets/Effects/Overlays/MovieBarShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:MovieBarShader"] = new Filter(new(movieBarShader, "BarPass"), EffectPriority.VeryHigh);

            // Astral Dimension.
            Filters.Scene["InfernumMode:AstralDimension"] = new Filter(new PerforatorScreenShaderData("FilterMiniTower").UseColor(0f, 0f, 0f).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:AstralDimension"] = new AstralDimensionSky();

            // Deus gas shader.
            var deusGasShader = assets.Request<Effect>("Assets/Effects/Overlays/DeusGasShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:DeusGasShader"] = new Filter(new(deusGasShader, "GasPass"), EffectPriority.VeryHigh);

            // Sprite burn shader.
            var spriteBurnShader = assets.Request<Effect>("Assets/Effects/SpriteDistortions/SpriteBurnShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:SpriteBurn"] = new Filter(new(spriteBurnShader, "BurnPass"), EffectPriority.VeryHigh);

            // Pulse ring shader.
            var pulseRingShader = assets.Request<Effect>("Assets/Effects/Shapes/PulseRing", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:PulseRing"] = new Filter(new(pulseRingShader, "PulsePass"), EffectPriority.VeryHigh);

            // Crystal crack shader.
            var crystalCrackShader = assets.Request<Effect>("Assets/Effects/Cutouts/CrystalCrackShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:CrystalCrackShader"] = new Filter(new(crystalCrackShader, "CrackPass"), EffectPriority.VeryHigh);

            // DoG portal shader.
            var dogPortalShader = assets.Request<Effect>("Assets/Effects/Shapes/DoGPortalShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:DoGPortalShader"] = new Filter(new(dogPortalShader, "PortalPass"), EffectPriority.VeryHigh);

            // Base Metaball Edge shader
            var baseMetaballEdgeShader = assets.Request<Effect>("Assets/Effects/Metaballs/BaseMetaballEdgeShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:BaseMetaballEdgeShader"] = new Filter(new(baseMetaballEdgeShader, "EdgePass"), EffectPriority.VeryHigh);

            // Lightning Overlay shader.
            var lightningOverlayShader = assets.Request<Effect>("Assets/Effects/Overlays/LightningOverlayShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:LightningOverlay"] = new Filter(new(lightningOverlayShader, "LightningOverlayPass"), EffectPriority.VeryHigh);

            // Bossbar shader.
            var bossBarShader = assets.Request<Effect>("Assets/Effects/Overlays/BossBarShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:BossBar"] = new Filter(new(bossBarShader, "FilterPass"), EffectPriority.VeryHigh);

            // Raindrop shader.
            var rainShader = assets.Request<Effect>("Assets/Effects/SpriteDistortions/RaindropShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:Raindrops"] = new Filter(new(rainShader, "RainPass"), EffectPriority.VeryHigh);

            // Credits shader.
            var creditShader = assets.Request<Effect>("Assets/Effects/Overlays/CreditShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:Credits"] = new Filter(new(creditShader, "CreditPass"), EffectPriority.VeryHigh);

            // Bloom shader.
            var bloomShader = assets.Request<Effect>("Assets/Effects/Overlays/GaussianBlur", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:Bloom"] = new Filter(new(bloomShader, "BloomPass"), EffectPriority.VeryHigh);

            // Flower of the ocean sky.
            Filters.Scene["InfernumMode:FlowerOfTheOcean"] = new Filter(new PerforatorScreenShaderData("FilterMiniTower").UseColor(0f, 0f, 0f).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:FlowerOfTheOcean"] = new FlowerOceanSky();

            // Fireball shader.
            var fireballShader = assets.Request<Effect>("Assets/Effects/Shapes/FireballShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["Infernum:FireballShader"] = new Filter(new(fireballShader, "FirePass"), EffectPriority.VeryHigh);

            // Screen Border Shader.
            var screenBorderShader = assets.Request<Effect>("Assets/Effects/Overlays/ScreenBorderShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:ScreenBorder"] = new Filter(new ScreenShaderData(screenBorderShader, "ScreenPass"), EffectPriority.VeryHigh);

            Filters.Scene["InfernumMode:GuardianCommander"] = new Filter(new PerforatorScreenShaderData("FilterMiniTower").UseColor(0f, 0f, 0f).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:GuardianCommander"] = new ProfanedGuardiansSky();

            // Ares (ultimate attack).
            Filters.Scene["InfernumMode:Ares"] = new Filter(new AresScreenShaderData("FilterMiniTower").UseColor(Color.Red).UseOpacity(0.5f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Ares"] = new AresSky();

            // Astrum Deus.
            Filters.Scene["InfernumMode:Deus"] = new Filter(new DeusScreenShaderData("FilterMiniTower").UseColor(Color.Lerp(Color.Purple, Color.Black, 0.75f)).UseOpacity(0.24f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Deus"] = new DeusSky();

            // Calamitas' Shadow.
            Filters.Scene["InfernumMode:CalShadow"] = new Filter(new CalShadowScreenShaderData("FilterMiniTower").UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:CalShadow"] = new CalShadowSky();

            // Dragonfolly.
            Filters.Scene["InfernumMode:Dragonfolly"] = new Filter(new DragonfollyScreenShaderData("FilterMiniTower").UseColor(Color.Red).UseOpacity(0.6f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Dragonfolly"] = new DragonfollySky();

            // Devourer of Gods.
            Filters.Scene["InfernumMode:DoG"] = new Filter(new PerforatorScreenShaderData("FilterMiniTower").UseColor(0.4f, 0.1f, 1.0f).UseOpacity(0.5f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:DoG"] = new DoGSkyInfernum();

            // Empress of Light.
            var screenShader = assets.Request<Effect>("Assets/Effects/Overlays/EmpressOfLightScreenShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:EmpressOfLight"] = new Filter(new EmpressOfLightScreenShaderData(screenShader, "ScreenPass"), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:EmpressOfLight"] = new EmpressOfLightSky();

            // General screen shake distortion shaders.
            var screenShakeShader = assets.Request<Effect>("Assets/Effects/SpriteDistortions/ScreenShakeShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:ScreenShake"] = new Filter(new ScreenShaderData(screenShakeShader, "DyePass"), EffectPriority.VeryHigh);

            screenShakeShader = assets.Request<Effect>("Assets/Effects/SpriteDistortions/ScreenShockwaveShader2", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:ScreenShake2"] = new Filter(new ScreenShaderData(screenShakeShader, "DyePass"), EffectPriority.VeryHigh);

            // Heat distortion effect.
            var screenDistortionShader = assets.Request<Effect>("Assets/Effects/Overlays/ScreenDistortionShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:ScreenDistortion"] = new Filter(new ScreenShaderData(screenDistortionShader, "ScreenPass"), EffectPriority.VeryHigh);

            // Hive Mind.
            Filters.Scene["InfernumMode:HiveMind"] = new Filter(new HiveMindScreenShaderData("FilterMiniTower").UseColor(HiveMindSky.SkyColor).UseOpacity(0.6f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:HiveMind"] = new HiveMindSky();

            // Hyperplane Matrix time change sky.
            Filters.Scene["InfernumMode:HyperplaneMatrixTimeChange"] = new Filter(new PerforatorScreenShaderData("FilterMiniTower").UseColor(1f, 1f, 1f).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:HyperplaneMatrixTimeChange"] = new HyperplaneMatrixTimeChangeSky();

            // Deerclops.
            var madnessShader = assets.Request<Effect>("Assets/Effects/Overlays/Madness", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:Madness"] = new Filter(new MadnessScreenShaderData(madnessShader, "DyePass"), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Madness"] = new MadnessSky();

            // Moon Lord.
            var fireBGShader = assets.Request<Effect>("Assets/Effects/Overlays/SCalFireBGShader", AssetRequestMode.ImmediateLoad);
            Filters.Scene["InfernumMode:MoonLord"] = new Filter(new MLScreenShaderData(fireBGShader, "DyePass").UseOpacity(0.5f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:MoonLord"] = new MLSky();

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
            Filters.Scene["InfernumMode:SCal"] = new Filter(new SCalScreenShaderData(fireBGShader, "DyePass").UseColor(0.3f, 0f, 0f).UseOpacity(0.5f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:SCal"] = new SCalSkyInfernum();

            // Twins (desperation phase).
            Filters.Scene["InfernumMode:Twins"] = new Filter(new TwinsScreenShaderData("FilterMiniTower").UseColor(Color.Red).UseOpacity(0.5f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Twins"] = new TwinsSky();

            // Yharon.
            Filters.Scene["InfernumMode:Yharon"] = new Filter(new PerforatorScreenShaderData("FilterMiniTower").UseColor(0f, 0f, 0f).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Yharon"] = new YharonSky();
        }
        #endregion
        #endregion

        #region New
        #region Shaders
        public static ManagedShader AresEnergySlashShader => ShaderManager.GetShader("InfernumMode.AresEnergySlashShader");
        public static ManagedShader FlameVertexShader => ShaderManager.GetShader("InfernumMode.Flame");
        public static ManagedShader GaleLightningShader => ShaderManager.GetShader("InfernumMode.HeavenlyGaleLightningArc");
        public static ManagedShader ImpFlameTrailShader => ShaderManager.GetShader("InfernumMode.ImpFlameTrail");
        public static ManagedShader BaseMetaballEdgeShader => ShaderManager.GetShader("InfernumMode.BaseMetaballEdgeShader");
        public static ManagedShader RealityTearVertexShader => ShaderManager.GetShader("InfernumMode.RealityTearShader");
        #endregion
        #region Filters
        #endregion
        #endregion
    }
}
