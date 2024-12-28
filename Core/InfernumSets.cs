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
    public static class InfernumSets
    {
        public sealed class Projectiles : ModSystem
        {
            public static bool[] Tombstone
            {
                get;
                private set;
            }

            public static bool[] RocketIllegalInArena
            {
                get;
                private set;
            }

            public override void PostSetupContent()
            {
                Tombstone = ProjectileID.Sets.Factory.CreateBoolSet(false,
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

                RocketIllegalInArena = ProjectileID.Sets.Factory.CreateBoolSet(false,
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
}
