using System;
using System.IO;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Player;
using Shanhai.Demo.Presentation;
using Shanhai.Demo.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shanhai.Demo.Editor
{
    internal static class DemoCombatFeedbackAuthoring
    {
        private const string ArcPath = "Assets/Game/VFX/Demo/DemoAttackArc.prefab";
        private const string HitPath = "Assets/Game/VFX/Demo/DemoHitParticle.prefab";
        private const string TextPath = "Assets/Game/Prefabs/UI/Item/DemoDamageText.prefab";
        [MenuItem("Shanhai/Demo/Author combat feedback")]
        private static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("先停止Play Mode。");
            Directory.CreateDirectory("Assets/Game/VFX/Demo"); Directory.CreateDirectory("Assets/Game/Materials/Demo"); AssetDatabase.Refresh();
            const string materialPath = "Assets/Game/Materials/Demo/DemoFeedback.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material, materialPath); }
            const string flashMaterialPath = "Assets/Game/Materials/Demo/DemoSpriteFeedback.mat";
            Material flashMaterial = AssetDatabase.LoadAssetAtPath<Material>(flashMaterialPath);
            if (flashMaterial == null)
            {
                var shader = Shader.Find("Shanhai/Demo/SpriteFeedback");
                if (shader == null) throw new InvalidOperationException("受击Shader缺失。");
                flashMaterial = new Material(shader); AssetDatabase.CreateAsset(flashMaterial, flashMaterialPath);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ArcPath) == null)
            {
                var root = new GameObject("DemoAttackArc"); Undo.RegisterCreatedObjectUndo(root, "创建斩击模板");
                try
                {
                    var line = root.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.useWorldSpace = false;
                    line.widthMultiplier = .1f; line.numCapVertices = 4; line.sortingOrder = 25;
                    var arc = root.AddComponent<DemoAttackArc>(); Set(arc, "line", line);
                    root.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root, ArcPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HitPath) == null)
            {
                var root = new GameObject("DemoHitParticle"); Undo.RegisterCreatedObjectUndo(root, "创建命中粒子模板");
                try
                {
                    var particles = root.AddComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = particles.main; main.loop = false; main.playOnAwake = false; main.duration = .4f;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(.18f, .4f); main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
                    main.startSize = new ParticleSystem.MinMaxCurve(.04f, .09f); main.maxParticles = 32;
                    main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = 0f;
                    var emission = particles.emission; emission.rateOverTime = 0f; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
                    var shape = particles.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .05f;
                    var velocity = particles.velocityOverLifetime; velocity.enabled = false;
                    var color = particles.colorOverLifetime; color.enabled = true;
                    var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                        new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }); color.color = gradient;
                    var renderer = root.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material; renderer.sortingOrder = 30;
                    root.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root, HitPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TextPath) == null)
            {
                var root = new GameObject("DemoDamageText", typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(root, "创建飘字Item模板");
                try
                {
                    var rect = (RectTransform)root.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(130f, 45f);
                    var text = root.AddComponent<TextMeshProUGUI>(); text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/Font/Common/AlibabaPuHuiTi-3-85-Bold SDF.asset");
                    text.fontSize = 30f; text.fontStyle = FontStyles.Bold; text.alignment = TextAlignmentOptions.Center;
                    text.raycastTarget = false; text.text = "10";
                    var item = root.AddComponent<DemoDamageTextItem>(); Set(item, "label", text);
                    root.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root, TextPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var config = AssetDatabase.LoadAssetAtPath<DemoPresentationConfig>("Assets/Game/ScriptableAssets/Demo/DemoPresentationConfig.asset");
            Set(config, "attackArcPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ArcPath).GetComponent<DemoAttackArc>());
            Set(config, "hitParticlePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(HitPath).GetComponent<ParticleSystem>());
            Set(config, "damageTextPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(TextPath).GetComponent<DemoDamageTextItem>());
            string hudPath = "Assets/Game/Prefabs/UI/DemoCombatOverlay.prefab";
            var hud = PrefabUtility.LoadPrefabContents(hudPath);
            try
            {
                Transform layer = hud.transform.Find("DamageLayer");
                if (layer == null)
                {
                    var go = new GameObject("DamageLayer", typeof(RectTransform)); go.transform.SetParent(hud.transform, false);
                    var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                    // 放在交互面板之前，飘字不遮挡奖励和结算操作。
                    go.transform.SetSiblingIndex(0); layer = go.transform;
                }
                Set(hud.GetComponent<DemoCombatOverlayForm>(), "damageLayer", layer);
                PrefabUtility.SaveAsPrefabAsset(hud, hudPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(hud); }
            foreach (string name in new[] { "DemoPlayer", "DemoCommonEnemy", "DemoEliteEnemy", "DemoBoss" })
            {
                string path = "Assets/Game/Prefabs/Entity/" + name + ".prefab"; var actor = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var sprite = actor.GetComponent<SpriteRenderer>();
                    if (sprite != null) Set(sprite, "m_Materials.Array.data[0]", flashMaterial);
                    if (actor.GetComponent<DemoCombatFeedbackBinding>() == null) Undo.AddComponent<DemoCombatFeedbackBinding>(actor);
                    PrefabUtility.SaveAsPrefabAsset(actor, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(actor); }
            }
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene("Assets/Game/Scene/Demo/DemoMain.unity", OpenSceneMode.Additive);
            try
            {
                DemoWorldEntry entry = null;
                foreach (GameObject root in scene.GetRootGameObjects()) if (root.GetComponent<DemoWorldEntry>() != null) entry = root.GetComponent<DemoWorldEntry>();
                if (entry == null) throw new InvalidOperationException("玩法会话入口缺失。");
                var feedback = entry.GetComponent<DemoFeedbackSession>(); if (feedback == null) feedback = Undo.AddComponent<DemoFeedbackSession>(entry.gameObject);
                Set(entry, "feedback", feedback); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
            AssetDatabase.SaveAssets(); Debug.Log("[Demo.FeedbackAuthoring] 2D斩击、命中粒子、飘字Item和会话引用已保存。");
        }
        private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            Undo.RecordObject(target, "绑定战斗表现引用"); var fields = new SerializedObject(target);
            fields.FindProperty(name).objectReferenceValue = value; fields.ApplyModifiedProperties();
        }
    }
}
