using CalamityMod.Events;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.Providence;
using CalamityMod.Particles;
using InfernumMode.Balancing;
using InfernumMode.BehaviorOverrides.BossAIs.Cryogen;
using InfernumMode.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena;
using InfernumMode.BehaviorOverrides.BossAIs.MoonLord;
using InfernumMode.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using InfernumMode.BossIntroScreens;
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
using Terraria.UI;

using static CalamityMod.CalamityMod;

namespace InfernumMode
{
    public class InfernumMode : Mod
    {
        internal static InfernumMode Instance = null;

        internal static Mod CalamityMod = null;

        internal static bool CanUseCustomAIs => (!BossRushEvent.BossRushActive || BossRushApplies) && PoDWorld.InfernumMode;

        internal static bool BossRushApplies => true;

        internal static readonly Color HiveMindSkyColor = new Color(53, 42, 81);

        public static float BlackFade = 0f;

        public static float DraedonThemeTimer = 0f;

        public static float ProvidenceArenaTimer
        {
            get;
            set;
        }

        public override void Load()
        {
            Instance = this;
            CalamityMod = ModLoader.GetMod("CalamityMod");

            OverridingListManager.Load();
            BalancingChangesManager.Load();
            HookManager.Load();

            Filters.Scene["InfernumMode:HiveMind"] = new Filter(new HiveMindScreenShaderData("FilterMiniTower").UseColor(HiveMindSkyColor).UseOpacity(0.6f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:HiveMind"] = new HiveMindSky();

            Filters.Scene["InfernumMode:Perforators"] = new Filter(new PerforatorScreenShaderData("FilterMiniTower").UseColor(new Color(255, 60, 30)).UseOpacity(0.445f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Perforators"] = new PerforatorSky();

            Filters.Scene["InfernumMode:Dragonfolly"] = new Filter(new DragonfollyScreenShaderData("FilterMiniTower").UseColor(Color.Red).UseOpacity(0.6f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Dragonfolly"] = new DragonfollySky();

            Filters.Scene["InfernumMode:Deus"] = new Filter(new DeusScreenShaderData("FilterMiniTower").UseColor(Color.Lerp(Color.Purple, Color.Black, 0.75f)).UseOpacity(0.24f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:Deus"] = new DeusSky();

            Filters.Scene["InfernumMode:OldDuke"] = new Filter(new OldDukeScreenShaderData("FilterMiniTower").UseColor(Color.Lerp(Color.Lime, Color.Black, 0.9f)).UseOpacity(0.6f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:OldDuke"] = new OldDukeSky();

            SkyManager.Instance["InfernumMode:DoG"] = new DoGSkyInfernum();

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
                CryogenBehaviorOverride.SetupCustomBossIcon();

                Ref<Effect> portalShader = new Ref<Effect>(GetEffect("Effects/DoGPortalShader"));
                Filters.Scene["Infernum:DoGPortal"] = new Filter(new ScreenShaderData(portalShader, "ScreenPass"), EffectPriority.High);
                Filters.Scene["Infernum:DoGPortal"].Load();

                Ref<Effect> aewPsychicEnergyShader = new Ref<Effect>(GetEffect("Effects/AEWPsychicDistortionShader"));
                GameShaders.Misc["Infernum:AEWPsychicEnergy"] = new MiscShaderData(aewPsychicEnergyShader, "DistortionPass");

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

                Ref<Effect> aresLightningArcShader = new Ref<Effect>(GetEffect("Effects/AresLightningArcShader"));
                GameShaders.Misc["Infernum:AresLightningArc"] = new MiscShaderData(aresLightningArcShader, "TrailPass");

                Ref<Effect> ghostlyShader = new Ref<Effect>(GetEffect("Effects/EidolicWailRingShader"));
                GameShaders.Misc["Infernum:PolterghastEctoplasm"] = new MiscShaderData(ghostlyShader, "BurstPass");

                ghostlyShader = new Ref<Effect>(GetEffect("Effects/NecroplasmicRoarShader"));
                GameShaders.Misc["Infernum:NecroplasmicRoar"] = new MiscShaderData(ghostlyShader, "BurstPass");

                Ref<Effect> backgroundShader = new Ref<Effect>(GetEffect("Effects/MoonLordBGDistortionShader"));
                GameShaders.Misc["Infernum:MoonLordBGDistortion"] = new MiscShaderData(backgroundShader, "DistortionPass");

                Ref<Effect> introShader = new Ref<Effect>(GetEffect("Effects/MechIntroLetterShader"));
                GameShaders.Misc["Infernum:MechsIntro"] = new MiscShaderData(introShader, "LetterPass");

                introShader = new Ref<Effect>(GetEffect("Effects/SCalIntroLetterShader"));
                GameShaders.Misc["Infernum:SCalIntro"] = new MiscShaderData(introShader, "LetterPass");

                Ref<Effect> rayShader = new Ref<Effect>(GetEffect("Effects/PrismaticRayShader"));
                GameShaders.Misc["Infernum:PrismaticRay"] = new MiscShaderData(rayShader, "TrailPass");

                Ref<Effect> hologramShader = new Ref<Effect>(GetEffect("Effects/HologramShader"));
                GameShaders.Misc["Infernum:Hologram"] = new MiscShaderData(hologramShader, "HologramPass");

                Ref<Effect> matrixShader = new Ref<Effect>(GetEffect("Effects/LocalLinearTransformationShader"));
                GameShaders.Misc["Infernum:LinearTransformation"] = new MiscShaderData(matrixShader, "TransformationPass");

                OverrideMusicBox(ItemID.MusicBoxBoss3, GetSoundSlot(SoundType.Music, "Sounds/Music/Boss3"), TileID.MusicBoxes, 36 * 12);
                OverrideMusicBox(ItemID.MusicBoxLunarBoss, GetSoundSlot(SoundType.Music, "Sounds/Music/MoonLord"), TileID.MusicBoxes, 36 * 32);
            }

            if (BossRushApplies)
                BossRushChanges.Load();

            if (Main.netMode != NetmodeID.Server)
                GeneralParticleHandler.LoadModParticleInstances(this);
        }

        public override void PostUpdateEverything()
        {
            // Disable natural GSS spawns.
            if (CanUseCustomAIs)
                sharkKillCount = 0;
        }

        internal static IDictionary<int, int> SoundLoaderMusicToItem => (IDictionary<int, int>)typeof(SoundLoader).GetField("musicToItem", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        internal static IDictionary<int, int> SoundLoaderItemToMusic => (IDictionary<int, int>)typeof(SoundLoader).GetField("itemToMusic", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        internal static IDictionary<int, IDictionary<int, int>> SoundLoaderTileToMusic => (IDictionary<int, IDictionary<int, int>>)typeof(SoundLoader).GetField("tileToMusic", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        public override void UpdateMusic(ref int music, ref MusicPriority priority)
        {
            if (NPC.AnyNPCs(NPCID.EyeofCthulhu))
            {
                music = Instance.GetSoundSlot(SoundType.Music, "Sounds/Music/EyeOfCthulhu");
                priority = MusicPriority.BossLow;
            }

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

            if (NPC.AnyNPCs(NPCID.DukeFishron))
            {
                music = Instance.GetSoundSlot(SoundType.Music, "Sounds/Music/DukeFishron");
                priority = MusicPriority.BossMedium;
            }

            if (NPC.AnyNPCs(NPCID.CultistBoss))
            {
                music = Instance.GetSoundSlot(SoundType.Music, "Sounds/Music/LunaticCultist");
                priority = MusicPriority.BossMedium;
            }

            int moonLordIndex = NPC.FindFirstNPC(NPCID.MoonLordCore);
            if (moonLordIndex != -1)
            {
                NPC moonLord = Main.npc[moonLordIndex];

                music = Instance.GetSoundSlot(SoundType.Music, "Sounds/Music/MoonLord");
                if (moonLord.Infernum().ExtraAI[10] < MoonLordCoreBehaviorOverride.IntroSoundLength)
                    music = 0;
                Main.musicFade[Main.curMusic] = 1f;
                priority = MusicPriority.BossHigh;
            }

            if (DoGPhase2HeadBehaviorOverride.InPhase2)
            {
                music = (CalamityMod as CalamityMod.CalamityMod).GetMusicFromMusicMod("DevourerOfGodsP2") ?? MusicID.LunarBoss;
                priority = MusicPriority.BiomeHigh;
            }

            bool areExoMechsAround = NPC.AnyNPCs(ModContent.NPCType<AresBody>()) ||
                NPC.AnyNPCs(ModContent.NPCType<ThanatosHead>()) ||
                NPC.AnyNPCs(ModContent.NPCType<Apollo>()) ||
                NPC.AnyNPCs(ModContent.NPCType<AthenaNPC>());

            if (areExoMechsAround)
            {
                int draedon = NPC.FindFirstNPC(ModContent.NPCType<Draedon>());
                if (draedon >= 0 && Main.npc[draedon].Infernum().ExtraAI[0] < DraedonBehaviorOverride.IntroSoundLength)
                    music = 0;
                else
                    music = Instance.GetSoundSlot(SoundType.Music, "Sounds/Music/ExoMechBosses");
                priority = MusicPriority.BossHigh;
            }

            if (DraedonThemeTimer > 0f)
            {
                DraedonThemeTimer++;
                if (DraedonThemeTimer >= DraedonBehaviorOverride.PostBattleMusicLength)
                    DraedonThemeTimer = 0f;
                else
                    music = Instance.GetSoundSlot(SoundType.Music, "Sounds/Music/Draedon");
                priority = MusicPriority.BossHigh;
            }
        }

        public static void OverrideMusicBox(int itemType, int musicSlot, int tileType, int tileFrameY)
        {
            SoundLoaderMusicToItem[musicSlot] = itemType;
            SoundLoaderItemToMusic[itemType] = musicSlot;
            if (!SoundLoaderTileToMusic.ContainsKey(tileType))
                SoundLoaderTileToMusic[tileType] = new Dictionary<int, int>();

            SoundLoaderTileToMusic[tileType][tileFrameY] = musicSlot;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI) => NetcodeHandler.ReceivePacket(this, reader, whoAmI);

        public override void AddRecipes() => RecipeUpdates.Update();

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
            if (mouseIndex != -1)
            {
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer("Boss Introduction Screens", () =>
                {
                    IntroScreenManager.Draw();
                    return true;
                }, InterfaceScaleType.None));
            }
        }

        public override void PreUpdateEntities()
        {
            BlackFade = MathHelper.Clamp(BlackFade - 0.025f, 0f, 1f);
            NetcodeHandler.Update();
            TwinsAttackSynchronizer.DoUniversalUpdate();
            TwinsAttackSynchronizer.PostUpdateEffects();

            bool arenaShouldApply = Utilities.AnyProjectiles(ModContent.ProjectileType<ProvidenceSummonerProjectile>()) || NPC.AnyNPCs(ModContent.NPCType<Providence>());
            ProvidenceArenaTimer = MathHelper.Clamp(ProvidenceArenaTimer + arenaShouldApply.ToDirectionInt(), 0f, 120f);
            if (Main.netMode != NetmodeID.MultiplayerClient && ProvidenceArenaTimer > 0 && !Utilities.AnyProjectiles(ModContent.ProjectileType<ProvidenceArenaBorder>()))
                Utilities.NewProjectileBetter(Vector2.One * 9999f, Vector2.Zero, ModContent.ProjectileType<ProvidenceArenaBorder>(), 0, 0f);
        }

        public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
        {
            if (msgType == MessageID.SyncNPC)
            {
                NPC npc = Main.npc[number];
                if (!npc.active)
                    return base.HijackSendData(whoAmI, msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);

                ModPacket packet = InfernumMode.Instance.GetPacket();
                packet.Write((short)InfernumPacketType.SendExtraNPCData);
                packet.Write(npc.whoAmI);
                packet.Write(npc.realLife);
                packet.Write(npc.Infernum().TotalAISlotsInUse);
                packet.Write(npc.Infernum().arenaRectangle.X);
                packet.Write(npc.Infernum().arenaRectangle.Y);
                packet.Write(npc.Infernum().arenaRectangle.Width);
                packet.Write(npc.Infernum().arenaRectangle.Height);
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

        public override object Call(params object[] args)
        {
            return InfernumModCalls.Call(args);
        }

        public override void Unload()
        {
            IntroScreenManager.Unload();
            OverridingListManager.Unload();
            BalancingChangesManager.Unload();
            HookManager.Unload();
            Instance = null;
            CalamityMod = null;
        }
    }
}