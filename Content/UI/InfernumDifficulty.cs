using System.Collections.Generic;
using CalamityMod.Systems;
using CalamityMod.World;
using InfernumMode.Assets.Sounds;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.Netcode;
using InfernumMode.Core.Netcode.Packets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static CalamityMod.Systems.DifficultyModeSystem;

namespace InfernumMode.Content.UI
{
    public class InfernumDifficulty : DifficultyMode
    {
        public override bool Enabled
        {
            get => WorldSaveSystem.InfernumModeEnabled;
            set
            {
                WorldSaveSystem.InfernumModeEnabled = value;
                if (value)
                {
                    CalamityWorld.revenge = true;
                    Main.GameMode = BackBoneGameModeID;
                }      
                if (Main.netMode != NetmodeID.SinglePlayer)
                    PacketManager.SendPacket<InfernumModeActivityPacket>();
            }
        }

        public override Asset<Texture2D> Texture => _texture ??= ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/UI/InfernumIcon");

        //temp
        public override Asset<Texture2D> TextureDisabled => _textureDisabled ??= ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/UI/InfernumIcon_Off");

        public override Asset<Texture2D> OutlineTexture => _outlineTexture ??= ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/UI/InfernumIcon_Outline");

        public override LocalizedText ExpandedDescription => Language.GetText("Mods.InfernumMode.DifficultyUI.ExpandedDescription");

        public override int BackBoneGameModeID => GameModeID.Expert;

        public override float DifficultyScale => 0.1f;
        public override LocalizedText Name => Language.GetText("Mods.InfernumMode.DifficultyUI.Name");
        public override LocalizedText ShortDescription => Language.GetText("Mods.InfernumMode.DifficultyUI.ShortDescription");

        public override SoundStyle ActivationSound => InfernumSoundRegistry.ModeToggleLaugh;
        public override Color ChatTextColor => Color.DarkRed;

        public override int[] FavoredDifficultyAtTier(int tier)
        {
            DifficultyMode[] difficultyArray = DifficultyTiers[tier];
            List<int> list = new List<int>();

            for (int i = 0; i < difficultyArray.Length; i++)
            {
                if (difficultyArray[i] is ExpertDifficulty || difficultyArray[i] is RevengeanceDifficulty)
                    list.Add(i);
            }

            if (list.Count <= 0)
                list.Add(0);


            return list.ToArray();
        }
    }
}
