using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    public class NetworkManaState : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariable<int> ManaPoints = new NetworkVariable<int>();

        public event Action ManaPointsChangedEvent;

        private void OnEnable()
        {
            ManaPoints.OnValueChanged += ManaPointsChanged;
        }

        private void OnDisable()
        {
            ManaPoints.OnValueChanged -= ManaPointsChanged;
        }

        private void ManaPointsChanged(int previousValue, int newValue)
        {
            ManaPointsChangedEvent?.Invoke();
        }
    }
}
