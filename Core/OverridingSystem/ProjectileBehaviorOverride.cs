using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.OverridingSystem
{
    public abstract class ProjectileBehaviorOverride
    {
        internal static ProjectileBehaviorOverride[] BehaviorOverrideSet = [];

        internal static void LoadAll()
        {
            BehaviorOverrideSet = new SetFactory(ContentSamples.ProjectilesByType.Count).CreateCustomSet<ProjectileBehaviorOverride>(null);

            foreach (Type type in Utilities.GetEveryTypeDerivedFrom(typeof(ProjectileBehaviorOverride), typeof(InfernumMode).Assembly))
            {
                ProjectileBehaviorOverride instance = (ProjectileBehaviorOverride)Activator.CreateInstance(type);

                //bool hasPreAI = false;
                //var preAIMethod = type.GetMethod("PreAI", Utilities.UniversalBindingFlags);
                //if (preAIMethod is not null && preAIMethod.DeclaringType != typeof(ProjectileBehaviorOverride))
                //    hasPreAI = true;

                //bool hasFindFrame = false;
                //var findFrameMethod = type.GetMethod("FindFrame", Utilities.UniversalBindingFlags);
                //if (findFrameMethod is not null && findFrameMethod.DeclaringType != typeof(ProjectileBehaviorOverride))
                //    hasFindFrame = true;

                BehaviorOverrideSet[instance.ProjectileOverrideType] = instance;
            }
        }

        public static bool Registered(int npcID) => BehaviorOverrideSet[npcID] != null;

        public static bool Registered<T>() where T : ModProjectile => Registered(ModContent.ProjectileType<T>());

        public abstract int ProjectileOverrideType { get; }

        public virtual bool PreAI(Projectile projectile) => true;

        public virtual bool PreDraw(Projectile projectile, SpriteBatch spriteBatch, Color lightColor) => true;
    }
}
