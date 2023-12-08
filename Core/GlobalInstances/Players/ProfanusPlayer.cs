using InfernumMode.Content.Projectiles.Melee;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class ProfanusPlayer : ModPlayer
    {
        public int[] RockSlots = new int[ProfanusProjectile.MaxRocks];

        public bool PauseTimer;

        public bool CheckToResetTimer;

        public int RockTimer;

        public override void ResetEffects() => ResetRocks();

        public override void UpdateDead() => ResetRocks();

        private void ResetRocks()
        {
            if (!PauseTimer)
                RockTimer++;
            

            if (CheckToResetTimer)
            {
                CheckToResetTimer = false;
                bool result = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (!Main.projectile[i].active)
                        continue;

                    if (Main.projectile[i].ModProjectile is not ProfanusRocks rocks)
                        continue;

                    if (rocks.CurrentState is ProfanusRocks.State.Circling or ProfanusRocks.State.Firing)
                        break;

                    result = true;
                    break;
                }

                if (result)
                    RockTimer = 0;
            }

            int type = ModContent.ProjectileType<ProfanusRocks>();

            for (int i = 0; i < RockSlots.Length; i++)
            {
                ref int index = ref RockSlots[i];
                if (index >= 0)
                {
                    // If the index npc is not active, reset the index.
                    if (!Main.projectile[index].active)
                        index = -1;
                    // If the index is not the correct type.
                    else if (Main.projectile[index].type != type)
                        index = -1;

                    else if (Main.projectile[index].ModProjectile is  ProfanusRocks rocks)
                    {
                        if (rocks.CurrentState is ProfanusRocks.State.Firing)
                            index = -1;
                    }
                }
            }
        }
    }
}
