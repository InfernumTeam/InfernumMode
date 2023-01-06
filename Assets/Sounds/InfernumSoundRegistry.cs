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

        #region Bosses
        public static readonly SoundStyle AresLaughSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/AresLaugh");

        public static readonly SoundStyle AresTeslaShotSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/AresTeslaShot");

        public static readonly SoundStyle AresPulseCannonChargeSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/AresPulseCannonCharge");

        public static readonly SoundStyle ArtemisSpinLaserbeamSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/ArtemisSpinLaserbeam");

        public static readonly SoundStyle BrimstoneElementalShellGroundHit = new("InfernumMode/Assets/Sounds/Custom/DeathAnimations/BrimstoneElementalShellGroundHit");

        public static readonly SoundStyle CalThunderStrikeSound = new("CalamityMod/Sounds/Custom/ThunderStrike");

        public static readonly SoundStyle CloudElementalWindSound = new("InfernumMode/Assets/Sounds/Custom/CloudElementalWind");

        public static readonly SoundStyle DeerclopsRubbleAttackDistortedSound = new("InfernumMode/Assets/Sounds/Custom/DeerclopsRubbleAttackDistorted");

        public static readonly SoundStyle DesertScourgeSandstormWindSound = new("InfernumMode/Assets/Sounds/Custom/DesertScourgeSandstormWind");

        public static readonly SoundStyle DesertScourgeShortRoar = new("InfernumMode/Assets/Sounds/Custom/DesertScourgeShortRoar");

        public static readonly SoundStyle DoGLaughSound = new("InfernumMode/Assets/Sounds/Custom/DoGLaugh");

        public static readonly SoundStyle ExoMechFinalPhaseSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/ExoMechFinalPhaseChargeup");

        public static readonly SoundStyle ExoMechImpendingDeathSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/ExoMechImpendingDeathSound");

        public static readonly SoundStyle ExoMechIntroSound = new("InfernumMode/Assets/Sounds/Custom/ExoMechs/ExoMechIntro");

        public static readonly SoundStyle GolemSansSound = new("InfernumMode/Assets/Sounds/Custom/BadTime");

        public static readonly SoundStyle GolemSpamtonSound = new("InfernumMode/Assets/Sounds/Custom/[BIG SHOT]");

        public static readonly SoundStyle GreatSandSharkChargeRoarSound = new("InfernumMode/Assets/Sounds/Custom/BereftVassal/GreatSandSharkChargeRoar");

        public static readonly SoundStyle GreatSandSharkMiscRoarSound = new("InfernumMode/Assets/Sounds/Custom/BereftVassal/GreatSandSharkMiscRoar");

        public static readonly SoundStyle GreatSandSharkHitSound = new("InfernumMode/Assets/Sounds/NPCHit/GreatSandSharkHit", 3);

        public static readonly SoundStyle GreatSandSharkSpawnSound = new("InfernumMode/Assets/Sounds/Custom/BereftVassal/GreatSandSharkSpawnSound");

        public static readonly SoundStyle GreatSandSharkSuddenRoarSound = new("InfernumMode/Assets/Sounds/Custom/BereftVassal/GreatSandSharkSuddenRoar");

        public static readonly SoundStyle HeavyExplosionSound = new("InfernumMode/Assets/Sounds/Custom/HeavyExplosion");

        public static readonly SoundStyle KingSlimeDeathAnimation = new("InfernumMode/Assets/Sounds/Custom/DeathAnimations/KingSlimeDeathAnimation");

        public static readonly SoundStyle LeviathanRumbleSound = new("InfernumMode/Assets/Sounds/Custom/LeviathanSummonBase");

        public static readonly SoundStyle MoonLordIntroSound = new("InfernumMode/Assets/Sounds/Custom/MoonLordIntro");

        public static readonly SoundStyle ModeToggleLaugh = new("InfernumMode/Assets/Sounds/Custom/ModeToggleLaugh");

        public static readonly SoundStyle MyrindaelHitSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/MyrindaelHit") with { Volume = 1.8f };

        public static readonly SoundStyle MyrindaelSpinSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/MyrindaelSpin") with { Volume = 1.7f };

        public static readonly SoundStyle MyrindaelThrowSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/MyrindaelThrow") with { Volume = 1.8f };

        public static readonly SoundStyle PBGMechanicalWarning = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGNukeWarning");

        public static readonly SoundStyle PerforatorDeathAnimation = new("InfernumMode/Assets/Sounds/Custom/DeathAnimations/PerforatorDeathAnimation");

        public static readonly SoundStyle PolterghastDash = new("InfernumMode/Assets/Sounds/Custom/PolterDash");

        public static readonly SoundStyle PolterghastShortDash = new("InfernumMode/Assets/Sounds/Custom/PolterDashShort");

        public static readonly SoundStyle PoltergastDeathEcho = new("InfernumMode/Assets/Sounds/Custom/DeathAnimations/PolterghastDeath");

        public static readonly SoundStyle PrimeSawSound = new("InfernumMode/Assets/Sounds/Custom/PrimeSaw");

        public static readonly SoundStyle ProvidenceHolyBlastShootSound = new("CalamityMod/Sounds/Custom/ProvidenceHolyBlastShoot");

        public static readonly SoundStyle ProvidenceBlenderSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/ProvidenceBlender") with { Volume = 2f };

        public static readonly SoundStyle ProvidenceHolyRaySound = new("CalamityMod/Sounds/Custom/ProvidenceHolyRay");

        public static readonly SoundStyle ProvidenceDoorShimmerSoundLoop = new("InfernumMode/Assets/Sounds/Custom/ProvidenceDoorSoundLoop");

        public static readonly SoundStyle ProvidenceDoorShatterSound = new("InfernumMode/Assets/Sounds/Custom/ProvidenceDoorShatter");

        public static readonly SoundStyle SCalBrothersSpawnSound = new("InfernumMode/Assets/Sounds/Custom/SCalBrothersSpawn");

        public static readonly SoundStyle SkeletronHeadBonkSound = new("InfernumMode/Assets/Sounds/Custom/SkeletronHeadBonk");

        public static readonly SoundStyle SonicBoomSound = new SoundStyle("InfernumMode/Assets/Sounds/Custom/SonicBoom") with { Volume = 1.7f };

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
        #endregion Bosses

        #region Items

        public static readonly SoundStyle GlassmakerFireStartSound = new("InfernumMode/Assets/Sounds/Item/GlassmakerIntro");

        public static readonly SoundStyle GlassmakerFireSound = new("InfernumMode/Assets/Sounds/Item/GlassmakerFire");

        public static readonly SoundStyle GlassmakerFireEndSound = new("InfernumMode/Assets/Sounds/Item/GlassmakerOutro");

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