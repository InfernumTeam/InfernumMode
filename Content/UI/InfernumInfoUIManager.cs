using System.Collections.Generic;
using InfernumMode.Core.GlobalInstances.Systems;
using Luminance.Core.MenuInfoUI;

namespace InfernumMode.Content.UI
{
    public class InfernumInfoUIManager : InfoUIManager
    {
        public override IEnumerable<WorldInfoIcon> GetWorldInfoIcons()
        {
            yield return new WorldInfoIcon("InfernumMode/icon_small", "Mods.InfernumMode.UI.InfernumIconText", data =>
            {
                if (!data.TryGetHeaderData<WorldSaveSystem>(out var tag))
                    return false;

                return tag.ContainsKey("InfernumModeActive");
            }, 75);
        }
    }
}
