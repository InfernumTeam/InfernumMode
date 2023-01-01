using CalamityMod.World;
using InfernumMode.Subworlds;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Systems
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

            // Disable Infernum interactions with Eternity Mode due to horrific AI conflicts and FTW/Master because they're just not good and are undeserving of the
            // work it'd take to make Infernum a meaningful experience alongside them.
            // TODO -- Maybe just make a popup in chat warning the player that they won't actually influence anything so that people won't complain? Would need to talk with the team about that.
            bool stupidDifficultyIsActive = Main.masterMode || Main.getGoodWorld || InfernumMode.EmodeIsActive;
            if (WorldSaveSystem.InfernumMode && stupidDifficultyIsActive && DisableDifficultyModes)
            {
                Utilities.DisplayText("Infernum is not allowed in Master Mode, For the Worthy, or Eternity Mode.", Color.Red);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetcodeHandler.SyncInfernumActivity(Main.myPlayer);
                WorldSaveSystem.InfernumMode = false;
            }
            
            // Ensure that Infernum is always active in the Lost Colosseum.
            // This is necessary because difficulty states do not automatically translate over to subworlds.
            if (!stupidDifficultyIsActive && SubworldSystem.IsActive<LostColosseum>())
                WorldSaveSystem.InfernumMode = true;
        }
    }
}