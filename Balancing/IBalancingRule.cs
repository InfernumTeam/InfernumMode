using Terraria;

namespace InfernumMode.Balancing
{
    public interface IBalancingRule
    {
        bool AppliesTo(NPC npc, NPCHitContext hitContext);

        void ApplyBalancingChange(NPC npc, ref int damage);
    }
}