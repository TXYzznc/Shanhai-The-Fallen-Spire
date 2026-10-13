using System;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.UI;
using UnityEngine;

namespace Shanhai.Demo.Presentation
{
    /// <summary>展示对象的固定容量池；饱和时回收最旧展示，不改变玩法。</summary>
    public sealed class DemoFeedbackSession : MonoBehaviour
    {
        private DemoAttackArc[] _arcs;
        private ParticleSystem[] _particles;
        private float[] _particleEnds;
        private DemoDamageTextItem[] _texts;
        private Camera _camera;
        private RectTransform _layer;
        private int _arcCursor, _hitCursor, _textCursor;
        public int AttackCount { get; private set; }
        public int HitCount { get; private set; }
        public float LastDamage { get; private set; }
        public int ActiveCount
        {
            get
            {
                int count = 0;
                if (_arcs == null) return 0;
                foreach (var arc in _arcs) if (arc != null && arc.gameObject.activeSelf) count++;
                foreach (var particle in _particles) if (particle != null && particle.gameObject.activeSelf) count++;
                foreach (var text in _texts) if (text != null && text.gameObject.activeSelf) count++;
                return count;
            }
        }
        public void Initialize(DemoPresentationConfig config, RectTransform layer, Camera camera)
        {
            if (_arcs != null) throw new InvalidOperationException("反馈会话不能重复初始化。");
            if (config.AttackArcPrefab == null || config.HitParticlePrefab == null || config.DamageTextPrefab == null || layer == null)
                throw new InvalidOperationException("战斗表现Prefab或HUD飘字层缺失。");
            _camera = camera; _layer = layer;
            var eventWarmup = GameFramework.ReferencePool.Acquire<DemoCombatPresentationEvent>();
            GameFramework.ReferencePool.Release(eventWarmup);
            _arcs = new DemoAttackArc[config.AttackCapacity];
            _particles = new ParticleSystem[config.HitCapacity]; _particleEnds = new float[_particles.Length];
            _texts = new DemoDamageTextItem[config.TextCapacity];
            for (int i = 0; i < _arcs.Length; i++) { _arcs[i] = Instantiate(config.AttackArcPrefab, transform); _arcs[i].gameObject.SetActive(false); }
            for (int i = 0; i < _particles.Length; i++) { _particles[i] = Instantiate(config.HitParticlePrefab, transform); _particles[i].gameObject.SetActive(false); }
            for (int i = 0; i < _texts.Length; i++) { _texts[i] = Instantiate(config.DamageTextPrefab, layer); _texts[i].WarmUp(); }
        }
        public void ActionStarted(DemoCombatPresentationEvent args)
        {
            if (_arcs == null) return;
            _arcs[_arcCursor].Play(args); _arcCursor = (_arcCursor + 1) % _arcs.Length; AttackCount++;
        }
        public void Damaged(DemoCombatPresentationEvent args)
        {
            if (_particles == null) return;
            var particle = _particles[_hitCursor]; particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.transform.position = new Vector3(args.Position.x, args.Position.y, 0f);
            var main = particle.main; main.startColor = args.IsPlayer ? new Color(1f, .35f, .3f) : new Color(1f, .85f, .3f);
            particle.gameObject.SetActive(true); particle.Play(true); _particleEnds[_hitCursor] = Time.time + .6f;
            _hitCursor = (_hitCursor + 1) % _particles.Length;
            _texts[_textCursor].Play(args.Amount, args.Position, args.IsPlayer, _camera, _layer, _textCursor);
            _textCursor = (_textCursor + 1) % _texts.Length; HitCount++; LastDamage = args.Amount;
        }
        private void Update()
        {
            if (_particles == null) return;
            for (int i = 0; i < _particles.Length; i++)
                if (_particles[i] != null && _particles[i].gameObject.activeSelf && Time.time >= _particleEnds[i])
                { _particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); _particles[i].gameObject.SetActive(false); }
        }
        public void ResetFeedback()
        {
            if (_arcs == null) return;
            foreach (var arc in _arcs) if (arc != null) arc.gameObject.SetActive(false);
            foreach (var particle in _particles) if (particle != null) { particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); particle.gameObject.SetActive(false); }
            foreach (var text in _texts) if (text != null) text.gameObject.SetActive(false);
            _arcCursor = _hitCursor = _textCursor = AttackCount = HitCount = 0; LastDamage = 0f;
        }
        private void OnDestroy() { Release(); }
        public void Release()
        {
            ResetFeedback();
            if (_texts != null) foreach (var text in _texts) if (text != null) Destroy(text.gameObject);
            _texts = null; _arcs = null; _particles = null; _particleEnds = null; _layer = null; _camera = null;
        }
    }
}
