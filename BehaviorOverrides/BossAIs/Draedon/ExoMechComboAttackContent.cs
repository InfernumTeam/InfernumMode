using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public static partial class ExoMechComboAttackContent
    {
        public enum ExoMechComboAttackType
        {
            AresTwins_PressureLaser = 100,
            AresTwins_DualLaserCharges,
            AresTwins_CircleAttack,
            AresTwins_ElectromagneticPlasmaStar,
            ThanatosAres_ExplosionCircle,
            ThanatosAres_NuclearHell,
            ThanatosAres_LaserBarrage,
            ThanatosAres_ElectropulseBursts
        }

        public static bool ShouldSelectComboAttack(NPC npc, out ExoMechComboAttackType newAttack)
        {
            // Use a fallback for the attack.
            newAttack = (ExoMechComboAttackType)(int)npc.ai[0];

            // If the initial mech is not present, stop attack selections.
            NPC initialMech = FindInitialMech();
            if (initialMech is null || initialMech.Opacity == 0f)
                return false;

            int complementMechIndex = (int)initialMech.Infernum().ExtraAI[ComplementMechIndexIndex];
            NPC complementMech = complementMechIndex >= 0 && Main.npc[complementMechIndex].active ? Main.npc[complementMechIndex] : null;

            // If the complement mech isn't present, stop attack seletions.
            if (complementMech is null || complementMech.Opacity == 0f)
                return false;

            bool aresAndTwins = initialMech.type == ModContent.NPCType<Apollo>() && complementMech.type == ModContent.NPCType<AresBody>();
            bool thanatosAndAres = (initialMech.type == ModContent.NPCType<ThanatosHead>() && complementMech.type == ModContent.NPCType<AresBody>()) ||
                (initialMech.type == ModContent.NPCType<AresBody>() && complementMech.type == ModContent.NPCType<ThanatosHead>());

            if (aresAndTwins)
            {
                WeightedRandom<ExoMechComboAttackType> attackSelector = new WeightedRandom<ExoMechComboAttackType>(Main.rand);
                attackSelector.Add(ExoMechComboAttackType.AresTwins_PressureLaser);
                attackSelector.Add(ExoMechComboAttackType.AresTwins_DualLaserCharges);
                attackSelector.Add(ExoMechComboAttackType.AresTwins_CircleAttack);
                if (CurrentTwinsPhase != 4)
                    attackSelector.Add(ExoMechComboAttackType.AresTwins_ElectromagneticPlasmaStar, 1.5);

                do
                    newAttack = attackSelector.Get();
                while ((int)newAttack == initialMech.ai[0]);

                // Inform all mechs of the change.
                int apolloID = ModContent.NPCType<Apollo>();
                int thanatosID = ModContent.NPCType<ThanatosHead>();
                int aresID = ModContent.NPCType<AresBody>();

                // Find the initial mech. If it cannot be found, return nothing.
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type != apolloID && Main.npc[i].type != thanatosID && Main.npc[i].type != aresID)
                        continue;
                    if (!Main.npc[i].active)
                        continue;

                    Main.npc[i].ai[0] = (int)newAttack;
                    Main.npc[i].ai[1] = 0f;
                    for (int j = 0; j < 5; j++)
                        Main.npc[i].Infernum().ExtraAI[j] = 0f;
                    Main.npc[i].netUpdate = true;
                }
                return true;
            }
            if (thanatosAndAres)
            {
                WeightedRandom<ExoMechComboAttackType> attackSelector = new WeightedRandom<ExoMechComboAttackType>(Main.rand);
                attackSelector.Add(ExoMechComboAttackType.ThanatosAres_ExplosionCircle);
                attackSelector.Add(ExoMechComboAttackType.ThanatosAres_NuclearHell);
                attackSelector.Add(ExoMechComboAttackType.ThanatosAres_LaserBarrage);
                if (CurrentThanatosPhase != 4 && CurrentAresPhase != 4)
                    attackSelector.Add(ExoMechComboAttackType.ThanatosAres_ElectropulseBursts, 1.65);

                do
                    newAttack = attackSelector.Get();
                while ((int)newAttack == initialMech.ai[0]);

                // Inform all mechs of the change.
                int apolloID = ModContent.NPCType<Apollo>();
                int thanatosID = ModContent.NPCType<ThanatosHead>();
                int aresID = ModContent.NPCType<AresBody>();

                // Find the initial mech. If it cannot be found, return nothing.
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type != apolloID && Main.npc[i].type != thanatosID && Main.npc[i].type != aresID)
                        continue;
                    if (!Main.npc[i].active)
                        continue;

                    Main.npc[i].ai[0] = (int)newAttack;
                    Main.npc[i].ai[1] = 0f;
                    for (int j = 0; j < 5; j++)
                        Main.npc[i].Infernum().ExtraAI[j] = 0f;
                    Main.npc[i].netUpdate = true;
                }
                return true;
            }

            return false;
        }
    }
}
