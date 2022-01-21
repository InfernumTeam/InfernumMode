using Microsoft.Xna.Framework;

namespace InfernumMode.InverseKinematics
{
    public interface IInverseKinematicsUpdateRule
    {
        void Update(LimbCollection limbs, Vector2 destination);
    }
}
