using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RestaurantMVC.Models;

namespace RestaurantMVC.Services
{
    public static class ChatStore
    {
        // conversationId -> messages
        private static readonly ConcurrentDictionary<string, List<ChatMessage>> Conversations = new();

        public static string EnsureConversation(string? conversationId = null)
        {
            var id = string.IsNullOrWhiteSpace(conversationId) ? Guid.NewGuid().ToString("N") : conversationId!;
            Conversations.TryAdd(id, new List<ChatMessage>());
            return id;
        }

        public static IReadOnlyList<ChatMessage> GetMessages(string conversationId)
        {
            if (Conversations.TryGetValue(conversationId, out var list))
                return list.OrderBy(m => m.CreatedAt).ToList();
            return Array.Empty<ChatMessage>();
        }

        public static void AddMessage(ChatMessage msg)
        {
            var list = Conversations.GetOrAdd(msg.ConversationId, _ => new List<ChatMessage>());
            lock (list)
            {
                list.Add(msg);
            }
        }

        public static IReadOnlyDictionary<string, ChatMessage?> GetLatestByConversation()
        {
            return Conversations.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.OrderByDescending(m => m.CreatedAt).FirstOrDefault()
            );
        }
    }
}