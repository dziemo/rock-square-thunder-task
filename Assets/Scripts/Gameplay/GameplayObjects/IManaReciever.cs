namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    public interface IManaReciever
    {
        /// <summary>
        /// Receives mana drain or replenish.
        /// </summary>
        /// <param name="mana">The amount of mana. Negative value is mana drained, positive is mana replenished.</param>
        void RecieveMana(int mana);

        /// <summary>
        /// The NetworkId of this object.
        /// </summary>
        ulong NetworkObjectId { get; }

        bool CanRecieveMana();
    }
}
