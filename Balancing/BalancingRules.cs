using System.Linq;
using Terraria;

namespace InfernumMode.Balancing
{
    public class PierceResistBalancingRule : IBalancingRule
    {
        public float DamageReductionFactor;
        public PierceResistBalancingRule(float damageReductionFactor) => DamageReductionFactor = damageReductionFactor;

        public bool AppliesTo(NPC npc, NPCHitContext hitContext) => hitContext.Pierce > 1 || hitContext.Pierce == -1;

        public void ApplyBalancingChange(NPC npc, ref int damage) => damage = (int)(damage * DamageReductionFactor);
    }

    public class ProjectileResistBalancingRule : IBalancingRule
    {
        public float DamageReductionFactor;
        public int[] ApplicableProjectileTypes;
        public ProjectileResistBalancingRule(float damageReductionFactor, params int[] projTypes)
        {
            DamageReductionFactor = damageReductionFactor;
            ApplicableProjectileTypes = projTypes;
        }

        public bool AppliesTo(NPC npc, NPCHitContext hitContext)
        {
            if (hitContext.DamageSource != DamageSourceType.FriendlyProjectile)
                return false;
            if (!ApplicableProjectileTypes.Contains(hitContext.ProjectileType ?? -1))
                return false;

            return true;
        }

        public void ApplyBalancingChange(NPC npc, ref int damage) => damage = (int)(damage * DamageReductionFactor);
    }

    public class StealthStrikeBalancingRule : IBalancingRule
    {
        public float DamageReductionFactor;
        public int[] ApplicableProjectileTypes;
        public StealthStrikeBalancingRule(float damageReductionFactor, params int[] projTypes)
        {
            DamageReductionFactor = damageReductionFactor;
            ApplicableProjectileTypes = projTypes;
        }

        public bool AppliesTo(NPC npc, NPCHitContext hitContext)
        {
            if (hitContext.DamageSource != DamageSourceType.FriendlyProjectile)
                return false;
            if (!ApplicableProjectileTypes.Contains(hitContext.ProjectileType ?? -1))
                return false;

            return hitContext.IsStealthStrike;
        }

        public void ApplyBalancingChange(NPC npc, ref int damage) => damage = (int)(damage * DamageReductionFactor);
    }

    public class NPCSpecificRequirementBalancingRule : IBalancingRule
    {
        public NPCApplicationRequirement Requirement;
        public delegate bool NPCApplicationRequirement(NPC npc);
        public NPCSpecificRequirementBalancingRule(NPCApplicationRequirement npcApplicationRequirement)
        {
            Requirement = npcApplicationRequirement;
        }

        public bool AppliesTo(NPC npc, NPCHitContext hitContext) => Requirement(npc);

        // This "balancing" rule doesn't actually perform any changes. It simply serves as a means of enforcing NPC-specific requirements.
        // As such, this method is empty.
        public void ApplyBalancingChange(NPC npc, ref int damage) { }
    }
}