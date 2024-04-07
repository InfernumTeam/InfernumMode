namespace InfernumMode.Core.Balancing
{
    public readonly struct NPCBalancingChange(int npcType, params IBalancingRule[] balancingRules)
    {
        public readonly int NPCType = npcType;
        public readonly IBalancingRule[] BalancingRules = balancingRules;
    }
}
