﻿namespace Content
{
    internal class IdGenerator
    {
        private static int _messageId;
        private static int _chatContextId;

        public static int getMessageId()
        {
            var prevId = _messageId;
            _messageId++;
            return prevId;
        }

        public static void resetMessageId()
        {
            _messageId = 0;
        }

        public static int getChatContextId()
        {
            var prevId = _chatContextId;
            _chatContextId++;
            return prevId;
        }

        public static void resetChatContextId()
        {
            _chatContextId = 0;
        }
    }
}