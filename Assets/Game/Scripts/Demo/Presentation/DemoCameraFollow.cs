using UnityEngine;

namespace Shanhai.Demo.Presentation
{
    /// <summary>固定2D正交朝向，在纵向线性地图中保持玩家处于可操作区域。</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class DemoCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float verticalOffset = 3f;

        public void BindTarget(Transform player) { target = player; }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 position = transform.position;
            position.y = target.position.y + verticalOffset;
            transform.position = position;
        }
    }
}
