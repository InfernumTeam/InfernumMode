using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.CrossCompatibility
{
    public class BossChecklistManagementSystem : ModSystem
    {
        internal static Mod BossChecklist;

        public override void OnModLoad()
        {
            ModLoader.TryGetMod("BossChecklist", out BossChecklist);

            // Stop here if Boss Checklist is not enabled.
            if (BossChecklist is null)
                return;

            IList<ModNPC> modNPCs = (IList<ModNPC>)typeof(NPCLoader).GetField("npcs", Utilities.UniversalBindingFlags).GetValue(null);
            foreach (ModNPC npc in modNPCs)
            {
                if (npc.Mod.Name != Mod.Name || !typeof(IBossChecklistHandler).IsAssignableFrom(npc.GetType()))
                    continue;

                IBossChecklistHandler handler = (IBossChecklistHandler)npc;
                List<int> extraNPCs = new()
                {
                    npc.Type
                };
                if (handler.ExtraNPCIDs is not null)
                    extraNPCs.AddRange(handler.ExtraNPCIDs);

                var result = BossChecklist.Call(
                        "AddBoss",
                        Mod,
                        handler.BossTitle,
                        extraNPCs,
                        handler.ProgressionValue,
                        () => handler.DefeatCondition,
                        () => handler.AvailabilityCondition,
                        handler.CollectibleItems,
                        handler.SpawnItem ?? ItemID.None,
                        handler.SpawnRequirement,
                        () => handler.DespawnMessage,
                        null,
                        handler.HeadIconPath
                    );
            }
        }

        public override void Unload()
        {
            BossChecklist = null;
        }
    }
}
