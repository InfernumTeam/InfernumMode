using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena;
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
            AresTwins_DualLaserCharges = 100,
            AresTwins_CircleAttack,

            ThanatosAres_LaserCircle,
            ThanatosAres_ElectricCage,

            TwinsAthena_ThermoplasmaDance,
            TwinsAthena_ThermoplasmaChargeupBursts
        }

        public static void InformAllMechsOfComboAttackChange(int newAttack)
        {
            int apolloID = ModContent.NPCType<Apollo>();
            int thanatosID = ModContent.NPCType<ThanatosHead>();
            int athenaID = ModContent.NPCType<AthenaNPC>();
            int aresID = ModContent.NPCType<AresBody>();

            // Find the initial mech. If it cannot be found, return nothing.
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type != apolloID && Main.npc[i].type != thanatosID && Main.npc[i].type != athenaID && Main.npc[i].type != aresID)
                    continue;
                if (!Main.npc[i].active)
                    continue;

                Main.npc[i].ai[0] = newAttack;
                Main.npc[i].ai[1] = 0f;
                for (int j = 0; j < 5; j++)
                    Main.npc[i].Infernum().ExtraAI[j] = 0f;
                Main.npc[i].netUpdate = true;
            }
        }

        public static bool ShouldSelectComboAttack(NPC npc, out ExoMechComboAttackType newAttack)
        {
            // Use a fallback for the attack.
            newAttack = (ExoMechComboAttackType)(int)npc.ai[0];

            // If the initial mech is not present, stop attack selections.
            NPC initialMech = FindInitialMech();
            if (initialMech is null || initialMech.Opacity == 0f || npc != initialMech)
                return false;

            newAttack = (ExoMechComboAttackType)(int)initialMech.ai[0];
            int complementMechIndex = (int)initialMech.Infernum().ExtraAI[ComplementMechIndexIndex];
            NPC complementMech = complementMechIndex >= 0 && Main.npc[complementMechIndex].active ? Main.npc[complementMechIndex] : null;

            // If the complement mech isn't present, stop attack seletions.
            if (complementMech is null)
                return false;

            bool aresAndTwins = initialMech.type == ModContent.NPCType<Apollo>() && complementMech.type == ModContent.NPCType<AresBody>();
            bool athenaAndTwins = initialMech.type == ModContent.NPCType<AthenaNPC>() && complementMech.type == ModContent.NPCType<Apollo>();
            bool thanatosAndAres = (initialMech.type == ModContent.NPCType<ThanatosHead>() && complementMech.type == ModContent.NPCType<AresBody>()) ||
                (initialMech.type == ModContent.NPCType<AresBody>() && complementMech.type == ModContent.NPCType<ThanatosHead>());

            if (aresAndTwins)
            {
                WeightedRandom<ExoMechComboAttackType> attackSelector = new WeightedRandom<ExoMechComboAttackType>(Main.rand);

                attackSelector.Add(ExoMechComboAttackType.AresTwins_DualLaserCharges);
                attackSelector.Add(ExoMechComboAttackType.AresTwins_CircleAttack);

                switch ((int)initialMech.ai[0])
                {
                    case (int)ExoMechComboAttackType.AresTwins_DualLaserCharges:
                        initialMech.ai[0] = (int)ExoMechComboAttackType.AresTwins_CircleAttack;
                        break;
                    case (int)ExoMechComboAttackType.AresTwins_CircleAttack:
                    default:
                        initialMech.ai[0] = (int)ExoMechComboAttackType.AresTwins_DualLaserCharges;
                        break;
                }

                // Inform all mechs of the change.
                newAttack = (ExoMechComboAttackType)initialMech.ai[0];
                InformAllMechsOfComboAttackChange((int)newAttack);
                return true;
            }

            if (athenaAndTwins)
            {
                WeightedRandom<ExoMechComboAttackType> attackSelector = new WeightedRandom<ExoMechComboAttackType>(Main.rand);

                attackSelector.Add(ExoMechComboAttackType.TwinsAthena_ThermoplasmaDance);
                attackSelector.Add(ExoMechComboAttackType.TwinsAthena_ThermoplasmaChargeupBursts);

                switch ((int)initialMech.ai[0])
                {
                    case (int)ExoMechComboAttackType.TwinsAthena_ThermoplasmaDance:
                        initialMech.ai[0] = (int)ExoMechComboAttackType.TwinsAthena_ThermoplasmaChargeupBursts;
                        break;
                    case (int)ExoMechComboAttackType.TwinsAthena_ThermoplasmaChargeupBursts:
                    default:
                        initialMech.ai[0] = (int)ExoMechComboAttackType.TwinsAthena_ThermoplasmaDance;
                        break;
                }

                // Inform all mechs of the change.
                newAttack = (ExoMechComboAttackType)initialMech.ai[0];
                InformAllMechsOfComboAttackChange((int)newAttack);
                return true;
            }

            if (thanatosAndAres)
            {
                WeightedRandom<ExoMechComboAttackType> attackSelector = new WeightedRandom<ExoMechComboAttackType>(Main.rand);
                attackSelector.Add(ExoMechComboAttackType.ThanatosAres_LaserCircle);
                attackSelector.Add(ExoMechComboAttackType.ThanatosAres_ElectricCage);

                switch ((int)initialMech.ai[0])
                {
                    case (int)ExoMechComboAttackType.ThanatosAres_LaserCircle:
                        initialMech.ai[0] = (int)ExoMechComboAttackType.ThanatosAres_ElectricCage;
                        break;
                    case (int)ExoMechComboAttackType.ThanatosAres_ElectricCage:
                    default:
                        initialMech.ai[0] = (int)ExoMechComboAttackType.ThanatosAres_LaserCircle;
                        break;
                }

                // Inform all mechs of the change.
                newAttack = (ExoMechComboAttackType)initialMech.ai[0];
                InformAllMechsOfComboAttackChange((int)newAttack);
                return true;
            }

            return false;
        }
    }
}
