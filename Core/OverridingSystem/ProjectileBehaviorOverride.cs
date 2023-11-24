using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace InfernumMode.Core.OverridingSystem
{
    public abstract class ProjectileBehaviorOverride
    {
        internal static Dictionary<int, ProjectileBehaviorOverride> BehaviorOverrides = new();

        internal static void LoadAll()
        {
            BehaviorOverrides = new();

            static void getMethodBasedOnContext( ProjectileBehaviorOverride instance, ProjectileOverrideContext context)
            {
                string methodName = string.Empty;

                methodName = context switch
                {
                    ProjectileOverrideContext.ProjectileAI => "PreAI",
                    ProjectileOverrideContext.ProjectilePreDraw => "PreDraw",
                    _ => throw new ArgumentException("The given override context is invalid."),
                };

                // Mark this projectile as having an override for the method.
                switch (context)
                {
                    case ProjectileOverrideContext.ProjectileAI:
                        OverridingListManager.InfernumProjectilePreAIOverrideList.Add(instance.ProjectileOverrideType);
                        break;
                    case ProjectileOverrideContext.ProjectilePreDraw:
                        OverridingListManager.InfernumProjectilePreDrawOverrideList.Add(instance.ProjectileOverrideType);
                        break;
                }
            }

            foreach (Type type in Utilities.GetEveryTypeDerivedFrom(typeof(ProjectileBehaviorOverride), typeof(InfernumMode).Assembly))
            {
                ProjectileBehaviorOverride instance = (ProjectileBehaviorOverride)Activator.CreateInstance(type);
                if (instance.ContentToOverride.HasFlag(ProjectileOverrideContext.ProjectileAI))
                    getMethodBasedOnContext(instance, ProjectileOverrideContext.ProjectileAI);
                if (instance.ContentToOverride.HasFlag(ProjectileOverrideContext.ProjectilePreDraw))
                    getMethodBasedOnContext(instance, ProjectileOverrideContext.ProjectilePreDraw);

                BehaviorOverrides[instance.ProjectileOverrideType] = instance;
            }
        }

        public abstract int ProjectileOverrideType { get; }

        public abstract ProjectileOverrideContext ContentToOverride { get; }

        public virtual bool PreAI(Projectile projectile) => true;

        public virtual bool PreDraw(Projectile projectile, SpriteBatch spriteBatch, Color lightColor) => true;
    }
}
