using System.ComponentModel.DataAnnotations;

namespace buyselwebapi.model
{
        public class Conversation
        {
            [Key]
            
            public int id { get; set; }

            [Required]
            public int property_id { get; set; }

            [Required]
            public int buyer_id { get; set; }

            [Required]
            public int seller_id { get; set; }

            public DateTime created_at { get; set; } = DateTime.UtcNow;
        }
    public class ConversationCount
    {
        [Key]

        public int id { get; set; }

        [Required]
        public int property_id { get; set; }

        [Required]
        public int buyer_id { get; set; }

        [Required]
        public int seller_id { get; set; }

        public int? unread { get; set; }
    }
    public class Message
        {
            [Key]
           
            public int id { get; set; }

           
            public int conversation_id { get; set; }

            [Required]
            public int sender_id { get; set; }

            [Required]
            public string content { get; set; }

            public DateTime? read_at { get; set; }

            public DateTime created_at { get; set; } = DateTime.UtcNow;

            public string? bloburl { get; set; }
    }

    }
