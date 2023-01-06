using CalamityMod.Cooldowns;
using InfernumMode.Assets.Effects;
using InfernumMode.Content.BossIntroScreens;
using InfernumMode.Content.Items;
using InfernumMode.Core;
using InfernumMode.Core.Balancing;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.Netcode;
using InfernumMode.Core.OverridingSystem;
using InfernumMode.ILEditingStuff;
using System.IO;
using Terraria;
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

        internal static bool CanUseCustomAIs => WorldSaveSystem.InfernumMode;

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
            Main.RunOnMainThread(HookManager.Load);

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
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/CryogenMapIcon", -1);
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/Dreadnautilus/DreadnautilusMapIcon", -1);
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/SupremeCalamitas/SepulcherMapIcon", -1);

                InfernumEffectsRegistry.LoadEffects();
            }

            CooldownRegistry.RegisterModCooldowns(this);

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

        public override void HandlePacket(BinaryReader reader, int whoAmI) => PacketHandler.ReceivePacket(this, reader, whoAmI);

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