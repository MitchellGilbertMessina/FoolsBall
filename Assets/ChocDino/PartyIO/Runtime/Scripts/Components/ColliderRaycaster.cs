//--------------------------------------------------------------------------//
// Copyright 2025-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ChocDino.PartyIO
{
    /// <summary>
    /// Adds Mouse Party support for MonoBehaviour.OnMouseEnter/OnMouseExter/OnMouseOver() for Collider or Collider2D.
    /// This involves manually raycasting to all colliders for each active cursor and then using reflection
    /// to call those methods on each MonoBehaviour in that GameObject.
    /// Also adds support for Mouse Party events which are similiar but also contain the Cursor that triggered the event.
    /// </summary>
    public class ColliderRaycaster : MonoBehaviour
    {
        [SerializeField] Camera _camera = null;
        [SerializeField] LayerMask _layer3D = Physics.DefaultRaycastLayers;
        [SerializeField] LayerMask _layer2D = 0;
        [SerializeField] float _maxRaycastDistance = 100f;

        [SerializeField] bool _emitStandardEvents = true;
        [SerializeField] bool _emitMousePartyEvents = true;

        public Camera Camera => _camera;
        public LayerMask Layer3D => _layer3D;
        public LayerMask Layer2D => _layer2D;
        public float MaxRaycastDistance => _maxRaycastDistance;
        public bool EmitStandardEvents => _emitStandardEvents;
        public bool EmitMousePartyEvents => _emitMousePartyEvents;

        // State
        private Dictionary<int, GameObject> _lastHoveredObjects3D;
        private Dictionary<int, GameObject> _lastHoveredObjects2D;

        // Cache to optimise reflection
        private static readonly Dictionary<Type, Dictionary<string, Action<MonoBehaviour>>> s_methodCache = new Dictionary<Type, Dictionary<string, Action<MonoBehaviour>>>();

        void Awake()
        {
            ClearState();
		}

		private void Start()
		{
			if (_camera == null)
            {
                _camera = Camera.main;
			}
		}

		public void ClearState()
        {
            _lastHoveredObjects3D = new Dictionary<int, GameObject>();
            _lastHoveredObjects2D = new Dictionary<int, GameObject>();
        }

        public bool IsAnyOver(GameObject gameObject)
        {
            foreach (var go in _lastHoveredObjects3D.Values)
            {
                if (go == gameObject)
                {
                    return true;
                }
            }
            foreach (var go in _lastHoveredObjects2D.Values)
            {
                if (go == gameObject)
                {
                    return true;
                }
            }
            return false;
        }

        void Update()
        {
            if (MouseCursorManager.Instance)
            {
                Camera camera = _camera;
                if (!camera)
                {
                    camera = Camera.main;
				}

                if (camera)
                {
                    foreach (var cursor in MouseCursorManager.Instance.Cursors)
                    {
                        if (cursor.Enabled && cursor.Mouse.ConnectionState == MouseConnectionState.Connected)
                        {
                            Ray ray = camera.ScreenPointToRay(cursor.ScreenPosition);

                            // Collider
                            if (_layer3D != 0)
                            {
                                GameObject currentHitObject = null;
                                if (Physics.Raycast(ray, out RaycastHit hit, _maxRaycastDistance, _layer3D))
                                {
                                    currentHitObject = hit.collider.gameObject;
                                }

                                UpdateState(currentHitObject, cursor, _lastHoveredObjects3D);
                            }

                            // Collider2D
                            if (_layer2D != 0)
                            {
                                GameObject currentHitObject = null;
                                RaycastHit2D hit = Physics2D.GetRayIntersection(ray, _maxRaycastDistance, _layer2D);
                                if (hit.transform != null)
                                {
                                    currentHitObject = hit.transform.gameObject;
                                }

                                UpdateState(currentHitObject, cursor, _lastHoveredObjects2D);
                            }
                        }
                    }
                }
            }
        }

        private void UpdateState(GameObject currentHitObject, BaseMouseCursor cursor, Dictionary<int, GameObject> lastHoveredObjects)
        {
            MouseDevice mouseDevice = cursor.Mouse;

            GameObject lastHoveredObject;
            lastHoveredObjects.TryGetValue(mouseDevice.DeviceId, out lastHoveredObject);

            // Handle buttons
            if (currentHitObject != null)
            {
                if (mouseDevice.WasPressedThisFrame(MouseButton.Left))
                {
                    if (_emitMousePartyEvents)
                    {
                        Events.ExecuteButton(currentHitObject, cursor, MouseButton.Left, Events.downHandler);
                    }
                    if (_emitStandardEvents)
                    {
                        InvokeMethodOptimised(currentHitObject, "OnMouseDown");
                    }
                }
                if (mouseDevice.WasPressedThisFrame(MouseButton.Right))
                {
                    if (_emitMousePartyEvents)
                    {
                        Events.ExecuteButton(currentHitObject, cursor, MouseButton.Left, Events.downHandler);
                    }
                    if (_emitStandardEvents)
                    {
                        InvokeMethodOptimised(currentHitObject, "OnMouseDown");
                    }
                }
                if (mouseDevice.WasReleasedThisFrame(MouseButton.Left))
                {
                    if (_emitMousePartyEvents)
                    {
                        Events.ExecuteButton(currentHitObject, cursor, MouseButton.Left, Events.upHandler);
                    }
                    if (_emitStandardEvents)
                    {
                        InvokeMethodOptimised(currentHitObject, "OnMouseUpAsButton");
                    }
                }
                if (mouseDevice.WasReleasedThisFrame(MouseButton.Right))
                {
                    if (_emitMousePartyEvents)
                    {
                        Events.ExecuteButton(currentHitObject, cursor, MouseButton.Left, Events.upHandler);
                    }
                    if (_emitStandardEvents)
                    {
                        InvokeMethodOptimised(currentHitObject, "OnMouseUpAsButton");
                    }
                }

                // TODO: Implement OnMouseUp - this involves keeping track of where it was pressed down.
                // See: https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnMouseUp.html
                //InvokeMethodOptimised(currentHitObject, "OnMouseUp");

                // TODO: Implmenent OnMouseDrag() - this involves keeping track of state.
            }

            // Compare states to mimic the native hover sequence
            if (currentHitObject != lastHoveredObject)
            {
                if (lastHoveredObject != null)
                {
                    if (_emitMousePartyEvents)
                    {
                        Events.Execute(lastHoveredObject, cursor, Events.exitHandler);
                    }
                    if (_emitStandardEvents)
                    {
                        InvokeMethodOptimised(lastHoveredObject, "OnMouseExit");
                    }
                }

                if (currentHitObject != null)
                {
                    if (_emitMousePartyEvents)
                    {
                        Events.Execute(currentHitObject, cursor, Events.enterHandler);
                    }
                    if (_emitStandardEvents)
                    {
                        InvokeMethodOptimised(currentHitObject, "OnMouseEnter");
                    }
                    // "Over" is called immediately after entering
                    if (_emitMousePartyEvents)
                    {
                        Events.Execute(currentHitObject, cursor, Events.overHandler);
                    }
                    if (_emitStandardEvents)
                    {
                        InvokeMethodOptimised(currentHitObject, "OnMouseOver");
                    }
                }

                lastHoveredObjects[mouseDevice.DeviceId] = currentHitObject;
            }
            else if (currentHitObject != null)
            {
                if (_emitMousePartyEvents)
                {
                    Events.Execute(currentHitObject, cursor, Events.overHandler);
                }
                if (_emitStandardEvents)
                {
                    InvokeMethodOptimised(currentHitObject, "OnMouseOver");
                }
            }
        }

        private static void InvokeMethodSimple(GameObject target, string methodName)
        {
            // Get every MonoBehaviour script attached to this specific GameObject
            MonoBehaviour[] components = target.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour script in components)
            {
                if (script == null) continue;

                // Use reflection to find the method, even if it is private or protected
                MethodInfo method = script.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (method != null)
                {
                    method.Invoke(script, null);
                }
            }
        }

        private void InvokeMethodOptimised(GameObject target, string methodName)
        {
            // Get components without allocating a new array every frame (GC optimization)
            List<MonoBehaviour> components = new List<MonoBehaviour>();
            target.GetComponents(components);

            int count = components.Count;
            for (int i = 0; i < count; i++)
            {
                MonoBehaviour script = components[i];
                if (script == null) continue;

                Type type = script.GetType();

                // 1. Ensure the component type is registered in our cache
                if (!s_methodCache.TryGetValue(type, out var methodMap))
                {
                    methodMap = new Dictionary<string, Action<MonoBehaviour>>();
                    s_methodCache[type] = methodMap;
                }

                // 2. Ensure the specific method delegate is compiled and cached
                if (!methodMap.TryGetValue(methodName, out Action<MonoBehaviour> compiledDelegate))
                {
                    compiledDelegate = CreateDelegate(type, methodName);
                    methodMap[methodName] = compiledDelegate; // Caches even null if method doesn't exist
                }

                // 3. Fast execution of the pre-compiled delegate (Zero Reflection Overhead)
                compiledDelegate?.Invoke(script);
            }
        }

        private static Action<MonoBehaviour> CreateDelegate(Type type, string methodName)
        {
            // Find the private or public method on the script type
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (methodInfo == null) return null;

            // Compile the MethodInfo into a highly optimized Open Delegate
            try
            {
                // Creates an open delegate that accepts 'this' target as the first parameter
                var openDelegate = Delegate.CreateDelegate(typeof(Action<MonoBehaviour>), null, methodInfo, false);
                if (openDelegate != null)
                {
                    return (Action<MonoBehaviour>)openDelegate;
                }
            }
            catch
            {
                // Fallback strategy for rare edge cases or mismatched signatures
                return (instance) => methodInfo.Invoke(instance, null);
            }

            // Secondary fallback if Delegate.CreateDelegate fails directly due to type safety checks
            return (instance) => methodInfo.Invoke(instance, null);
        }
    }
}