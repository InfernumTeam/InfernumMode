using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode
{
    public enum InfernumPacketType : short
	{
        SendExtraNPCData
	}
    public static class NetcodeHandler
    {
        public static void ReceivePacket(Mod mod, BinaryReader reader, int whoAmI)
		{
            InfernumPacketType packetType = (InfernumPacketType)reader.ReadInt16();
            switch (packetType)
			{
                case InfernumPacketType.SendExtraNPCData:
                    int npcIndex = reader.ReadInt32();
                    int totalUniqueAIIndicesUsed = reader.ReadInt32();
                    for (int i = 0; i < totalUniqueAIIndicesUsed; i++)
					{
                        int aiIndex = reader.ReadInt32();
                        float aiValue = reader.ReadSingle();
                        Main.npc[npcIndex].Infernum().ExtraAI[aiIndex] = aiValue;
					}
                    break;
			}
        }
    }
}