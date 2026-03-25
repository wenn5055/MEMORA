using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace VRC.Examples.Persistence.PersistentPen
{
    public class UdonPen : UdonSharpBehaviour
    {
        #region Serialized Fields

        [SerializeField] [Tooltip("The amount the pen must be moved before adding a new point. Increase for chunkier lines, decrease for smoother.")]
        private float minMoveDistance = 0.001f;
        
        [SerializeField] [Tooltip("The minimum amount of time the pen must be held down before drawing. Changes color if not drawing.")]
        private float minHoldTime = 0.2f;
        
        private float holdTimeStart;
        
        [SerializeField] [Tooltip("How many points will be batched together for each network update. Lower this number to send updates more often, raise it to wait for more points in order to reduce bandwidth usage.")]
        private int pointsPerUpdate = 10;
        
        [SerializeField] [Tooltip("The transform from which the points will be created.")]
        private Transform penTip;
        
        [SerializeField] [Tooltip("The Transform that contains all of the LineRenderers as children.")]
        private Transform linesContainer;

        [SerializeField] private Gradient paletteColor;
        [SerializeField] [Tooltip("The renderer for the pen mesh, to update its color")] private Renderer penRenderer;
        [SerializeField] [Tooltip("The first color to have selected")] private int firstColorIndex;

        #endregion
        
        // The Pickup component that allows the pen to be picked up and used
        private VRCPickup pickup;
        // The current line being drawn
        private UdonPenLine line;
        // Gates the update loop to only run when the pen is down
        private bool pickupIsDown;
        // Tracks whether the pen has been moved enough to be considered drawing
        private bool isDrawing;
        // The position where the pen was last updated, used to determine if the pen has moved enough to add a new point
        private Vector3 startPosition;

        private MeshRenderer penBody;
        private Collider penCollider;
        private LineRenderer currentLineRenderer;
        private int currentIndex;
        private int nextLineIndex;
        private LineRenderer[] linePool;
        
        // Color to use for next line, updates Pen material on change
        [UdonSynced, FieldChangeCallback(nameof(ActiveColorIndex))] private int _activeColorIndex;
        public int ActiveColorIndex
        {
            set
            {
                _activeColorIndex = value;
                UpdatePenColor();
            }
            get => _activeColorIndex;
        }
        
        // Whether pickup is held, changes visibility for others
        [UdonSynced, FieldChangeCallback(nameof(IsHeld))] private bool _isHeld;

        public bool IsHeld
        {
            set
            {
                _isHeld = value;
                SetVisibleForOthers(value);
            }
            get => _isHeld;
        }
        
        void Start()
        {
            // Cache Pickup
            pickup = GetComponent<VRCPickup>();
            if (Networking.LocalPlayer.IsUserInVR())
            {
                pickup.orientation = VRC_Pickup.PickupOrientation.Any;
            }
            
            // Cache pen body
            penBody = GetComponent<MeshRenderer>();
            
            // Cache pen collider
            penCollider = GetComponent<Collider>();
            
            // Fill Pool from GameObjects on Start
            linePool = linesContainer.GetComponentsInChildren<LineRenderer>();
            
            // Set color to first color
            ActiveColorIndex = firstColorIndex;
            UpdatePenColor();

            // Disable the pen for others, hide until owner picks it up
            if (!Networking.IsOwner(gameObject))
            {
                penCollider.enabled = false;
                pickup.enabled = false;
                SetVisibleForOthers(false);
            }
        }
        
        public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
        {
            if (!Networking.IsOwner(gameObject)) return;

            if (PlayerData.HasKey(Networking.LocalPlayer, "nextLineIndex"))
            {
                nextLineIndex = PlayerData.GetInt(Networking.LocalPlayer, "nextLineIndex");
            }
        }

        private void SetVisibleForOthers(bool value)
        {
            if (Networking.IsOwner(gameObject))
                return;
            
            penRenderer.enabled = value;
            penBody.enabled = value;
        }

        public override void OnPickup()
        {
            IsHeld = true;
            RequestSerialization();
        }

        public override void OnDrop()
        {
            IsHeld = false;
            RequestSerialization();
        }

        public override void OnPickupUseDown()
        {
            // Reset Variables
            holdTimeStart = Time.time;
            pickupIsDown = true;
            isDrawing = false;
            startPosition = penTip.position;
            currentIndex = 0;
        }

        private void InitNextLine()
        {
            // Get a new line from the Pool
            Debug.Log($"InitNextLine: nextLineIndex: {nextLineIndex}");
            currentLineRenderer = linePool[nextLineIndex];
            currentLineRenderer.positionCount = 2;
            line = currentLineRenderer.GetComponent<UdonPenLine>();
            
            // move line to counter offset in world space
            currentLineRenderer.transform.localPosition = Vector3.zero - currentLineRenderer.transform.parent.TransformPoint(Vector3.zero);
            
            // Set ownership of line to local player
            Networking.SetOwner(Networking.LocalPlayer, currentLineRenderer.gameObject);
            currentLineRenderer.gameObject.SetActive(true);

            // Initialize line with 2 points at the pen tip
            for (int i = 0; i < 2; i++)
            {
                currentLineRenderer.SetPosition(i, penTip.position);
            }
            
            // Set color of line
            line.SyncedColor = GetActiveColor();
            
            // Increment nextLineIndex
            nextLineIndex = (nextLineIndex + 1) % linePool.Length;
            PlayerData.SetInt("nextLineIndex", nextLineIndex);
        }

        private void Update()
        {
            // Are we drawing?
            if(!pickupIsDown) return;
            
            // Has the pen moved enough?
            if(Vector3.Distance(penTip.position, startPosition) > minMoveDistance)
            {
                // If not drawing, initialize the next line
                if (!isDrawing)
                {
                    InitNextLine();
                    isDrawing = true;
                }
                currentLineRenderer.positionCount = currentIndex + 1;
                startPosition = penTip.position;
                currentLineRenderer.SetPosition(currentIndex, startPosition);
                currentIndex++;
                // Group Point Updates to reduce network traffic
                if(currentIndex % pointsPerUpdate == 0)
                {
                    line.OnUpdate();
                }
            }
        }

        private Color GetActiveColor()
        {
            return paletteColor.Evaluate(_activeColorIndex / ((float)paletteColor.colorKeys.Length - 1));
        }

        private void UpdatePenColor()
        {
            penRenderer.material.color = GetActiveColor();
        }

        public override void OnPickupUseUp()
        {
            // Reset pickup state
            pickupIsDown = false;
            
            // Finish the line if we're drawing
            if(isDrawing)
            {
                line.OnFinish();
                isDrawing = false;
            }
            
            // If the pen has been tapped quickly, reset and change color
            if(Time.time - holdTimeStart < minHoldTime && !isDrawing)
            {
                ActiveColorIndex = (ActiveColorIndex + 1) % paletteColor.colorKeys.Length;
                UpdatePenColor();
            }
        }
    }

}