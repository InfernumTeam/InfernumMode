using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Assets.Sounds
{
    public static class InfernumSoundRegistry
    {
        public static SoundStyle SafeLoadCalamitySound(string path, SoundStyle fallback)
        {
            if (!ModContent.HasAsset($"CalamityMod/{path}"))
                return fallback;

            return new($"CalamityMod/{path}");
        }

        #region Bosses and Enemies

        public static readonly SoundStyle AEWDeathAnimationSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/AEW/AEWDeathAnimation") with { Volume = 1.6f };

        public static readonly SoundStyle AEWEnergyCharge = new SoundStyle("InfernumMode/Assets/Sounds/Custom/AEW/AEWEnergyCharge") with { Volume = 1.5f };

        public static readonly SoundStyle AEWIceBurst = new("InfernumMode/Assets/Sounds/Custom/AEW/AEWIceBurst");

        public static readonly SoundStyle AEWThreatenRoar = new SoundStyle("InfernumMode/Assets/Sounds/Custom/AEW/AEWThreatenRoar") with { Volume = 1.64f };

        public static readonly SoundStyle AresLaughSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/AresLaugh");

        public static readonly SoundStyle AresTeslaShotSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/AresTeslaShot");

        public static readonly SoundStyle AresPulseCannonChargeSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/AresPulseCannonCharge");

        public static readonly SoundStyle ArtemisSpinLaserbeamSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/ArtemisSpinLaserbeam");

        public static readonly SoundStyle AquaticScourgeAcidHissLoopSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/AquaticScourge/AquaticScourgeAcidHissLoop") with { IsLooped = true };

        public static readonly SoundStyle AquaticScourgeAppearSound = new("InfernumMode/Assets/Sounds/Custom/AquaticScourge/AquaticScourgeAppear");

        public static readonly SoundStyle AquaticScourgeChargeSound = new("InfernumMode/Assets/Sounds/Custom/AquaticScourge/AquaticScourgeCharge");

        public static readonly SoundStyle AquaticScourgeGoreSound = new("InfernumMode/Assets/Sounds/Custom/AquaticScourge/AquaticScourgeGore");

        public static readonly SoundStyle AstrumAureusStompSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/AstrumAureus/AureusStomp") with { Volume = 1.5f };

        public static readonly SoundStyle AstrumAureusLaserSound = new("InfernumMode/Assets/Sounds/Custom/AstrumAureus/AureusLaser");

        public static readonly SoundStyle BrimstoneLaser = new("InfernumMode/Assets/Sounds/Custom/BrimstoneElemental/BrimstoneLaser");

        public static readonly SoundStyle BrimstoneElementalShellGroundHit = new("InfernumMode/Assets/Sounds/Custom/DeathAnimations/BrimstoneElementalShellGroundHit");

        public static readonly SoundStyle BubblePop = new("InfernumMode/Assets/Sounds/Custom/AquaticScourge/BubblePop");

        public static readonly SoundStyle CalThunderStrikeSound = new("CalamityMod/Sounds/Custom/ThunderStrike");

        public static readonly SoundStyle CalCloneDissipateSound = new("InfernumMode/Assets/Sounds/Custom/CalClone/CalamitasCloneDissipate");

        public static readonly SoundStyle CalCloneTeleportSound = new("InfernumMode/Assets/Sounds/Custom/CalClone/CalamitasCloneTeleport");

        public static readonly SoundStyle CeaselessVoidChainSound = new("InfernumMode/Assets/Sounds/Custom/CeaselessVoid/CeaselessVoidChain", 2);

        public static readonly SoundStyle CloudElementalWindSound = new("InfernumMode/Assets/Sounds/Custom/CloudElementalWind");

        public static readonly SoundStyle DeerclopsRubbleAttackDistortedSound = new("InfernumMode/Assets/Sounds/Custom/DeerclopsRubbleAttackDistorted");

        public static readonly SoundStyle DesertScourgeSandstormWindSound = new("InfernumMode/Assets/Sounds/Custom/DesertScourgeSandstormWind");

        public static readonly SoundStyle DesertScourgeShortRoar = new("InfernumMode/Assets/Sounds/Custom/DesertScourgeShortRoar");

        public static readonly SoundStyle DestroyerBombExplodeSound = new("InfernumMode/Assets/Sounds/Custom/Destroyer/DestroyerBombExplode");

        public static readonly SoundStyle DestroyerChargeImpactSound = new("InfernumMode/Assets/Sounds/Custom/Destroyer/DestroyerChargeImpact");

        public static readonly SoundStyle DestroyerChargeUpSound = new("InfernumMode/Assets/Sounds/Custom/Destroyer/DestroyerChargeUp");

        public static readonly SoundStyle DestroyerLaserTelegraphSound = new("InfernumMode/Assets/Sounds/Custom/Destroyer/DestroyerLaserTelegraph");

        public static readonly SoundStyle DevilfishRoarSound = new("InfernumMode/Assets/Sounds/Custom/DevilfishRoar");

        public static readonly SoundStyle DoGLaughSound = new("InfernumMode/Assets/Sounds/Custom/DoGLaugh");

        public static readonly SoundStyle EidolistChoirSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/EidolistChoir") with { IsLooped = true, Volume = 0.425f };

        public static readonly SoundStyle EntropyRayChargeSound = new("InfernumMode/Assets/Sounds/Custom/CalClone/EntropyRayCharge");

        public static readonly SoundStyle EntropyRayFireSound = new("InfernumMode/Assets/Sounds/Custom/CalClone/EntropyRayFire");

        public static readonly SoundStyle ExoMechFinalPhaseSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/ExoMechFinalPhaseChargeup");

        public static readonly SoundStyle ExoMechImpendingDeathSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/ExoMechImpendingDeathSound");

        public static readonly SoundStyle ExoMechIntroSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/ExoMechIntro");

        public static readonly SoundStyle GolemGroundHitSound = new("InfernumMode/Assets/Sounds/Custom/Golem/GolemGroundHit");

        public static readonly SoundStyle GolemSansSound = new("InfernumMode/Assets/Sounds/Custom/Golem/BadTime");

        public static readonly SoundStyle GolemSpamtonSound = new("InfernumMode/Assets/Sounds/Custom/Golem/[BIG SHOT]");

        public static readonly SoundStyle GreatSandSharkChargeRoarSound = new("InfernumMode/Assets/Sounds/Custom/BereftVassal/GreatSandSharkChargeRoar");

        public static readonly SoundStyle GreatSandSharkMiscRoarSound = new("InfernumMode/Assets/Sounds/Custom/BereftVassal/GreatSandSharkMiscRoar");

        public static readonly SoundStyle GreatSandSharkHitSound = new("InfernumMode/Assets/Sounds/NPCHit/GreatSandSharkHit", 3);

        public static readonly SoundStyle GreatSandSharkSpawnSound = new("InfernumMode/Assets/Sounds/Custom/BereftVassal/GreatSandSharkSpawnSound");

        public static readonly SoundStyle GreatSandSharkSuddenRoarSound = new("InfernumMode/Assets/Sounds/Custom/BereftVassal/GreatSandSharkSuddenRoar");

        public static readonly SoundStyle GulperEelScreamSound = new("InfernumMode/Assets/Sounds/Custom/GulperEelScream");

        public static readonly SoundStyle HeavyExplosionSound = new("InfernumMode/Assets/Sounds/Custom/HeavyExplosion");

        public static readonly SoundStyle KingSlimeDeathAnimation = new("InfernumMode/Assets/Sounds/Custom/DeathAnimations/KingSlimeDeathAnimation");

        public static readonly SoundStyle LeviathanRumbleSound = new("InfernumMode/Assets/Sounds/Custom/LeviathanSummonBase");

        public static readonly SoundStyle MoonLordIntroSound = new("InfernumMode/Assets/Sounds/Custom/MoonLordIntro");

        public static readonly SoundStyle ModeToggleLaugh = new("InfernumMode/Assets/Sounds/Custom/ModeToggleLaugh");

        public static readonly SoundStyle MyrindaelHitSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/BereftVassal/MyrindaelHit") with { Volume = 1.8f };

        public static readonly SoundStyle MyrindaelSpinSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/BereftVassal/MyrindaelSpin") with { Volume = 1.7f };

        public static readonly SoundStyle MyrindaelThrowSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/BereftVassal/MyrindaelThrow") with { Volume = 1.8f };

        public static readonly SoundStyle PBGMechanicalWarning = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGNukeWarning");

        public static readonly SoundStyle PBGNukeExplosionSound = new("InfernumMode/Assets/Sounds/Custom/PlaguebringerGoliath/PBGNukeExplosion");

        public static readonly SoundStyle PerforatorDeathAnimation = new("InfernumMode/Assets/Sounds/Custom/DeathAnimations/PerforatorDeathAnimation");

        public static readonly SoundStyle PolterghastDashSound = new("InfernumMode/Assets/Sounds/Custom/Polterghast/PolterDash");

        public static readonly SoundStyle PolterghastDeathEchoSound = new("InfernumMode/Assets/Sounds/Custom/DeathAnimations/PolterghastDeath");

        public static readonly SoundStyle PolterghastShortDashSound = new("InfernumMode/Assets/Sounds/Custom/Polterghast/PolterDashShort");

        public static readonly SoundStyle PolterghastSoulSound = new("InfernumMode/Assets/Sounds/Custom/Polterghast/PolterSoulVortexShoot");

        public static readonly SoundStyle PolterSoulVortexShootSound = new("InfernumMode/Assets/Sounds/Custom/Polterghast/PolterSoulShoot");

        public static readonly SoundStyle PrimeSawSound = new("InfernumMode/Assets/Sounds/Custom/PrimeSaw");

        public static readonly SoundStyle ProvidenceBlenderSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/Providence/ProvidenceBlender") with { Volume = 2f };

        public static readonly SoundStyle ProvidenceBurnSound = new("CalamityMod/Sounds/Custom/Providence/ProvidenceBurn");

        public static readonly SoundStyle ProvidenceCrystalPillarShatterSound = new("InfernumMode/Assets/Sounds/Custom/Providence/ProvidenceCrystalPillarShatter");

        public static readonly SoundStyle ProvidenceDoorShimmerSoundLoop = new("InfernumMode/Assets/Sounds/Custom/Providence/ProvidenceDoorSoundLoop");

        public static readonly SoundStyle ProvidenceDoorShatterSound = new("InfernumMode/Assets/Sounds/Custom/Providence/ProvidenceDoorShatter");

        public static readonly SoundStyle ProvidenceHolyBlastShootSound = new("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyBlastShoot");

        public static readonly SoundStyle ProvidenceHolyRaySound = new("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyRay");

        public static readonly SoundStyle ProvidenceLavaEruptionSound = new("InfernumMode/Assets/Sounds/Custom/Providence/ProvidenceLavaEruption");

        public static readonly SoundStyle ProvidenceLavaEruptionSmallSound = new("InfernumMode/Assets/Sounds/Custom/Providence/ProvidenceLavaEruptionSmall");

        public static readonly SoundStyle ProvidenceScreamSound = new("InfernumMode/Assets/Sounds/Custom/Providence/ProvidenceScream");

        public static readonly SoundStyle ProvidenceSpawnSuspenseSound = new("InfernumMode/Assets/Sounds/Custom/Providence/ProvidenceSpawnSuspense");

        public static readonly SoundStyle ProvidenceSpearHitSound = new("InfernumMode/Assets/Sounds/Custom/Providence/ProvidenceSpearHit");

        public static readonly SoundStyle QueenSlimeExplosionSound = new("InfernumMode/Assets/Sounds/Custom/QueenSlime/QueenSlimeExplosion");

        public static readonly SoundStyle ReaperSharkIceBreathSound = new("InfernumMode/Assets/Sounds/Custom/ReaperSharkIceBreath");

        public static readonly SoundStyle SCalBrothersSpawnSound = new("InfernumMode/Assets/Sounds/Custom/SupremeCalamitas/SCalBrothersSpawn");

        public static readonly SoundStyle ShadowHydraCharge = new("InfernumMode/Assets/Sounds/Custom/SupremeCalamitas/HydraCharge");

        public static readonly SoundStyle ShadowHydraSpawn = new("InfernumMode/Assets/Sounds/Custom/SupremeCalamitas/HydraSpawn");

        public static readonly SoundStyle SizzleSound = new("CalamityMod/Sounds/Custom/Providence/ProvidenceSizzle");

        public static readonly SoundStyle SkeletronHeadBonkSound = new("InfernumMode/Assets/Sounds/Custom/SkeletronHeadBonk");

        public static readonly SoundStyle SonicBoomSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/SonicBoom") with { Volume = 1.7f };

        public static readonly SoundStyle TerminusLaserbeamSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/AEW/TerminusLaserbeam") with { Volume = 1.75f };

        public static readonly SoundStyle TerminusPulseSound = new("InfernumMode/Assets/Sounds/Custom/AEW/TerminusPulse");

        public static readonly SoundStyle ThanatosLightRay = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/ThanatosLightRay");

        public static readonly SoundStyle ThanatosTransitionSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/ThanatosTransition");

        public static readonly SoundStyle VassalAngerSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/BereftVassal/VassalAnger") with { Volume = 1.5f };

        public static readonly SoundStyle VassalHitSound = new("InfernumMode/Assets/Sounds/NPCHit/VassalHit", 3);

        public static readonly SoundStyle VassalHornSound = new("InfernumMode/Assets/Sounds/Custom/BereftVassal/VassalHornSound");

        public static readonly SoundStyle VassalJumpSound = new("InfernumMode/Assets/Sounds/Custom/BereftVassal/VassalJump");

        public static readonly SoundStyle VassalSlashSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/BereftVassal/VassalSlash") with { Volume = 1.5f };

        public static readonly SoundStyle VassalTeleportSound = new("InfernumMode/Assets/Sounds/Custom/BereftVassal/VassalTeleport");

        public static readonly SoundStyle VassalWaterBeamSound = new("InfernumMode/Assets/Sounds/Custom/BereftVassal/VassalWaterBeam");

        public static readonly SoundStyle WyrmChargeSound = new("InfernumMode/Assets/Sounds/Custom/WyrmElectricCharge");

        public const int AresTelegraphSoundLength = 183;
        #endregion Bosses and Enemies

        #region Items

        public static readonly SoundStyle GlassmakerFireStartSound = new("InfernumMode/Assets/Sounds/Item/GlassmakerIntro");

        public static readonly SoundStyle GlassmakerFireSound = new("InfernumMode/Assets/Sounds/Item/GlassmakerFire");

        public static readonly SoundStyle GlassmakerFireEndSound = new("InfernumMode/Assets/Sounds/Item/GlassmakerOutro");

        public static readonly SoundStyle HalibutSpotlight = new("InfernumMode/Assets/Sounds/Item/HalibutSpotlight");

        public static readonly SoundStyle HyperplaneMatrixActivateSound = new("InfernumMode/Assets/Sounds/Item/HyperplaneMatrixActivate");

        public static readonly SoundStyle WayfinderCreateSound = new("InfernumMode/Assets/Sounds/Item/WayfinderCreate");

        public static readonly SoundStyle WayfinderDestroySound = new("InfernumMode/Assets/Sounds/Item/WayfinderDestroy");

        public static readonly SoundStyle WayfinderFail = new("InfernumMode/Assets/Sounds/Item/WayfinderFail");

        public static readonly SoundStyle WayfinderTeleport = new("InfernumMode/Assets/Sounds/Item/WayfinderTeleport");

        #endregion Items

        #region Miscellaneous

        public static readonly SoundStyle InfernumAchievementCompletionSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/InfernumAchievementComplete") with { Volume = 1.5f };

        public static readonly SoundStyle WayfinderGateLoop = new("InfernumMode/Assets/Sounds/Custom/WayfinderGateLoop");

        public static readonly SoundStyle WayfinderObtainSound = new("InfernumMode/Assets/Sounds/Custom/WayfinderObtainSound");

        #endregion
    }
}