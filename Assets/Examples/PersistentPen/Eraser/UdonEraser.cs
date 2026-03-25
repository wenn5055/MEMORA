using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace VRC.Examples.Persistence.PersistentPen
{
    public class UdonEraser : UdonSharpBehaviour
    {
        private DataList targetDataList;

        private void Start()
        {
            targetDataList = new DataList();
        }

        // On Use, Deactivate LineRenderer and Clear the Target
        public override void OnPickupUseDown()
        {
            if (targetDataList.Count == 0) return;

            for (int i = targetDataList.Count - 1; i >= 0; --i)
            {
                var targetCollider = (Collider)targetDataList[i].Reference;
                var targetPenLine = targetCollider.GetComponent<UdonPenLine>();
                if (Utilities.IsValid(targetPenLine))
                {
                    if (Networking.IsOwner(targetPenLine.gameObject))
                    {
                        targetPenLine.Erase();
                    }

                    targetDataList.Remove(targetCollider);
                    InputManager.EnableObjectHighlight(targetCollider.gameObject, false);
                }
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

            targetDataList.Add(other);
            InputManager.EnableObjectHighlight(other.gameObject, true);
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
                    targetDataList.Remove(targetCollider);
                    break;
                }
            }
        }
    }
}