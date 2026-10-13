using TMPro;
using UnityEngine;

namespace Shanhai.Demo.UI
{
    public sealed class DemoDamageTextItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField, Min(.1f)] private float lifetime = .75f;
        [SerializeField] private float rise = 70f;
        private RectTransform _rect, _layer;
        private Camera _camera;
        private Vector3 _world;
        private float _startedAt, _offset;
        private Color _color;
        public float Amount { get; private set; }
        private void Awake() { _rect = (RectTransform)transform; }
        public void WarmUp()
        {
            gameObject.SetActive(true); label.SetText("{0:1}", 999999999f); label.ForceMeshUpdate();
            gameObject.SetActive(false);
        }
        public void Play(float amount, Vector3 world, bool player, Camera camera, RectTransform layer, int lane)
        {
            Amount = amount; _world = world; _camera = camera; _layer = layer;
            _offset = (lane % 5 - 2) * 18f; _startedAt = Time.time;
            _color = player ? new Color(1f, .35f, .3f) : new Color(1f, .9f, .5f);
            label.SetText("{0:1}", amount); label.color = _color;
            gameObject.SetActive(true); Update();
        }
        private void Update()
        {
            float progress = (Time.time - _startedAt) / lifetime;
            if (progress >= 1f || _camera == null || _layer == null) { gameObject.SetActive(false); return; }
            Vector3 screen = _camera.WorldToScreenPoint(_world);
            if (screen.z <= 0f) { gameObject.SetActive(false); return; }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, null, out Vector2 local))
                _rect.anchoredPosition = local + new Vector2(_offset, 30f + rise * progress);
            Color color = _color; color.a *= 1f - progress; label.color = color;
        }
    }
}
