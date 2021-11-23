using CalamityMod.Events;
using InfernumMode.BehaviorOverrides.BossAIs.Cryogen;
using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using InfernumMode.BossRush;
using InfernumMode.ILEditingStuff;
using InfernumMode.Items;
using InfernumMode.OverridingSystem;
using InfernumMode.Skies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode
{
    public class InfernumMode : Mod
    {
        internal static InfernumMode Instance = null;
        internal static Mod CalamityMod = null;
        internal static bool CanUseCustomAIs => (!BossRushEvent.BossRushActive || BossRushApplies) && PoDWorld.InfernumMode;

        internal static bool BossRushApplies => false;

        internal static readonly Color HiveMindSkyColor = new Color(53, 42, 81);

        public static float BlackFade = 0f;

        public override void Load()
        {
            OverridingListManager.Load();
            ILEditingChanges.ILEditingLoad();

            Instance = this;
            CalamityMod = ModLoader.GetMod("CalamityMod");

            Filters.Scene["InfernumMode:HiveMind"] = new Filter(new HiveMindScreenShaderData("FilterMiniTower").UseColor(HiveMindSkyColor).UseOpacity(0.6f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:HiveMind"] = new HiveMindSky();

            Filters.Scene["InfernumMode:Dragonfolly"] = new Filter(new DragonfollyScreenShaderData("FilterMiniTower").UseColor(Color.Red).UseOpacity(0.6f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Dragonfolly"] = new DragonfollySky();

            Filters.Scene["InfernumMode:Deus"] = new Filter(new DeusScreenShaderData("FilterMiniTower").UseColor(Color.Lerp(Color.Purple, Color.Black, 0.75f)).UseOpacity(0.24f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Deus"] = new DeusSky();

            // Manually invoke the attribute constructors to get the marked methods cached.
            foreach (var type in typeof(InfernumMode).Assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(Utilities.UniversalBindingFlags))
                    method.GetCustomAttributes(false);
            }

            NPCBehaviorOverride.LoadAll();
            ProjectileBehaviorOverride.LoadAll();

            if (Main.netMode != NetmodeID.Server)
			{
                CryogenBehaviorOverride.SetupCustomBossIcon();

                Ref<Effect> portalShader = new Ref<Effect>(GetEffect("Effects/DoGPortalShader"));
                Filters.Scene["Infernum:DoGPortal"] = new Filter(new ScreenShaderData(portalShader, "ScreenPass"), EffectPriority.High);
                Filters.Scene["Infernum:DoGPortal"].Load();

                Ref<Effect> gradientShader = new Ref<Effect>(GetEffect("Effects/GradientWingShader"));
                GameShaders.Misc["Infernum:GradientWingShader"] = new MiscShaderData(gradientShader, "GradientPass");

                Ref<Effect> cyclicHueShader = new Ref<Effect>(GetEffect("Effects/CyclicHueShader"));
                GameShaders.Misc["Infernum:CyclicHueShader"] = new MiscShaderData(cyclicHueShader, "OutlineShader");

                Ref<Effect> pristineArmorShader = new Ref<Effect>(GetEffect("Effects/PristineArmorShader"));
                GameShaders.Misc["Infernum:PristineArmorShader"] = new MiscShaderData(pristineArmorShader, "PristinePass");

                Ref<Effect> dukeTornadoShader = new Ref<Effect>(GetEffect("Effects/DukeTornado"));
                GameShaders.Misc["Infernum:DukeTornado"] = new MiscShaderData(dukeTornadoShader, "TrailPass");

                Ref<Effect> tentacleFleshShader = new Ref<Effect>(GetEffect("Effects/TentacleTexture"));
                GameShaders.Misc["Infernum:WoFTentacleTexture"] = new MiscShaderData(tentacleFleshShader, "TrailPass");

                Ref<Effect> bloodGeyserShader = new Ref<Effect>(GetEffect("Effects/BloodGeyser"));
                GameShaders.Misc["Infernum:WoFGeyserTexture"] = new MiscShaderData(bloodGeyserShader, "TrailPass");

                Ref<Effect> shadowflameShader = new Ref<Effect>(GetEffect("Effects/Shadowflame"));
                GameShaders.Misc["Infernum:Fire"] = new MiscShaderData(shadowflameShader, "TrailPass");

                Ref<Effect> brainPsychicShader = new Ref<Effect>(GetEffect("Effects/BrainPsychicShader"));
                GameShaders.Misc["Infernum:BrainPsychic"] = new MiscShaderData(brainPsychicShader, "TrailPass");

                Ref<Effect> cultistDeathAnimationShader = new Ref<Effect>(GetEffect("Effects/CultistDeathAnimation"));
                GameShaders.Misc["Infernum:CultistDeath"] = new MiscShaderData(cultistDeathAnimationShader, "DeathPass");

                Ref<Effect> flameTrailShader = new Ref<Effect>(GetEffect("Effects/TwinsFlameTail"));
                GameShaders.Misc["Infernum:TwinsFlameTrail"] = new MiscShaderData(flameTrailShader, "TrailPass");

                Ref<Effect> ghostlyShader = new Ref<Effect>(GetEffect("Effects/EidolicWailRingShader"));
                GameShaders.Misc["Infernum:PolterghastEctoplasm"] = new MiscShaderData(ghostlyShader, "BurstPass");

                ghostlyShader = new Ref<Effect>(GetEffect("Effects/NecroplasmicRoarShader"));
                GameShaders.Misc["Infernum:NecroplasmicRoar"] = new MiscShaderData(ghostlyShader, "BurstPass");

                OverrideMusicBox(ItemID.MusicBoxBoss3, GetSoundSlot(SoundType.Music, "Sounds/Music/Boss3"), TileID.MusicBoxes, 36 * 12);
            }

            if (BossRushApplies)
                BossRushChanges.Load();
        }

        internal static IDictionary<int, int> SoundLoaderMusicToItem => (IDictionary<int, int>)typeof(SoundLoader).GetField("musicToItem", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        internal static IDictionary<int, int> SoundLoaderItemToMusic => (IDictionary<int, int>)typeof(SoundLoader).GetField("itemToMusic", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        internal static IDictionary<int, IDictionary<int, int>> SoundLoaderTileToMusic => (IDictionary<int, IDictionary<int, int>>)typeof(SoundLoader).GetField("tileToMusic", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        public static void OverrideMusicBox(int itemType, int musicSlot, int tileType, int tileFrameY)
        {
            SoundLoaderMusicToItem[musicSlot] = itemType;
            SoundLoaderItemToMusic[itemType] = musicSlot;
            if (!SoundLoaderTileToMusic.ContainsKey(tileType))
                SoundLoaderTileToMusic[tileType] = new Dictionary<int, int>();

            SoundLoaderTileToMusic[tileType][tileFrameY] = musicSlot;
        }

        public override void UpdateMusic(ref int music, ref MusicPriority priority)
        {
            if (NPC.AnyNPCs(NPCID.SkeletronHead))
            {
                music = Instance.GetSoundSlot(SoundType.Music, "Sounds/Music/Boss3");
                priority = MusicPriority.BossLow;
            }

            if (NPC.AnyNPCs(NPCID.SkeletronPrime) || NPC.AnyNPCs(NPCID.Retinazer) || NPC.AnyNPCs(NPCID.Spazmatism) || NPC.AnyNPCs(NPCID.TheDestroyer))
            {
                music = Instance.GetSoundSlot(SoundType.Music, "Sounds/Music/MechBosses");
                priority = MusicPriority.BossLow;
            }
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI) => NetcodeHandler.ReceivePacket(this, reader, whoAmI);

        public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
		{
            if (msgType == MessageID.SyncNPC)
			{
                NPC npc = Main.npc[number];

                ModPacket packet = GetPacket();
                packet.Write((short)InfernumPacketType.SendExtraNPCData);
                packet.Write(npc.whoAmI);
                packet.Write(npc.Infernum().TotalAISlotsInUse);
                for (int i = 0; i < npc.Infernum().ExtraAI.Length; i++)
				{
                    if (!npc.Infernum().HasAssociatedAIBeenUsed[i])
                        continue;
                    packet.Write(i);
                    packet.Write(npc.Infernum().ExtraAI[i]);
                }
                packet.Send();
            }
			return base.HijackSendData(whoAmI, msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
		}

        public override void AddRecipes() => RecipeUpdates.Update();

        public override void Unload()
        {
            OverridingListManager.Unload();
            ILEditingChanges.ILEditingUnload();
            Instance = null;
            CalamityMod = null;
        }

        public override void PreUpdateEntities()
        {
            BlackFade = MathHelper.Clamp(BlackFade - 0.025f, 0f, 1f);
            TwinsAttackSynchronizer.DoUniversalUpdate();
            TwinsAttackSynchronizer.PostUpdateEffects();
        }
    }
}