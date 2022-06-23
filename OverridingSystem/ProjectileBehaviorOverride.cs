using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Terraria;

namespace InfernumMode.OverridingSystem
{
    public abstract class ProjectileBehaviorOverride
    {
        internal static void LoadAll()
        {
            void getMethodBasedOnContext(Type type, ProjectileBehaviorOverride instance, ProjectileOverrideContext context)
            {
                string methodName = string.Empty;

                switch (context)
                {
                    case ProjectileOverrideContext.ProjectileAI:
                        methodName = "PreAI";
                        break;
                    case ProjectileOverrideContext.ProjectilePreDraw:
                        methodName = "PreDraw";
                        break;
                    default:
                        throw new ArgumentException("The given override context is invalid.");
                }

                MethodInfo method = type.GetMethod(methodName, Utilities.UniversalBindingFlags);
                List<Type> paramTypes = method.GetParameters().Select(parameter => parameter.ParameterType).ToList();
                paramTypes.Add(method.ReturnType);

                Type delegateType = Expression.GetDelegateType(paramTypes.ToArray());
                Delegate methodAsDelegate = Delegate.CreateDelegate(delegateType, instance, method);

                // Cache the delegate in question with intent for it to override the base AI.
                switch (context)
                {
                    case ProjectileOverrideContext.ProjectileAI:
                        OverridingListManager.InfernumProjectilePreAIOverrideList[instance.ProjectileOverrideType] = methodAsDelegate;
                        break;
                    case ProjectileOverrideContext.ProjectilePreDraw:
                        OverridingListManager.InfernumProjectilePreDrawOverrideList[instance.ProjectileOverrideType] = methodAsDelegate;
                        break;
                }
            }

            foreach (Type type in Utilities.GetEveryMethodDerivedFrom(typeof(ProjectileBehaviorOverride), typeof(InfernumMode).Assembly))
            {
                ProjectileBehaviorOverride instance = (ProjectileBehaviorOverride)Activator.CreateInstance(type);
                if (instance.ContentToOverride.HasFlag(ProjectileOverrideContext.ProjectileAI))
                    getMethodBasedOnContext(type, instance, ProjectileOverrideContext.ProjectileAI);
                if (instance.ContentToOverride.HasFlag(ProjectileOverrideContext.ProjectilePreDraw))
                    getMethodBasedOnContext(type, instance, ProjectileOverrideContext.ProjectilePreDraw);
            }
        }

        public abstract int ProjectileOverrideType { get; }
        public abstract ProjectileOverrideContext ContentToOverride { get; }
        public virtual bool PreAI(Projectile projectile) => true;
        public virtual bool PreDraw(Projectile projectile, SpriteBatch spriteBatch, Color lightColor) => true;
    }
}
