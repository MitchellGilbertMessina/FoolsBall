//--------------------------------------------------------------------------//
// Copyright 2025-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.UI;

namespace ChocDino.PartyIO.Demos
{
    public class ColliderDemo : MonoBehaviour, IMousePartyEnterHandler, IMousePartyExitHandler, IMousePartyOverHandler, IMousePartyDownHandler, IMousePartyUpHandler
    {
        [SerializeField] Text _text;

		#region Standard Monobehaviour mouse events

		void OnMouseEnter()
        {
            Debug.Log("OnMouseEnter() - " + this.name);
            if (_text)
            {
                _text.text = "Enter " + this.name;
            }
        }

        void OnMouseExit()
        {
            Debug.Log("OnMouseExit() - " + this.name);
            if (_text)
            {
                _text.text = "Exit " + this.name;
            }
        }

        void OnMouseOver()
        {
            Debug.Log("OnMouseOver() - " + this.name);
            if (_text)
            {
                _text.text = "Over " + this.name;
            }
        }

        void OnMouseDown()
        {
            Debug.Log("OnMouseDown() - " + this.name);
            if (_text)
            {
                _text.text = "Down " + this.name;
            }
        }

        void OnMouseUp()
        {
            Debug.Log("OnMouseUp() - " + this.name);
            if (_text)
            {
                _text.text = "Up " + this.name;
            }
        }

        void OnMouseUpAsButton()
        {
            Debug.Log("OnMouseUpAsButton() - " + this.name);
            if (_text)
            {
                _text.text = "UpAsButton " + this.name;
            }
        }

		#endregion

		#region Mouse Party events

		void IMousePartyEnterHandler.OnMouseEnter(BaseMouseCursor cursor)
		{
            Debug.Log("OnMouseEnter - " + this.name + " by mouse #" + cursor.Mouse.DeviceId);
        }

        void IMousePartyExitHandler.OnMouseExit(BaseMouseCursor cursor)
        {
            Debug.Log("OnMouseExit - " + this.name + " by mouse #" + cursor.Mouse.DeviceId);
        }

        void IMousePartyOverHandler.OnMouseOver(BaseMouseCursor cursor)
        {
            Debug.Log("OnMouseOver - " + this.name + " by mouse #" + cursor.Mouse.DeviceId);
        }
    
        void IMousePartyDownHandler.OnMouseDown(BaseMouseCursor cursor, MouseButton button)
        {
            Debug.Log("OnMouseDown - " + this.name + " by mouse #" + cursor.Mouse.DeviceId + " " + button);
        }

        void IMousePartyUpHandler.OnMouseUp(BaseMouseCursor cursor, MouseButton button)
        {
            Debug.Log("OnMouseUp - " + this.name + " by mouse #" + cursor.Mouse.DeviceId + " " + button);
        }

        #endregion
    }
}