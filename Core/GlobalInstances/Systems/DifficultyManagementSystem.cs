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
        private const string message = "Hello, fellow developer. The fact that you're in this file likely means that you want to enable the disallowed difficulty modes in Infernum." +
            "While I personally think it isn't worth it, I won't be spiteful about it or work against people who know what they're doing. The boolean property can simply be set to false and" +
            "the effects will be disabled, no annoying label IL needed. -Dominic";
#pragma warning restore IDE0051 // Remove unused private members

        public static bool DisableDifficultyModes
        {
            get;
            set;
        } = true;

        public override void PreUpdateWorld()
        {
            // Ensure that Death and Revengeance Mode are always active while Infernum is.
            if (WorldSaveSystem.InfernumMode && !CalamityWorld.revenge)
                CalamityWorld.revenge = true;
            if (WorldSaveSystem.InfernumMode && !CalamityWorld.death)
                CalamityWorld.death = true;

            // Disable Infernum interactions with FTW/Master because they're just not good and are undeserving of the work it'd take to make Infernum a meaningful experience alongside them.
            bool stupidDifficultyIsActive = Main.masterMode || Main.getGoodWorld;
            if (WorldSaveSystem.InfernumMode && stupidDifficultyIsActive && DisableDifficultyModes)
            {
                Utilities.DisplayText("Infernum is not allowed in Master Mode or For the Worthy.", Color.Red);
                if (Main.netMode == NetmodeID.Server)
                    PacketManager.SendPacket<InfernumModeActivityPacket>();
                WorldSaveSystem.InfernumMode = false;
            }

            // Ensure that Infernum is always active in the Lost Colosseum.
            // This is necessary because difficulty states do not automatically translate over to subworlds.
            if (!stupidDifficultyIsActive && SubworldSystem.IsActive<LostColosseum>())
                WorldSaveSystem.InfernumMode = true;

            // Create some warning text about Eternity Mode if the player enables Infernum with it enabled.
            if (Main.netMode != NetmodeID.MultiplayerClient && WorldSaveSystem.InfernumMode && InfernumMode.EmodeIsActive && !WorldSaveSystem.DisplayedEmodeWarningText)
            {
                Utilities.DisplayText("Eternity mode's boss AI changes are overridden by Infernum if there are conflicts.", Color.Red);
                WorldSaveSystem.DisplayedEmodeWarningText = true;
            }
        }
    }
}
