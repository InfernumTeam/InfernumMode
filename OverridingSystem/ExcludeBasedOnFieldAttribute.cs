using System;
using System.Reflection;

namespace InfernumMode.OverridingSystem
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ExcludeBasedOnFieldAttribute : Attribute
    {
        public ExcludeBasedOnFieldAttribute(int npcTypeID, EntityOverrideContext exclusionDomain, Type fieldLocation, string fieldName, object fieldObjectReference = null)
        {
            FieldInfo field = fieldLocation.GetField(fieldName, Utilities.UniversalBindingFlags);

            if (field.FieldType != typeof(bool))
                throw new ArgumentException("An exclusion field must be a boolean in order to do exclusive checks.", nameof(field));

            if ((field.IsStatic && fieldObjectReference != null) || (!field.IsStatic && fieldObjectReference is null))
                throw new ArgumentException("In order to access a field, an appropriate object reference (or lack thereof) is required.", nameof(fieldObjectReference));

			bool conditionGetter() => (bool)field.GetValue(fieldObjectReference);
			OverridingListManager.ExclusionList[new OverrideExclusionContext(npcTypeID, exclusionDomain)] = conditionGetter;
        }
        public ExcludeBasedOnFieldAttribute(string calamityEntityName, EntityOverrideContext exclusionDomain, Type fieldLocation, string fieldName, object fieldObjectReference = null) :
            this(InfernumMode.CalamityMod.NPCType(calamityEntityName), exclusionDomain, fieldLocation, fieldName, fieldObjectReference)
		{

		}
    }
}
