using System.Collections.Generic;

namespace InfernumMode.Core.CrossCompatibility
{
    public interface IBossChecklistHandler
    {
        public string BossTitle
        {
            get;
        }

        public float ProgressionValue
        {
            get;
        }

        // Should not include things such as weapons.
        public List<int> CollectibleItems
        {
            get;
        }

        public int? SpawnItem
        {
            get;
        }

        public string SpawnRequirement
        {
            get;
        }

        public string DespawnMessage
        {
            get;
        }

        public bool AvailabilityCondition
        {
            get;
        }

        public bool DefeatCondition
        {
            get;
        }

        // Should include things such as boss servants.
        public List<int> ExtraNPCIDs
        {
            get;
        }

        public string HeadIconPath
        {
            get;
        }
    }
}
