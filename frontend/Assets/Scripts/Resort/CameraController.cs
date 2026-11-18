using UnityEngine;
using UnityEngine.EventSystems;

namespace ResortIsland.Resort
{
    /// Moves the orthographic camera over the island:
    /// - drag with the left button / one finger on empty land (not on a building, not while a path tool
    ///   paints), drag with the middle button anywhere,
    /// - mouse wheel, pinch with two fingers or Q / E to zoom (towards the cursor),
    /// - arrow keys / WASD to move.
    /// The view never leaves the island. Added to the main camera at runtime when the scene has none.
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float minZoom = 2.5f;
        [SerializeField, Min(0.5f)] private float maxZoom = 10f;
        [SerializeField, Min(0f)] private float wheelZoomStep = 0.12f;
        [SerializeField, Min(0f)] private float keyPanSpeed = 9f;
        [SerializeField, Min(0f)] private float keyZoomSpeed = 4f;
        [Tooltip("How far the pointer has to move (pixels) before a press becomes a drag.")]
        [SerializeField, Min(0f)] private float dragThreshold = 10f;
        [Tooltip("Extra room around the island the view may show (world units).")]
        [SerializeField] private float boundsMargin = 2f;

        /// True once the current left press moved the camera: a release then is not a tap.
        public static bool DraggedThisPress { get; private set; }

        private Camera cam;
        private ResortManager resort;
        private BuildingPlacer placer;
        private IslandGenerator island;
        private Rect bounds;
        private bool boundsReady;

        private bool pressCanPan;
        private bool panning;
        private int panButton = -1;
        private Vector3 pressScreen;
        private Vector3 grabWorld;
        private float pinchDistance;
        private Vector2 pinchCentre;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnMainCamera()
        {
            Camera main = Camera.main;
            if (main != null && main.GetComponent<CameraController>() == null)
                main.gameObject.AddComponent<CameraController>();
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            resort = FindFirstObjectByType<ResortManager>();
            placer = FindFirstObjectByType<BuildingPlacer>();
            island = FindFirstObjectByType<IslandGenerator>();
        }

        private void LateUpdate()
        {
            if (!cam.orthographic) return;
            if (!boundsReady) boundsReady = TryComputeBounds();

            if (Input.touchCount >= 2) UpdatePinch();
            else
            {
                pinchDistance = 0f;
                UpdateMouseDrag();
                UpdateWheel();
            }
            UpdateKeys();
            Clamp();
        }

        // ---- dragging -----------------------------------------------------------------------------

        private void UpdateMouseDrag()
        {
            if (Input.GetMouseButtonDown(0)) BeginPress(0);
            if (Input.GetMouseButtonDown(2)) BeginPress(2);

            if (panButton >= 0 && Input.GetMouseButton(panButton))
            {
                if (!panning && pressCanPan && (Input.mousePosition - pressScreen).magnitude >= dragThreshold)
                {
                    panning = true;
                    if (panButton == 0) DraggedThisPress = true;
                }
                // keep the point that was grabbed under the cursor
                if (panning) transform.position += grabWorld - ScreenToWorld(Input.mousePosition);
            }

            if (panButton >= 0 && !Input.GetMouseButton(panButton))
            {
                panButton = -1;
                panning = false;
            }
            if (!Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0)) DraggedThisPress = false;
        }

        private void BeginPress(int button)
        {
            if (panButton >= 0) return;
            panButton = button;
            panning = false;
            pressScreen = Input.mousePosition;
            grabWorld = ScreenToWorld(pressScreen);
            if (button == 0) DraggedThisPress = false;
            pressCanPan = !IsPointerOverUi() && (button == 2 || !PlacerUsesPress());
        }

        // the left press belongs to the placer: painting a path, or dragging a building
        private bool PlacerUsesPress()
        {
            if (placer == null || resort == null) return false;
            if (placer.ActivePathTool != PathTool.None) return true;
            if (placer.SelectedBuilding != null) return false; // placing happens on release, a drag moves the view
            return resort.TryGetBuilding(resort.WorldToCell(ScreenToWorld(Input.mousePosition)), out _);
        }

        private void UpdatePinch()
        {
            Touch a = Input.GetTouch(0), b = Input.GetTouch(1);
            float distance = Vector2.Distance(a.position, b.position);
            Vector2 centre = (a.position + b.position) * 0.5f;
            DraggedThisPress = true;

            if (pinchDistance > 0f && distance > 1f)
            {
                Vector3 before = ScreenToWorld(centre);
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * pinchDistance / distance, minZoom, maxZoom);
                transform.position += before - ScreenToWorld(centre);
                transform.position += ScreenToWorld(pinchCentre) - ScreenToWorld(centre);
            }
            pinchDistance = distance;
            pinchCentre = centre;
        }

        // ---- zoom and keys ------------------------------------------------------------------------

        private void UpdateWheel()
        {
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(wheel, 0f) || IsPointerOverUi()) return;
            ZoomAt(Input.mousePosition, Mathf.Pow(1f - wheelZoomStep, wheel));
        }

        private void UpdateKeys()
        {
            float dt = Time.unscaledDeltaTime;
            var move = new Vector3(
                (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) ? 1f : 0f),
                (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) ? 1f : 0f),
                0f);
            if (move != Vector3.zero)
                transform.position += move.normalized * keyPanSpeed * dt * (cam.orthographicSize / 7f);

            float zoom = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
            if (!Mathf.Approximately(zoom, 0f))
                ZoomAt(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f), Mathf.Exp(-zoom * keyZoomSpeed * dt * 0.3f));
        }

        private void ZoomAt(Vector3 screenPoint, float factor)
        {
            Vector3 before = ScreenToWorld(screenPoint);
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * factor, minZoom, maxZoom);
            transform.position += before - ScreenToWorld(screenPoint);
        }

        // ---- limits ------------------------------------------------------------------------------

        private bool TryComputeBounds()
        {
            if (resort == null || island == null || !resort.IsReady) return false;
            int halfW = island.MapWidth / 2, halfH = island.MapHeight / 2;
            Vector3[] corners =
            {
                resort.CellToWorld(new Vector3Int(-halfW, -halfH, 0)), resort.CellToWorld(new Vector3Int(halfW, -halfH, 0)),
                resort.CellToWorld(new Vector3Int(-halfW, halfH, 0)), resort.CellToWorld(new Vector3Int(halfW, halfH, 0))
            };
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector3 c in corners)
            {
                minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
                minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
            }
            bounds = Rect.MinMaxRect(minX - boundsMargin, minY - boundsMargin, maxX + boundsMargin, maxY + boundsMargin);
            return true;
        }

        private void Clamp()
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            if (!boundsReady) return;

            // the centre stays inside the island, so the edge of the view never shows much more than sea
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, bounds.xMin, bounds.xMax);
            p.y = Mathf.Clamp(p.y, bounds.yMin, bounds.yMax);
            transform.position = p;
        }

        private Vector3 ScreenToWorld(Vector3 screen)
        {
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -transform.position.z));
            world.z = 0f;
            return world;
        }

        private static bool IsPointerOverUi()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
