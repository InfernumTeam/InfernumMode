using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace InfernumMode.Core.ModCalls
{
    /// <summary>
    /// The base for the two type of modcall.
    /// </summary>
    public abstract class ModCall : ModType
    {
        ///<summary>
        /// Once a call makes it to a public version, NEVER delete it from here.
        /// </summary>
        public abstract IEnumerable<string> CallCommands
        {
            get;
        }

        /// <summary>
        /// The ordered types that the args must be. Set as null if none are needed.
        /// </summary>
        public abstract IEnumerable<Type> InputTypes
        {
            get;
        }

        protected sealed override void Register()
        {
            ModTypeLookup<ModCall>.Register(this);
            
            if (!InfernumModCalls.ModCalls.Contains(this))
                InfernumModCalls.ModCalls.Add(this);
        }

        public sealed override void SetupContent() => SetStaticDefaults();

        /// <summary>
        /// Processes the modcall, checking that all the parameters match and throws if not.
        /// </summary>
        /// <param name="argsWithoutCommand"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal object ProcessInternal(params object[] argsWithoutCommand)
        {
            // If null, no input types are required so skip to processing.
            if (InputTypes == null)
                return Process(argsWithoutCommand);

            IEnumerable<Type> expectedInputTypes = InputTypes;
            int expectedInputCount = expectedInputTypes.Count();

            // Verify that there are a correct amount of arguments.
            if (argsWithoutCommand.Length != expectedInputCount)
                throw new ArgumentException($"The inputted arguments for the '{Name}' mod call were of an invalid length! {argsWithoutCommand.Length} arguments were inputted, {expectedInputCount} were expected.");

            // Verify that the arguments are of the correct type.
            for (int i = 0; i < argsWithoutCommand.Length; i++)
            {
                // i + 1 is used because the 0th argument (aka the mod call command) isn't included in this method.
                Type expectedType = expectedInputTypes.ElementAt(i);
                if (argsWithoutCommand[i].GetType() != expectedType)
                    throw new ArgumentException($"Argument {i + 1} was invalid for the '{Name}' mod call! It was of type '{argsWithoutCommand[i].GetType()}', but '{expectedType}' was expected.");
            }

            return Process(argsWithoutCommand);
        }

        // Feel free to assume that the argument types are valid when setting up mod calls.
        // Any cases where they wouldn't be should be neatly handled via ProcessInternal's error handling.
        protected abstract object Process(params object[] argsWithoutCommand);
    }
}
