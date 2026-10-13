using System.Collections;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoCameraFollowAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private Camera demoCamera;

        private IEnumerator Start()
        {
            yield return null;
            GetComponent<DemoPlayerController>().enabled = false;
            foreach (var enemy in FindObjectsOfType<DemoEnemyController>(true)) enemy.enabled = false;
            var body = GetComponent<Rigidbody2D>();
            Vector2 original = body.position;
            float[] segments = { 0f, 12f, 24f, 36f, 48f, 56f, 0f };
            bool visible = true;
            bool stable = true;
            foreach (float y in segments)
            {
                body.position = new Vector2(0f, y);
                Physics2D.SyncTransforms();
                yield return new WaitForEndOfFrame();
                Vector3 viewport = demoCamera.WorldToViewportPoint(transform.position);
                visible &= viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f && viewport.z > 0f;
                stable &= Mathf.Abs(viewport.y - 0.35f) < 0.01f;
            }
            bool returned = Mathf.Abs(demoCamera.transform.position.y - 3f) < 0.01f;
            bool orthographic2D = demoCamera.orthographic && Vector3.Dot(demoCamera.transform.forward, Vector3.forward) > 0.999f;
            body.position = original;
            Debug.Log($"[DemoCameraFollowAcceptanceProbe] AllSegmentsVisible={visible}, StableFraming={stable}, ReturnToStart={returned}, Orthographic2D={orthographic2D}");
        }
    }
}
