using System;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    public class CharacterManaHandler : NetworkBehaviour, IManaReciever
    {
        public event Action<int> ManaRecieved;

        [SerializeField]
        private NetworkLifeState m_NetworkLifeState;

        public NetworkManaState NetManaState { get; private set; }

        private CharacterClass CharacterClass;

        private void Awake()
        {
            NetManaState = GetComponent<NetworkManaState>();
        }

        public int ManaPoints
        {
            get => NetManaState.ManaPoints.Value;
            private set => NetManaState.ManaPoints.Value = value;
        }

        public void RecieveMana(int mana)
        {
            if (CanRecieveMana())
            {
                ManaPoints = Mathf.Clamp(ManaPoints + mana, 0, CharacterClass.BaseMana);
                Debug.Log($"{name} RECIEVED {mana} MANA");
            }
        }

        public bool CanRecieveMana()
        {
            return m_NetworkLifeState.LifeState.Value == LifeState.Alive;
        }

        public void InitializeMana(CharacterClass characterClass)
        {
            CharacterClass = characterClass;

            ManaPoints = CharacterClass.BaseHP.Value;

            if (!CharacterClass.IsNpc)
            {
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
                if (sessionPlayerData is { HasCharacterSpawned: true })
                {
                    ManaPoints = sessionPlayerData.Value.CurrentManaPoints;
                }
            }
        }
    }
}
