﻿using System;
using System.Collections.Generic;
using Anvil.CSharp.DelayedExecution;
using UnityEngine;

namespace Anvil.Unity.DelayedExecution
{
    public class UnityDeltaTimeCallLaterHandle : AbstractCallLaterHandle
    {
        public static UnityDeltaTimeCallLaterHandle Create(float secondsToWait, Action callback)
        {
            return new UnityDeltaTimeCallLaterHandle(secondsToWait, callback);
        }
        
        private static readonly List<Type> s_ValidUpdateSourceTypes = new List<Type>()
        {
            typeof(UnityUpdateSource),
            typeof(UnityLateUpdateSource)
        };
        
        private readonly float m_SecondsToWait;
        private float m_SecondsWaited;
        
        private UnityDeltaTimeCallLaterHandle(float secondsToWait, Action callback) : base(callback)
        {
            m_SecondsToWait = secondsToWait;
            m_SecondsWaited = 0.0f;
        }

        protected override void HandleOnUpdate()
        {
            m_SecondsWaited += Time.deltaTime;
            if (m_SecondsWaited >= m_SecondsToWait)
            {
                Complete();
            }
        }

        protected override List<Type> GetValidUpdateSourceTypes()
        {
            return s_ValidUpdateSourceTypes;
        }
    }
}

