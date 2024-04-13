using System.Linq;
using CalamityMod;
using Terraria;

namespace InfernumMode.Core.Balancing
{
    public class PierceResistBalancingRule(float damageMultiplier) : IBalancingRule
    {
        public float DamageMultiplier = damageMultiplier;

        public bool AppliesTo(NPC npc, NPCHitContext hitContext) => hitContext.Pierce is > 1 or (-1);

        public void ApplyBalancingChange(NPC npc, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= DamageMultiplier;
    }

    public class ProjectileResistBalancingRule(float damageMultiplier, params int[] projTypes) : IBalancingRule
    {
        public float DamageMultiplier = damageMultiplier;
        public int[] ApplicableProjectileTypes = projTypes;

        public bool AppliesTo(NPC npc, NPCHitContext hitContext)
        {
            if (hitContext.DamageSource != DamageSourceType.FriendlyProjectile)
                return false;
            if (!ApplicableProjectileTypes.Contains(hitContext.ProjectileType ?? -1))
                return false;

            return true;
        }

        public void ApplyBalancingChange(NPC npc, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= DamageMultiplier;
    }

    public class ClassResistBalancingRule(float damageMultiplier, ClassType classType) : IBalancingRule
    {
        public float DamageMultiplier = damageMultiplier;

        public ClassType ApplicableClass = classType;

        public bool AppliesTo(NPC npc, NPCHitContext hitContext)
        {
            return hitContext.Class == ApplicableClass;
        }

        public void ApplyBalancingChange(NPC npc, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= DamageMultiplier;
    }

    public class StealthStrikeBalancingRule(float damageMultiplier, params int[] projTypes) : IBalancingRule
    {
        public float DamageMultiplier = damageMultiplier;
        public int[] ApplicableProjectileTypes = projTypes;

        public bool AppliesTo(NPC npc, NPCHitContext hitContext)
        {
            if (hitContext.DamageSource != DamageSourceType.FriendlyProjectile)
                return false;
            if (!ApplicableProjectileTypes.Contains(hitContext.ProjectileType ?? -1))
                return false;

            return hitContext.IsStealthStrike;
        }

        public void ApplyBalancingChange(NPC npc, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= DamageMultiplier;
    }

    public class TrueMeleeBalancingRule(float damageMultiplier) : IBalancingRule
    {
        public float DamageMultiplier = damageMultiplier;

        public bool AppliesTo(NPC npc, NPCHitContext hitContext)
        {
            if (hitContext.DamageSource == DamageSourceType.FriendlyProjectile)
                return Main.projectile[hitContext.ProjectileIndex.Value].IsTrueMelee();

            return hitContext.DamageSource == DamageSourceType.TrueMeleeSwing;
        }

        public void ApplyBalancingChange(NPC npc, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= DamageMultiplier;
    }

    public class NPCSpecificRequirementBalancingRule(NPCSpecificRequirementBalancingRule.NPCApplicationRequirement npcApplicationRequirement) : IBalancingRule
    {
        public NPCApplicationRequirement Requirement = npcApplicationRequirement;
        public delegate bool NPCApplicationRequirement(NPC npc);

        public bool AppliesTo(NPC npc, NPCHitContext hitContext) => Requirement(npc);

        // This "balancing" rule doesn't actually perform any changes. It simply serves as a means of enforcing NPC-specific requirements.
        // As such, this method is empty.
        public void ApplyBalancingChange(NPC npc, ref NPC.HitModifiers modifiers) { }
    }
}
