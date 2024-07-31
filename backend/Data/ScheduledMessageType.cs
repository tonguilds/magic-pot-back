namespace MagicPot.Backend.Data
{
    public enum ScheduledMessageType
    {
        /// <summary>
        /// Unused value. Message will be ignored.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Сообщение "пот" (в ответ на нажатие кнопки "get referral link").
        /// </summary>
        ReferralRichMessage = 1,

        /// <summary>
        /// Сообщение создателю пота о том, что первая транзакция в пот зашла и отсчёт начался.
        /// </summary>
        PotStarted = 20,

        /// <summary>
        /// Сообщение юзеру, что его транзакция дошла и он участвует в поте.
        /// </summary>
        PotTransactionAccepted = 21,

        /// <summary>
        /// Сообщение юзеру, что его транзакция дошла, но не приняла участие в поте и что он может вернуть токены по ссылке.
        /// </summary>
        PotTransactionDeclined = 22,

        /// <summary>
        /// Сообщение юзеру, что пот завершён но он не попал в список выигравших.
        /// </summary>
        PotEndedUserIsNotWinner = 31,

        /// <summary>
        /// Сообщение юзеру, что пот завершён и он в списке выигравших.
        /// </summary>
        PotEndedUserIsWinner = 32,
    }
}
