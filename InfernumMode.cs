using CalamityMod.Events;
using InfernumMode.ILEditingStuff;
using InfernumMode.Items;
using InfernumMode.OverridingSystem;
using InfernumMode.Skies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
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
        internal static bool CanUseCustomAIs => !BossRushEvent.BossRushActive && PoDWorld.InfernumMode;

        internal static readonly Color HiveMindSkyColor = new Color(53, 42, 81);

        public override void Load()
        {
            OverridingListManager.Load();
            ILEditingChanges.ILEditingLoad();

            Instance = this;
            CalamityMod = ModLoader.GetMod("CalamityMod");

            Filters.Scene["InfernumMode:HiveMind"] = new Filter(new HiveMindScreenShaderData("FilterMiniTower").UseColor(HiveMindSkyColor).UseOpacity(0.6f), EffectPriority.VeryHigh);
            SkyManager.Instance["InfernumMode:HiveMind"] = new HiveMindSky();

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
                //CryogenBehaviorOverride.SetupCustomBossIcon();

                Ref<Effect> distortionShader = new Ref<Effect>(GetEffect("Effects/DistortionEffect"));
                Filters.Scene["Infernum:DistortionShader"] = new Filter(new ScreenShaderData(distortionShader, "DistortionPass"), EffectPriority.High);
                Filters.Scene["Infernum:DistortionShader"].Load();

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

                Ref<Effect> ghostlyShader = new Ref<Effect>(GetEffect("Effects/EidolicWailRingShader"));
                GameShaders.Misc["Infernum:PolterghastEctoplasm"] = new MiscShaderData(ghostlyShader, "BurstPass");

                ghostlyShader = new Ref<Effect>(GetEffect("Effects/NecroplasmicRoarShader"));
                GameShaders.Misc["Infernum:NecroplasmicRoar"] = new MiscShaderData(ghostlyShader, "BurstPass");
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
    }
}