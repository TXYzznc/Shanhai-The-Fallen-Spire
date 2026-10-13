using System;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using UnityEngine;

namespace Shanhai.Demo.UI
{
    /// <summary>动态敌人绑定头顶UI；HUD持有显示节点，实体隐藏时清理会话引用。</summary>
    public sealed class DemoEnemyStatusBinding : MonoBehaviour
    {
        [SerializeField] private DemoEnemyStatusItem prefab;
        [SerializeField] private DemoDamageReceiver health;
        [SerializeField] private DemoEnemyController enemy;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField, Min(0f)] private float headOffset = .2f;
        private DemoEnemyStatusItem _item;
        private RectTransform _layer;
        private Camera _camera, _uiCamera;
        public DemoEnemyStatusItem View => _item;
        public bool IsBound => _layer != null && _camera != null;

        public void Bind(RectTransform layer, Camera camera)
        {
            Unbind();
            if (prefab == null || health == null || enemy == null || layer == null || camera == null)
                throw new InvalidOperationException("敌人状态条模板、内部组件或会话引用缺失。");
            if (_item != null && _item.transform.parent != layer) { Destroy(_item.gameObject); _item = null; }
            if (_item == null) _item = Instantiate(prefab, layer);
            _layer = layer; _camera = camera;
            Canvas canvas = layer.GetComponentInParent<Canvas>();
            _uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            enabled = true; Refresh();
        }
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (_item == null) return;
            if (!IsBound || health == null || health.IsDefeated || !gameObject.activeInHierarchy)
            { _item.Clear(); return; }
            Vector3 world = transform.position + Vector3.up * ((sprite != null ? sprite.bounds.extents.y : .5f) + headOffset);
            Vector3 viewport = _camera.WorldToViewportPoint(world);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
            { _item.Clear(); return; }
            Vector3 screen = _camera.WorldToScreenPoint(world);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, _uiCamera, out Vector2 local))
                _item.Render(health, enemy, local);
            else _item.Clear();
        }
        public void Unbind()
        {
            enabled = false; _layer = null; _camera = _uiCamera = null;
            if (_item != null) _item.Clear();
        }
        private void OnDisable() => Unbind();
        private void OnDestroy() { if (_item != null) Destroy(_item.gameObject); }
    }
}
