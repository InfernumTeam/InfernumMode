using Microsoft.Xna.Framework;

namespace InfernumMode.Common.InverseKinematics
{
    public interface IInverseKinematicsUpdateRule
    {
        void Update(LimbCollection limbs, Vector2 destination);
    }
}
