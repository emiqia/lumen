using Lumen.Core;
using UnityEngine;

namespace Lumen.FX
{
    /// <summary>Smooth follow with look-ahead, trauma-based screen shake and level bounds clamping.</summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig I { get; private set; }

        public Transform target;
        public float smoothTime = 0.12f;
        public float lookAhead = 0.8f;
        public Rect bounds;

        Vector3 basePos;      // follow position without shake
        Vector3 velocity;
        float trauma;
        Vector2 lastTargetPos;
        Vector2 aheadOffset;

        void Awake() => I = this;

        public static void Shake(float amount)
        {
            if (I != null && SaveData.ScreenShake) I.trauma = Mathf.Min(1f, I.trauma + amount);
        }

        public void SnapTo(Vector2 p)
        {
            basePos = new Vector3(p.x, p.y, -10f);
            transform.position = basePos;
            velocity = Vector3.zero;
            lastTargetPos = p;
            aheadOffset = Vector2.zero;
        }

        void LateUpdate()
        {
            if (target != null)
            {
                Vector2 tp = target.position;
                Vector2 moveDir = Time.deltaTime > 0 ? (tp - lastTargetPos) / Time.deltaTime : Vector2.zero;
                lastTargetPos = tp;
                aheadOffset = Vector2.Lerp(aheadOffset, Vector2.ClampMagnitude(moveDir * 0.25f, 1f) * lookAhead, Time.deltaTime * 4f);

                var desired = new Vector3(tp.x + aheadOffset.x, tp.y + aheadOffset.y, -10f);
                basePos = Vector3.SmoothDamp(basePos, desired, ref velocity, smoothTime);
                if (bounds.width > 0)
                {
                    basePos.x = Mathf.Clamp(basePos.x, bounds.xMin, bounds.xMax);
                    basePos.y = Mathf.Clamp(basePos.y, bounds.yMin, bounds.yMax);
                }
            }

            trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * 1.6f);
            Vector3 shake = Vector3.zero;
            if (trauma > 0f)
            {
                float s = trauma * trauma * 0.35f;
                float t = Time.unscaledTime * 30f;
                shake = new Vector3((Mathf.PerlinNoise(t, 0f) * 2f - 1f) * s, (Mathf.PerlinNoise(0f, t) * 2f - 1f) * s, 0f);
            }
            transform.position = basePos + shake;
        }
    }
}
