//--------------------------------------------------------------------------//
// Copyright 2025-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.PartyIO.Demos
{
	/// <summary>
	/// Demo that shows how to derive from MouseCursorManager_Canvas to customise the handling of cursors
	/// </summary>
	/// <remarks>
	/// </remarks>
	public class CanvasCursorDemo : MouseCursorManager_Canvas
	{
		private Color[] _cursorColors = { Color.white, Color.red, Color.green, Color.blue };
		//private int _cursorSpawnCount;

		protected override void Awake()
		{
			#if !ENABLE_LEGACY_INPUT_MANAGER
			Debug.LogError("[MouseParty] MousePartyInputModule doesn't support Unity's new Input System yet - please change Project Settings to use the old Input System or 'Both'.");
			Debug.Break();
			#endif

			// Here we can replace or extend the base logic
			base.Awake();
		}

		protected override void OnEnable()
		{
			// Here we can replace or extend the base logic
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			// Here we can replace or extend the base logic
			base.OnDisable();
		}

		protected override void OnApplicationFocus(bool hasFocus)
		{
			// Here we can replace or extend the base logic
			base.OnApplicationFocus(hasFocus);
		}

		protected override void OnApplicationPause(bool pauseStatus)
		{
			// Here we can replace or extend the base logic
			base.OnApplicationPause(pauseStatus);
		}

		protected override void Update()
		{
			// Run the base cursor manager logic
			base.Update();

			// Custom logic for spawning and destroying cursors based on left/right click.
			var mice = _mouseDeviceManager.All;
			foreach (var mouse in mice)
			{
				if (mouse.WasPressedThisFrame(MouseButton.Left))
				{
					AddCursor(mouse);
				}
				else if (mouse.WasPressedThisFrame(MouseButton.Right))
				{
					RemoveCursor(mouse, removeFromList:true);
				}
			}
		}

		protected override BaseMouseCursor CreateCursor(MouseDevice mouse)
		{
			// We override this method to add some custom cursor color logic

			// Create the cursor state
			var result = base.CreateCursor(mouse);

			// Override cursor properties
			result.Color = _cursorColors[_cursors.Count % _cursorColors.Length];

			return result;
		}

		protected override void UpdateCursor(BaseMouseCursor cursorBase)
		{
			base.UpdateCursor(cursorBase);
			// This is called whenever a cursor is updated.
			// Here we could update any cursor properties, eg Color, Image etc.
		}

		protected override void DestroyCursor(BaseMouseCursor cursorBase)
		{
			// This is called whenever a cursor is destroyed.
			base.DestroyCursor(cursorBase);
		}
	}
}