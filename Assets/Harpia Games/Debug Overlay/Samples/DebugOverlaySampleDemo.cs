using UnityEngine;

namespace Harpia.Tools.DebugTools
{
    /// <summary>Sample: feeds runtime values to Debug Overlay.</summary>
    [AddComponentMenu("Harpia Games/Debug Overlay/Sample Demo")]
    public class DebugOverlaySampleDemo : MonoBehaviour
    {
        private enum DemoState
        {
            Idle,
            Running,
            Paused
        }

        [Tooltip("How fast the fake 'speed' value oscillates.")]
        [SerializeField] private float speed = 5f;

        [Tooltip("Optional GameObject to display and select from the overlay.")]
        [SerializeField] private GameObject watchedObject;

        private float _timer;
        private int _score;
        private bool _alive = true;
        private DemoState _state = DemoState.Idle;
        private float _speedMultiplier = 1f;

        private void Start()
        {
            // Editable variable, feeds back here.
            DebugOverlay.ShowVariable("Speed Multiplier", "1", value =>
            {
                if (float.TryParse(value, out float parsed))
                    _speedMultiplier = parsed;
            });

            // Action buttons.
            DebugOverlay.ShowButton("Add 100 Score", () => _score += 100, Color.yellow);
            DebugOverlay.ShowButton("Toggle Alive", () => _alive = !_alive, Color.cyan);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            _state = _alive ? DemoState.Running : DemoState.Paused;

            float wave = Mathf.Sin(_timer * speed) * _speedMultiplier;

            // Primitives.
            DebugOverlay.Show("Time", _timer);
            DebugOverlay.Show("FPS", 1f / Mathf.Max(Time.deltaTime, 0.0001f), 0);
            DebugOverlay.Show("Score", _score);
            DebugOverlay.Show("Alive", _alive, DebugOverlay.GetAssertionColor(_alive));
            DebugOverlay.Show("State", _state);
            DebugOverlay.Show("Wave", wave);

            // Vectors and math.
            DebugOverlay.Show("Position", transform.position);
            DebugOverlay.Show("Rotation", transform.rotation);
            DebugOverlay.Show("Mouse", (Vector2)Input.mousePosition);
            DebugOverlay.Show("Screen", new Vector2Int(Screen.width, Screen.height));

            // Color, animated red to green.
            DebugOverlay.Show("Fade Color", Color.Lerp(Color.red, Color.green, Mathf.Abs(wave)));

            // References.
            DebugOverlay.Show("This GameObject", gameObject);
            if (watchedObject != null)
                DebugOverlay.ShowButton("Select Watched", watchedObject.transform);
        }
    }
}
