using System;
using System.Collections.Generic;
using System.Linq;
using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public abstract class BaseRelicItem : ModItem
    {
        public override LocalizedText Tooltip => Utilities.GetLocalization($"Items.{this.Name}.Tooltip").WithFormatArgs(PersonalMessage);
        public virtual string PersonalMessage => Utilities.GetLocalization("Items.InfernalRelicText").Value;

        public virtual Color? PersonalMessageColor
        {
            get
            {
                return null;
            }
        }

        public abstract int TileID { get; }

        public virtual int MaxLines => 1;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            for (int i = 0; i < MaxLines; i++)
            {
                TooltipLine? obj = tooltips.FirstOrDefault((x) => x.Name == $"Tooltip{i}" && x.Mod == "Terraria");
                if (PersonalMessageColor is null)
                {
                    float colorInterpolant = (float)(Math.Sin(Pi * Main.GlobalTimeWrappedHourly + 1f) * 0.5) + 0.5f;
                    Color c = LumUtils.MulticolorLerp(colorInterpolant, new Color(170, 0, 0, 255), Color.OrangeRed, new Color(255, 200, 0, 255));
                    obj?.Text = LumUtils.ColorMessage(obj?.Text, c);
                }
                else
                {
                    obj?.OverrideColor = PersonalMessageColor;
                }
            }
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileID, 0);
            Item.width = 30;
            Item.height = 44;
            Item.maxStack = 999;
            Item.rare = ModContent.RarityType<InfernumRedRarity>();
            Item.value = Item.buyPrice(0, 5, 0, 0);

            Item.Infernum_Tooltips().InfernumItem = true;
        }
    }
}
