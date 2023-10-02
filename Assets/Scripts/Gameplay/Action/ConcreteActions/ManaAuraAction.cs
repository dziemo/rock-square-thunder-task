using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    [CreateAssetMenu(menuName = "BossRoom/Actions/Mana Aura Action")]
    public class ManaAuraAction : Action
    {
        [SerializeField]
        private float auraApplyInterval;

        private float currentApplyInterval;

        public override bool OnStart(ServerCharacter serverCharacter)
        {
            return ActionConclusion.Continue;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            if (currentApplyInterval <= 0)
            {
                IManaReciever[] manaRecievers = GetManaRecievers(clientCharacter.transform.position);
                AffectManaRecievers(manaRecievers);
                currentApplyInterval = auraApplyInterval;
            }
            else
            {
                currentApplyInterval -= Time.deltaTime;
            }

            return ActionConclusion.Continue;
        }

        private void AffectManaRecievers(IManaReciever[] manaRecievers)
        {
            for (int i = 0; i < manaRecievers.Length; i++)
            {
                IManaReciever manaReciever = manaRecievers[i];
                if (manaReciever.CanRecieveMana())
                {
                    manaReciever.RecieveMana(Config.Amount);
                }
            }
        }

        private IManaReciever[] GetManaRecievers(Vector3 position)
        {
            Collider[] players = Physics.OverlapSphere(position, Config.Radius, LayerMask.GetMask("PCs"));
            IManaReciever[] manaRecievers = new IManaReciever[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                Collider player = players[i];
                if (player.TryGetComponent(out IManaReciever manaReciever))
                {
                    manaRecievers[i] = manaReciever;
                }
                else
                {
                    Debug.LogError($"{player.name} DOES NOT HAVE IManaReciever ATTACHED");
                }
            }

            return manaRecievers;
        }

        public override bool OnStartClient(ClientCharacter clientCharacter)
        {
            base.OnStartClient(clientCharacter);
            InstantiateSpecialFXGraphics(clientCharacter.transform, true);
            return true;
        }
    }
}
