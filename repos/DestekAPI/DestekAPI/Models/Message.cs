public class Message
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? TicketId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
}