using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using static CalamityMod.ILEditing.ILChanges;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class ProjectileSpawnManagementSystem : ModSystem
    {
        private static Action<Projectile> preSyncAction = null;

        public static void PrepareProjectileForSpawning(Action<Projectile> a) => preSyncAction = a;

        public override void OnModLoad()
        {
            IL.Terraria.Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float += PreSyncProjectileStuff;
        }

        public override void Unload()
        {
            IL.Terraria.Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float -= PreSyncProjectileStuff;
        }

        private void PreSyncProjectileStuff(ILContext il)
        {
            ILCursor cursor = new(il);

            // Go after the projectile instantiation phase and find the local index of the spawned projectile.
            int projectileILIndex = 0;
            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStfld<Projectile>("stepSpeed")))
            {
                LogFailure("Projectile Initialization Manager", "Could not find the step speed check variable.");
                return;
            }

            int placeToSetAction = cursor.Index;
            if (!cursor.TryGotoPrev(i => i.MatchLdloc(out projectileILIndex)))
            {
                LogFailure("Projectile Initialization Manager", "Could not find the spawned projectile's local IL index.");
                return;
            }

            cursor.Goto(placeToSetAction);
            cursor.Emit(OpCodes.Ldloc, projectileILIndex);
            cursor.EmitDelegate<Action<Projectile>>(projectile =>
            {
                // Invoke the pre-sync action and then destroy it, to ensure that the action doesn't bleed into successive, unrelated spawn calls.
                preSyncAction?.Invoke(projectile);
                preSyncAction = null;
            });
        }
    }
}