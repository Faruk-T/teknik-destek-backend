namespace DestekAPI.Models
{
    public class MessageDto
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }      // Yeni
        public string SenderCompany { get; set; }   // Yeni
        public string ReceiverId { get; set; }
        public string Content { get; set; }
        public int? TicketId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }
}
