using Terraria;

namespace InfernumMode.Core.Balancing
{
    public interface IBalancingRule
    {
        bool AppliesTo(NPC npc, NPCHitContext hitContext);

        void ApplyBalancingChange(NPC npc, ref NPC.HitModifiers modifiers);
    }
}
