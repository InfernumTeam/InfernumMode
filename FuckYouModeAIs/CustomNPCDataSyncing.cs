using System.Linq;
using Terraria;

namespace InfernumMode.FuckYouModeAIs.MainAI
{
	public partial class FuckYouModeAIsGlobal
    {
		internal bool[] HasAssociatedAIBeenUsed = new bool[TotalExtraAISlots];
		internal int TotalAISlotsInUse => HasAssociatedAIBeenUsed.Count(slot => slot);
		public override void PostAI(NPC npc)
		{
			for (int i = 0; i < ExtraAI.Length; i++)
			{
				if (ExtraAI[i] != 0f)
					HasAssociatedAIBeenUsed[i] = true;
			}
		}
	}
}