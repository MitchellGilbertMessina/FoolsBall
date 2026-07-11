//--------------------------------------------------------------------------//
// Copyright 2025-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.EventSystems;

namespace ChocDino.PartyIO.Demos
{
    public class CanvasHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            MousePartyInputModule inputModule = eventData.currentInputModule as MousePartyInputModule;
            if (inputModule)
            {
                var cursor = inputModule.GetCursorFromEvent(eventData);
                string mouseId = "Unknown";
                if (cursor != null)
                {
                    mouseId = cursor.Mouse.DeviceId.ToString();
                }
                Debug.Log(string.Format("Mouse #{0} entered UI element '{1}'", mouseId, this.name));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            MousePartyInputModule inputModule = eventData.currentInputModule as MousePartyInputModule;
            if (inputModule)
            {
                var cursor = inputModule.GetCursorFromEvent(eventData);
                string mouseId = "Unknown";
                if (cursor != null)
                {
                    mouseId = cursor.Mouse.DeviceId.ToString();
                }
                Debug.Log(string.Format("Mouse #{0} exited UI element '{1}", mouseId, this.name));
            }
        }
    }
}