using System;
using System.Collections.Generic;
using InfernumMode.Content.Rarities.InfernumRarities;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.ModCalls.InfernumCalls
{
    // You really shouldn't be calling this, but go ahead I suppose :3.
    public class RegisterAsSoulHeadphonesModCall : GenericModCall
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "RegisterAsSoulHeadphones";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(Item);
            }
        }

        protected override void ProcessGeneric(params object[] argsWithoutCommand)
        {
            Item item = (Item)argsWithoutCommand[0];
            item.value = 0;
            item.rare = ModContent.RarityType<InfernumSoulDrivenHeadphonesRarity>();
            item.Infernum_Tooltips().DeveloperItem = true;
        }
    }
}
