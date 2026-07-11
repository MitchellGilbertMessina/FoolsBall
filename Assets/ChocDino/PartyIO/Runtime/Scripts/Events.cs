//--------------------------------------------------------------------------//
// Copyright 2025-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.PartyIO
{
    public interface IMousePartyEventHandler
    {
    }

    public interface IMousePartyEnterHandler : IMousePartyEventHandler
    {
        void OnMouseEnter(BaseMouseCursor cursor);
    }

    public interface IMousePartyExitHandler : IMousePartyEventHandler
    {
        void OnMouseExit(BaseMouseCursor cursor);
    }

    public interface IMousePartyOverHandler : IMousePartyEventHandler
    {
        void OnMouseOver(BaseMouseCursor cursor);
    }

    public interface IMousePartyDownHandler : IMousePartyEventHandler
    {
        void OnMouseDown(BaseMouseCursor cursor, MouseButton button);
    }

    public interface IMousePartyUpHandler : IMousePartyEventHandler
    {
        void OnMouseUp(BaseMouseCursor cursor, MouseButton button);
    }

    public static class Events
    {
        public delegate void EventFunction<T1>(T1 handler, BaseMouseCursor cursor);
        public delegate void EventButtonFunction<T1>(T1 handler, BaseMouseCursor cursor, MouseButton button);

        private static readonly EventFunction<IMousePartyEnterHandler> s_EnterHandler = Execute;
        private static readonly EventFunction<IMousePartyExitHandler> s_ExitHandler = Execute;
        private static readonly EventFunction<IMousePartyOverHandler> s_OverHandler = Execute;
        private static readonly EventButtonFunction<IMousePartyDownHandler> s_DownHandler = Execute;
        private static readonly EventButtonFunction<IMousePartyUpHandler> s_UpHandler = Execute;

        private static void Execute(IMousePartyEnterHandler handler, BaseMouseCursor eventData)
        {
            handler.OnMouseEnter(eventData);
        }

        private static void Execute(IMousePartyExitHandler handler, BaseMouseCursor eventData)
        {
            handler.OnMouseExit(eventData);
        }

        private static void Execute(IMousePartyOverHandler handler, BaseMouseCursor eventData)
        {
            handler.OnMouseOver(eventData);
        }

        private static void Execute(IMousePartyDownHandler handler, BaseMouseCursor eventData, MouseButton button)
        {
            handler.OnMouseDown(eventData, button);
        }

        private static void Execute(IMousePartyUpHandler handler, BaseMouseCursor eventData, MouseButton button)
        {
            handler.OnMouseUp(eventData, button);
        }

        public static EventFunction<IMousePartyEnterHandler> enterHandler
        {
            get { return s_EnterHandler; }
        }

        public static EventFunction<IMousePartyExitHandler> exitHandler
        {
            get { return s_ExitHandler; }
        }

        public static EventFunction<IMousePartyOverHandler> overHandler
        {
            get { return s_OverHandler; }
        }

        public static EventButtonFunction<IMousePartyDownHandler> downHandler
        {
            get { return s_DownHandler; }
        }

        public static EventButtonFunction<IMousePartyUpHandler> upHandler
        {
            get { return s_UpHandler; }
        }

        public static void Execute<T>(GameObject target, BaseMouseCursor cursor, EventFunction<T> functor) where T : IMousePartyEventHandler
        {
            var eventHandlers = target.GetComponents<T>();
            if (eventHandlers != null)
            {
                foreach (var eventHandler in eventHandlers)
                {
                    if (eventHandler is T implementation)
                    {
                        functor(eventHandler, cursor);
                    }
                }
            }
        }

        public static void ExecuteButton<T>(GameObject target, BaseMouseCursor cursor, MouseButton button, EventButtonFunction<T> functor) where T : IMousePartyEventHandler
        {
            var eventHandlers = target.GetComponents<T>();
            if (eventHandlers != null)
            {
                foreach (var eventHandler in eventHandlers)
                {
                    if (eventHandler is T implementation)
                    {
                        functor(eventHandler, cursor, button);
                    }
                }
            }
        }
    }
}