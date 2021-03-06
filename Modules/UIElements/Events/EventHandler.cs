// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public interface IEventHandler
    {
        void SendEvent(EventBase e);

        void HandleEvent(EventBase evt);

        bool HasTrickleDownHandlers();

        bool HasBubbleUpHandlers();
    }

    public abstract class CallbackEventHandler : IEventHandler
    {
        EventCallbackRegistry m_CallbackRegistry;

        public void RegisterCallback<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback<TEventType>(callback, useTrickleDown);
        }

        public void RegisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TUserArgsType userArgs, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback<TEventType, TUserArgsType>(callback, userArgs, useTrickleDown);
        }

        public void UnregisterCallback<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry != null)
            {
                m_CallbackRegistry.UnregisterCallback(callback, useTrickleDown);
            }
        }

        public void UnregisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry != null)
            {
                m_CallbackRegistry.UnregisterCallback(callback, useTrickleDown);
            }
        }

        internal bool TryGetUserArgs<TEventType, TCallbackArgs>(EventCallback<TEventType, TCallbackArgs> callback, TrickleDown useTrickleDown, out TCallbackArgs userData) where TEventType : EventBase<TEventType>, new()
        {
            userData = default(TCallbackArgs);

            if (m_CallbackRegistry != null)
            {
                return m_CallbackRegistry.TryGetUserArgs(callback, useTrickleDown, out userData);
            }

            return false;
        }

        public abstract void SendEvent(EventBase e);

        public virtual void HandleEvent(EventBase evt)
        {
            if (evt == null)
                return;

            if (evt.propagationPhase != PropagationPhase.DefaultAction)
            {
                if (!evt.isPropagationStopped)
                {
                    m_CallbackRegistry?.InvokeCallbacks(evt);
                }

                if (evt.propagationPhase == PropagationPhase.AtTarget && !evt.isDefaultPrevented)
                {
                    ExecuteDefaultActionAtTarget(evt);
                }
            }
            else if (!evt.isDefaultPrevented)
            {
                ExecuteDefaultAction(evt);
            }
        }

        public bool HasTrickleDownHandlers()
        {
            return m_CallbackRegistry != null && m_CallbackRegistry.HasTrickleDownHandlers();
        }

        public bool HasBubbleUpHandlers()
        {
            return m_CallbackRegistry != null && m_CallbackRegistry.HasBubbleHandlers();
        }

        protected internal virtual void ExecuteDefaultActionAtTarget(EventBase evt) {}

        protected internal virtual void ExecuteDefaultAction(EventBase evt) {}
    }
}
