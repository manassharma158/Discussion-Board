namespace Content
{
    public class SendMessageData
    {
        /// <summary>
        ///     Message string if MessageType is Chat else file path if MessageType is File
        /// </summary>
        public string Message;

        /// <summary>
        ///     List of ids for receivers, empty in case of broadcast
        /// </summary>
        public int[] ReceiverIds;

        /// <summary>
        ///     Id of the thread if message belongs to an already existing thread else -1
        /// </summary>
        public int ReplyThreadId;

        /// <summary>
        ///     Message Type - File or Chat
        /// </summary>
        public MessageType Type;
    }
}