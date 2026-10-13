using Shanhai.Demo.Combat;
using UnityEngine;

namespace Shanhai.Demo.Presentation
{
    public sealed class DemoAttackArc : MonoBehaviour
    {
        [SerializeField] private LineRenderer line;
        [SerializeField, Min(4)] private int segments = 24;
        private float _startedAt, _duration;
        private Color _color;
        public bool IsVisible => gameObject.activeSelf;
        public void Play(DemoCombatPresentationEvent args)
        {
            transform.position = new Vector3(args.Position.x, args.Position.y, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(args.Forward.y, args.Forward.x) * Mathf.Rad2Deg - 90f);
            _color = args.Action == DemoPlayerAction.Ultimate ? new Color(1f, .8f, .15f)
                : args.Action == DemoPlayerAction.Skill ? new Color(.2f, .85f, 1f) : new Color(1f, .55f, .2f);
            if (!args.IsPlayer) _color = new Color(1f, .22f, .16f);
            _duration = args.Action == DemoPlayerAction.Ultimate ? .4f : args.Action == DemoPlayerAction.Skill ? .3f : .22f;
            _startedAt = Time.time;
            line.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (-args.FanAngle * .5f + args.FanAngle * i / segments) * Mathf.Deg2Rad;
                line.SetPosition(i, new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * args.Radius);
            }
            line.startColor = line.endColor = _color;
            gameObject.SetActive(true);
        }
        private void Update()
        {
            float progress = (Time.time - _startedAt) / _duration;
            if (progress >= 1f) { gameObject.SetActive(false); return; }
            Color color = _color; color.a *= 1f - progress;
            line.startColor = line.endColor = color;
        }
    }
}
