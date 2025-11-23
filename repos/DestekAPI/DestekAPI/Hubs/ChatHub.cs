using Microsoft.AspNetCore.SignalR;
using DestekAPI.Data;
using DestekAPI.Models;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace DestekAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly DestekDbContext _context;

        public ChatHub(DestekDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userId = httpContext.Request.Query["userId"].ToString();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnConnectedAsync();
        }

        public async Task JoinTicketGroup(int ticketId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket_{ticketId}");
        }

        public async Task SendMessage(string senderId, string receiverId, string content, int? ticketId = null)
        {
            try
            {
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    TicketId = ticketId,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Gönderenin adını ve şirketini bul (int.Parse ile düzeltildi)
                int senderIdInt = int.Parse(senderId);
                var sender = _context.Kullanicilar.FirstOrDefault(k => k.Id == senderIdInt);

                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    SenderName = sender?.AdSoyad,
                    SenderCompany = sender?.SirketAdi,
                    ReceiverId = message.ReceiverId,
                    Content = message.Content,
                    TicketId = message.TicketId,
                    Timestamp = message.Timestamp,
                    IsRead = message.IsRead
                };

                if (ticketId.HasValue)
                {
                    await Clients.Group($"ticket_{ticketId}").SendAsync("ReceiveMessage", messageDto);
                }
                else
                {
                    await Clients.All.SendAsync("ReceiveMessage", messageDto);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SignalR SendMessage Hatası: " + ex.ToString());
                System.IO.File.AppendAllText("signalr_error.log", ex.ToString());
                throw;
            }
        }

        // Direkt mesaj gönder (WhatsApp benzeri)
        public async Task SendDirectMessage(string senderId, string receiverId, string content)
        {
            try
            {
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    TicketId = null, // Direkt mesaj için ticket yok
                    Timestamp = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Gönderenin adını ve şirketini bul
                int senderIdInt = int.Parse(senderId);
                var sender = _context.Kullanicilar.FirstOrDefault(k => k.Id == senderIdInt);

                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    SenderName = sender?.AdSoyad,
                    SenderCompany = sender?.SirketAdi,
                    ReceiverId = message.ReceiverId,
                    Content = message.Content,
                    TicketId = message.TicketId,
                    Timestamp = message.Timestamp,
                    IsRead = message.IsRead
                };

                // SADECE alıcıya mesajı ilet (gönderen kendi mesajını görmesin)
                await Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", messageDto);

                Console.WriteLine($"SignalR: Direkt mesaj iletildi - ID: {message.Id}, Content: {message.Content}, Receiver: {receiverId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("SignalR SendDirectMessage Hatası: " + ex.ToString());
                System.IO.File.AppendAllText("signalr_error.log", ex.ToString());
                throw;
            }
        }

        // Kullanıcı yazıyor bilgisini diğer kullanıcıya ilet
        public async Task Typing(string receiverId, string senderId)
        {
            // receiverId: Yazıyor bilgisinin iletileceği kullanıcı
            // senderId: Yazıyor bilgisini gönderen kullanıcı
            await Clients.Group($"user_{receiverId}").SendAsync("ReceiveTyping", senderId);
        }

        // Mesaj silme event'i
        public async Task DeleteMessage(int messageId, string senderId, string receiverId)
        {
            try
            {
                // Mesajı veritabanından sil
                var message = _context.Messages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    _context.Messages.Remove(message);
                    await _context.SaveChangesAsync();

                    // Hem gönderen hem de alıcıya mesaj silindi bilgisini ilet
                    await Clients.Group($"user_{senderId}").SendAsync("MessageDeleted", messageId);
                    await Clients.Group($"user_{receiverId}").SendAsync("MessageDeleted", messageId);

                    Console.WriteLine($"SignalR: Mesaj silindi - ID: {messageId}, Sender: {senderId}, Receiver: {receiverId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SignalR DeleteMessage Hatası: " + ex.ToString());
                System.IO.File.AppendAllText("signalr_error.log", ex.ToString());
                throw;
            }
        }
    }
}