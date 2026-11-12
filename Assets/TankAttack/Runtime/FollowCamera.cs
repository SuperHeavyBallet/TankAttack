using UnityEngine;
namespace TankAttack
{
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 32, -23);
        public float smoothness = 8;
        void Start() { if (target != null) { transform.position = target.position + offset; transform.LookAt(target.position); } }
        void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.Lerp(transform.position, target.position + offset, 1 - Mathf.Exp(-smoothness * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
        }
    }
}
