using System.Collections.Generic;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.Content.Projectiles.Pets;
using Terraria;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ComboAttacks
{
    public static partial class ExoMechComboAttackContent
    {

        public enum ExoMechComboAttackType
        {
            AresTwins_DualLaserCharges = 100,
            AresTwins_CircleAttack,

            ThanatosAres_LaserCircle,
            ThanatosAres_EnergySlashesAndCharges,

            TwinsThanatos_ThermoplasmaDashes,
            TwinsThanatos_AlternatingTwinsBursts,
        }

        public static Dictionary<ExoMechComboAttackType, int[]> AffectedAresArms => new()
        {
            [ExoMechComboAttackType.ThanatosAres_EnergySlashesAndCharges] = [ModContent.NPCType<AresEnergyKatana>()],
        };

        public static void InformAllMechsOfComboAttackChange(int newAttack)
        {
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

            bool aresAndTwins = (initialMech.type == ModContent.NPCType<Apollo>() && complementMech.type == ModContent.NPCType<AresBody>()) ||
                (initialMech.type == ModContent.NPCType<AresBody>() && complementMech.type == ModContent.NPCType<Apollo>());
            bool thanatosAndAres = (initialMech.type == ModContent.NPCType<ThanatosHead>() && complementMech.type == ModContent.NPCType<AresBody>()) ||
                (initialMech.type == ModContent.NPCType<AresBody>() && complementMech.type == ModContent.NPCType<ThanatosHead>());
            bool thanatosAndTwins = (initialMech.type == ModContent.NPCType<ThanatosHead>() && complementMech.type == ModContent.NPCType<Apollo>()) ||
                (initialMech.type == ModContent.NPCType<Apollo>() && complementMech.type == ModContent.NPCType<ThanatosHead>());

            // Have the hat girl give comments if a combo attack is happening.
            HatGirl.SayThingWhileOwnerIsAlive(Main.player[npc.target], "Mods.InfernumMode.PetDialog.ExoMechComboTip");

            if (aresAndTwins)
            {
                initialMech.ai[0] = (int)initialMech.ai[0] switch
                {
                    (int)ExoMechComboAttackType.AresTwins_DualLaserCharges => (int)ExoMechComboAttackType.AresTwins_CircleAttack,
                    _ => (int)ExoMechComboAttackType.AresTwins_DualLaserCharges,
                };

                // Inform all mechs of the change.
                newAttack = (ExoMechComboAttackType)initialMech.ai[0];
                InformAllMechsOfComboAttackChange((int)newAttack);
                return true;
            }

            if (thanatosAndAres)
            {
                initialMech.ai[0] = (int)initialMech.ai[0] switch
                {
                    (int)ExoMechComboAttackType.ThanatosAres_LaserCircle => (int)ExoMechComboAttackType.ThanatosAres_EnergySlashesAndCharges,
                    _ => (int)ExoMechComboAttackType.ThanatosAres_LaserCircle,
                };

                // Inform all mechs of the change.
                newAttack = (ExoMechComboAttackType)initialMech.ai[0];
                InformAllMechsOfComboAttackChange((int)newAttack);
                return true;
            }

            if (thanatosAndTwins)
            {
                initialMech.ai[0] = (int)initialMech.ai[0] switch
                {
                    (int)ExoMechComboAttackType.TwinsThanatos_ThermoplasmaDashes => (int)ExoMechComboAttackType.TwinsThanatos_AlternatingTwinsBursts,
                    _ => (int)ExoMechComboAttackType.TwinsThanatos_ThermoplasmaDashes,
                };

                // Inform all mechs of the change.
                newAttack = (ExoMechComboAttackType)initialMech.ai[0];
                InformAllMechsOfComboAttackChange((int)newAttack);
                return true;
            }

            return false;
        }
    }
}
