global using static System.MathF;
global using static Microsoft.Xna.Framework.MathHelper;
using CalamityMod.Cooldowns;
using CalamityMod.Systems;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.BossBars;
using InfernumMode.Content.UI;
using InfernumMode.Core.Balancing;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.ILEditingStuff;
using InfernumMode.Core.Netcode;
using InfernumMode.Core.OverridingSystem;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using InfernumMode.Core.ModCalls;

namespace InfernumMode
{
    public class InfernumMode : Mod
    {
        internal static InfernumMode Instance;

        internal static Mod CalamityMod;

        internal static Mod CalamityModMusic;

        internal static Mod InfernumMusicMod;

        internal static Mod FargosMutantMod;

        internal static Mod FargowiltasSouls;

        internal static Mod PhaseIndicator;

        public static bool MusicModIsActive
        {
            get;
            private set;
        }

        public static bool CalMusicModIsActive
        {
            get;
            private set;
        }

        public static bool CanUseCustomAIs => WorldSaveSystem.InfernumModeEnabled;

        public static float BlackFade
        {
            get;
            set;
        }

        public static float DraedonThemeTimer
        {
            get;
            set;
        }

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
            MusicModIsActive = ModLoader.TryGetMod("InfernumModeMusic", out InfernumMusicMod);
            CalMusicModIsActive = ModLoader.TryGetMod("CalamityModMusic", out CalamityModMusic);

            BalancingChangesManager.Load();
            Main.RunOnMainThread(HookManager.Load);

            // Manually invoke the attribute constructors to get the marked methods cached.
            foreach (var type in typeof(InfernumMode).Assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(Utilities.UniversalBindingFlags))
                    method.GetCustomAttributes(false);
            }

            NPCBehaviorOverride.LoadAll();
            ProjectileBehaviorOverride.LoadAll();
            BossBarManager.LoadPhaseInfo();

            if (Main.netMode != NetmodeID.Server)
            {
                // Cryogen.
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/CryogenMapIcon", -1);

                // Dreadnautilus.
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/Dreadnautilus/DreadnautilusMapIcon", -1);

                // Calamitas' Shadow.
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasShadow/CalShadowMapIcon", -1);
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasShadow/CataclysmMapIcon", -1);
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasShadow/CatastropheMapIcon", -1);

                // Devourer of Gods.
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP1HeadMapIcon", -1);
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP1TailMapIcon", -1);
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2HeadMapIcon", -1);
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2BodyMapIcon", -1);
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2TailMapIcon", -1);

                // Calamitas.
                AddBossHeadTexture("InfernumMode/Content/BehaviorOverrides/BossAIs/SupremeCalamitas/SepulcherMapIcon", -1);

                InfernumEffectsRegistry.LoadEffects();
            }

            CooldownRegistry.RegisterModCooldowns(this);

            if (Main.netMode != NetmodeID.Server)
            {
                Main.QueueMainThreadAction(() =>
                {
                    CalamityMod.Call("LoadParticleInstances", this);
                });
            }

            // This is now the official way to add difficulties. A Mod.Call also exists, but this is a bit more efficient.
            InfernumDifficulty difficulty = new();
            DifficultyModeSystem.Difficulties.Add(difficulty);
            DifficultyModeSystem.CalculateDifficultyData();
        }

        public override void PostSetupContent()
        {
            NPCBehaviorOverride.LoadPhaseIndicators();
            Utilities.UpdateMapIconList();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI) => PacketManager.ReceivePacket(reader);

        public override object Call(params object[] args) => InfernumModCalls.Call(args);

        public override void Unload()
        {
            BalancingChangesManager.Unload();
            HookManager.Unload();

            Main.QueueMainThreadAction(() =>
            {
                PrimitiveTrailCopy.Dispose();
                Primitive3DStrip.Dispose();
            });

            Instance = null;
            CalamityMod = null;
        }
    }
}
