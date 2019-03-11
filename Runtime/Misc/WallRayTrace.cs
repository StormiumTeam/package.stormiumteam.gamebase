using UnityEngine;

namespace StormiumTeam.GameBase
{
    public class RayUtility
    {
        public static Vector3 GetBounce(Vector3 vec)
        {
            return default;
        }
        
        public static Vector3 SlideVelocityNoYChange(Vector3 velocity, Vector3 onNormal)
        {
            var oldY = velocity.y;

            velocity.y = 0;
            onNormal.y = 0;

            var desiredMotion = SlideVelocity(velocity, onNormal);
            desiredMotion.y = oldY;

            return desiredMotion;
        }
        
        public static Vector3 SlideVelocity(Vector3 velocity, Vector3 onNormal)
        {            
            var undesiredMotion = onNormal * Vector3.Dot(velocity, onNormal);
            var desiredMotion   = velocity - undesiredMotion;

            return desiredMotion;
        }
    }
}