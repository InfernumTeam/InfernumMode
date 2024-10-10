using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core
{
    public class InfernumSets : ModSystem
    {
        public class Items
        {

        }
        public class NPCs
        {

        }
        public class Projectiles
        {
            public static bool[] Tombstone;
            public static bool[] RocketIllegalInArena;
        }

        public override void PostSetupContent()
        {
            SetFactory projFactory = ProjectileID.Sets.Factory;
            Projectiles.Tombstone = projFactory.CreateBoolSet(false,
                ProjectileID.Tombstone, 
                ProjectileID.Gravestone, 
                ProjectileID.RichGravestone1, 
                ProjectileID.RichGravestone2,
                ProjectileID.RichGravestone3, 
                ProjectileID.RichGravestone4, 
                ProjectileID.RichGravestone4, 
                ProjectileID.Headstone, 
                ProjectileID.Obelisk
                );
            Projectiles.RocketIllegalInArena = projFactory.CreateBoolSet(false,
                ProjectileID.DryRocket,
                ProjectileID.DryGrenade,
                ProjectileID.DryMine,
                ProjectileID.DrySnowmanRocket,
                ProjectileID.WetRocket,
                ProjectileID.WetGrenade,
                ProjectileID.WetMine,
                ProjectileID.WetBomb,
                ProjectileID.WetSnowmanRocket,
                ProjectileID.HoneyRocket,
                ProjectileID.HoneyGrenade,
                ProjectileID.HoneyMine,
                ProjectileID.HoneyBomb,
                ProjectileID.HoneySnowmanRocket,
                ProjectileID.LavaRocket,
                ProjectileID.LavaGrenade,
                ProjectileID.LavaMine,
                ProjectileID.LavaBomb,
                ProjectileID.LavaSnowmanRocket,
                ProjectileID.DirtBomb,
                ProjectileID.DirtStickyBomb
                );
        }
    }
}
