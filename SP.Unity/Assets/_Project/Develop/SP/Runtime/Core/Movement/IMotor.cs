using UnityEngine;

namespace SP.Runtime.Core.Movement
{
    public interface IMotor
    {
        public void Move(Vector3 direction);
        public void Jump();
    }
}
