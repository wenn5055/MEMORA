using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace VRC.Examples.Persistence.PersistentPen
{
    public class UdonEraser : UdonSharpBehaviour
    {
        #region serialized fields
        [SerializeField][Tooltip("The renderer for the ERASER mesh, to update its color")] private Renderer eraserRenderer;
        #endregion 

        private DataList targetDataList;

        // The Pickup component that allows the pen to be picked up and used
        private VRCPickup pickup;
        private MeshRenderer eraserBody;
        private Collider eraserCollider;
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

        private void Start()
        {
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            eraserCollider = GetComponent<Collider>();
            eraserBody = GetComponent<MeshRenderer>();

            targetDataList = new DataList();
            // Disable the pen for others, hide until owner picks it up
            if (!Networking.IsOwner(gameObject))
            {
                eraserCollider.enabled = false;
                pickup.enabled = false;
                SetVisibleForOthers(false);
            }
        }

        private void SetVisibleForOthers(bool value)
        {
            if (Networking.IsOwner(gameObject))
                return;

            if (Utilities.IsValid(eraserRenderer))
                eraserRenderer.enabled = value;

            if (Utilities.IsValid(eraserBody))
                eraserBody.enabled = value;
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

        // On Use, Deactivate LineRenderer and Clear the Target
        public override void OnPickupUseDown()
        {
            for (int i = targetDataList.Count - 1; i >= 0; --i)
            {
                var targetCollider = (Collider)targetDataList[i].Reference;
                if (Utilities.IsValid(targetCollider))
                {
                    var targetPenLine = targetCollider.GetComponent<UdonPenLine>();
                    if (Utilities.IsValid(targetPenLine) && Networking.IsOwner(targetPenLine.gameObject))
                    {
                        targetPenLine.Erase();
                    }

                    InputManager.EnableObjectHighlight(targetCollider.gameObject, false);
                }

                targetDataList.RemoveAt(i);
            }
        }

        // Look for Valid Targets on Trigger Enter
        private void OnTriggerEnter(Collider other)
        {
            if (!Utilities.IsValid(other)) return;

            // Exit early if no self-owned UdonPenLine is found
            var penLine = other.GetComponent<UdonPenLine>();
            if (!Utilities.IsValid(penLine) || !Networking.IsOwner(penLine.gameObject))
            {
                return;
            }

            if (!targetDataList.Contains(other))
            {
                targetDataList.Add(other);
                InputManager.EnableObjectHighlight(other.gameObject, true);
            }
        }

        // On Trigger Exit, Re-Enable Target and Clear It
        private void OnTriggerExit(Collider other)
        {
            // Iterate backwards to remove the target collider from the list
            for (int i = targetDataList.Count - 1; i >= 0; --i)
            {
                var targetCollider = (Collider)targetDataList[i].Reference;
                if (targetCollider == other)
                {
                    InputManager.EnableObjectHighlight(targetCollider.gameObject, false);
                    targetDataList.RemoveAt(i);
                    break;
                }
            }
        }
    }
}