namespace InfernumMode.Core.Balancing
{
    public readonly struct NPCBalancingChange
    {
        public readonly int NPCType;
        public readonly IBalancingRule[] BalancingRules;

        public NPCBalancingChange(int npcType, params IBalancingRule[] balancingRules)
        {
            NPCType = npcType;
            BalancingRules = balancingRules;
        }
    }
}
