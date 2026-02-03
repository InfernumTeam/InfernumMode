using CalamityMod.World;
using InfernumMode.Content.Subworlds;
using InfernumMode.Core.Netcode;
using InfernumMode.Core.Netcode.Packets;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class DifficultyManagementSystem : ModSystem
    {
#pragma warning disable IDE0051 // Remove unused private members
        private const string Message = "Hello, fellow developer. The fact that you're in this file likely means that you want to enable the disallowed difficulty modes in Infernum." +
            "While I personally think it isn't worth it, I won't be spiteful about it or work against people who know what they're doing. The boolean property can simply be set to false and" +
            "the effects will be disabled, no annoying label IL needed. -Lucille";
#pragma warning restore IDE0051 // Remove unused private members

        public static bool DisableDifficultyModes
        {
            get;
            set;
        } = true;

        public override void PreUpdateWorld()
        {
            // Ensure that Revengeance Mode is always active while Infernum is.
            if (WorldSaveSystem.InfernumModeEnabled && !CalamityWorld.revenge)
                CalamityWorld.revenge = true;

            // Disable Infernum interactions with FTW/Master/GFB because they're just not good and are undeserving of the work it'd take to make Infernum a meaningful experience alongside them.
            bool stupidDifficultyIsActive = Main.masterMode || Main.getGoodWorld || Main.zenithWorld;
            if (WorldSaveSystem.InfernumModeEnabled && stupidDifficultyIsActive && DisableDifficultyModes)
            {
                LumUtils.BroadcastLocalizedText("Mods.InfernumMode.Status.InfernumDisallowedInWeirdDifficulties", Color.Red);
                if (Main.netMode == NetmodeID.Server)
                    PacketManager.SendPacket<InfernumModeActivityPacket>();
                WorldSaveSystem.InfernumModeEnabled = false;
            }

            // Ensure that Infernum is always active in the Lost Colosseum.
            // This is necessary because difficulty states do not automatically translate over to subworlds.
            if ((!stupidDifficultyIsActive || !DisableDifficultyModes) && SubworldSystem.IsActive<LostColosseum>())
                WorldSaveSystem.InfernumModeEnabled = true;

            // Create some warning text about Eternity Mode if the player enables Infernum with it enabled.
            if (Main.netMode != NetmodeID.MultiplayerClient && WorldSaveSystem.InfernumModeEnabled && InfernumMode.EmodeIsActive && !WorldSaveSystem.DisplayedEmodeWarningText)
            {
                LumUtils.BroadcastLocalizedText("Mods.InfernumMode.Status.EternityModeWarning", Color.Red);
                WorldSaveSystem.DisplayedEmodeWarningText = true;
            }
        }
    }
}
