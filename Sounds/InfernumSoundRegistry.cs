using System.Threading;
using Terraria.Audio;

namespace InfernumMode.Sounds
{
    public static class InfernumSoundRegistry
    {
        #region Bosses
        public static readonly SoundStyle AresLaughSound = new("InfernumMode/Sounds/Custom/AresLaugh");

        public static readonly SoundStyle AresTeslaShotSound = new("InfernumMode/Sounds/Custom/AresTeslaShot");

        public static readonly SoundStyle AresPulseCannonChargeSound = new("InfernumMode/Sounds/Custom/AresPulseCannonCharge");

        public static readonly SoundStyle CalThunderStrikeSound = new("CalamityMod/Sounds/Custom/ThunderStrike");

        public static readonly SoundStyle DeerclopsRubbleAttackDistortedSound = new("InfernumMode/Sounds/Custom/DeerclopsRubbleAttackDistorted");

        public static readonly SoundStyle DesertScourgeSandstormWindSound = new("InfernumMode/Sounds/Custom/DesertScourgeSandstormWind");

        public static readonly SoundStyle DesertScourgeShortRoar = new("InfernumMode/Sounds/Custom/DesertScourgeShortRoar");

        public static readonly SoundStyle DoGLaughSound = new("InfernumMode/Sounds/Custom/DoGLaugh");

        public static readonly SoundStyle ExoMechFinalPhaseSound = new("InfernumMode/Sounds/Custom/ExoMechFinalPhaseChargeup");

        public static readonly SoundStyle ExoMechImpendingDeathSound = new("InfernumMode/Sounds/Custom/ExoMechImpendingDeathSound");

        public static readonly SoundStyle ExoMechIntroSound = new("InfernumMode/Sounds/Custom/ExoMechIntro");

        public static readonly SoundStyle GolemSansSound = new("InfernumMode/Sounds/Custom/BadTime");

        public static readonly SoundStyle GolemSpamtonSound = new("InfernumMode/Sounds/Custom/[BIG SHOT]");

        public static readonly SoundStyle GreatSandSharkChargeRoarSound = new("InfernumMode/Sounds/Custom/GreatSandSharkChargeRoar");

        public static readonly SoundStyle GreatSandSharkMiscRoarSound = new("InfernumMode/Sounds/Custom/GreatSandSharkMiscRoar");

        public static readonly SoundStyle GreatSandSharkHitSound = new("InfernumMode/Sounds/NPCHit/GreatSandSharkHit", 3);

        public static readonly SoundStyle GreatSandSharkSpawnSound = new("InfernumMode/Sounds/Custom/GreatSandSharkSpawnSound");

        public static readonly SoundStyle GreatSandSharkSuddenRoarSound = new("InfernumMode/Sounds/Custom/GreatSandSharkSuddenRoar");

        public static readonly SoundStyle HeavyExplosionSound = new("InfernumMode/Sounds/Custom/HeavyExplosion");

        public static readonly SoundStyle LeviathanRumbleSound = new("InfernumMode/Sounds/Custom/LeviathanSummonBase");
        
        public static readonly SoundStyle MoonLordIntroSound = new("InfernumMode/Sounds/Custom/MoonLordIntro");

        public static readonly SoundStyle MyrindaelHitSound = new SoundStyle("InfernumMode/Sounds/Custom/MyrindaelHit") with { Volume = 1.8f };

        public static readonly SoundStyle MyrindaelSpinSound = new SoundStyle("InfernumMode/Sounds/Custom/MyrindaelSpin") with { Volume = 1.7f };

        public static readonly SoundStyle MyrindaelThrowSound = new SoundStyle("InfernumMode/Sounds/Custom/MyrindaelThrow") with { Volume = 1.8f };

        public static readonly SoundStyle PBGMechanicalWarning = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGNukeWarning");

        public static readonly SoundStyle PerforatorDeathAnimation = new("InfernumMode/Sounds/Custom/PerforatorDeathAnimation");

        public static readonly SoundStyle PolterghastDash = new("InfernumMode/Sounds/Custom/PolterDash");

        public static readonly SoundStyle PolterghastShortDash = new("InfernumMode/Sounds/Custom/PolterDashShort");

        public static readonly SoundStyle PoltergastDeathEcho = new("InfernumMode/Sounds/Custom/PolterghastDeath");

        public static readonly SoundStyle ProvidenceHolyBlastShootSound = new("CalamityMod/Sounds/Custom/ProvidenceHolyBlastShoot");

        public static readonly SoundStyle ProvidenceHolyRaySound = new("CalamityMod/Sounds/Custom/ProvidenceHolyRay");

        public static readonly SoundStyle ProvidenceDoorShimmerSoundLoop = new("InfernumMode/Sounds/Custom/ProvidenceDoorSoundLoop");

        public static readonly SoundStyle ProvidenceDoorShatterSound = new("InfernumMode/Sounds/Custom/ProvidenceDoorShatter");

        public static readonly SoundStyle SCalBrothersSpawnSound = new("InfernumMode/Sounds/Custom/SCalBrothersSpawn");

        public static readonly SoundStyle SonicBoomSound = new SoundStyle("InfernumMode/Sounds/Custom/SonicBoom") with { Volume = 1.7f };

        public static readonly SoundStyle ThanatosTransitionSound = new("InfernumMode/Sounds/Custom/ThanatosTransition");

        public static readonly SoundStyle VassalHitSound = new("InfernumMode/Sounds/NPCHit/VassalHit", 3);

        public static readonly SoundStyle VassalHornSound = new("InfernumMode/Sounds/Custom/VassalHornSound");

        public static readonly SoundStyle VassalJumpSound = new("InfernumMode/Sounds/Custom/VassalJump");

        public static readonly SoundStyle VassalSlashSound = new SoundStyle("InfernumMode/Sounds/Custom/VassalSlash") with { Volume = 1.5f };

        public static readonly SoundStyle VassalTeleportSound = new("InfernumMode/Sounds/Custom/VassalTeleport");

        public static readonly SoundStyle VassalWaterBeamSound = new("InfernumMode/Sounds/Custom/VassalWaterBeam");

        public static readonly SoundStyle WyrmChargeSound = new("InfernumMode/Sounds/Custom/WyrmElectricCharge");

        public const int AresTelegraphSoundLength = 183;
        #endregion Bosses

        #region Items

        public static readonly SoundStyle GlassmakerFireStartSound = new("InfernumMode/Sounds/Item/GlassmakerIntro");

        public static readonly SoundStyle GlassmakerFireSound = new("InfernumMode/Sounds/Item/GlassmakerFire");

        public static readonly SoundStyle GlassmakerFireEndSound = new("InfernumMode/Sounds/Item/GlassmakerOutro");

        #endregion Items
    }
}