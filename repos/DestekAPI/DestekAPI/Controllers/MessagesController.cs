using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DestekAPI.Data;
using DestekAPI.Models;
using DestekAPI.Hubs;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System;

namespace DestekAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly DestekDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessagesController(DestekDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: api/Messages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessages()
        {
            return await _context.Messages.ToListAsync();
        }

        // GET: api/Messages/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Message>> GetMessage(int id)
        {
            var message = await _context.Messages.FindAsync(id);

            if (message == null)
            {
                return NotFound();
            }

            return message;
        }

        // GET: api/Messages/history?ticketId=5
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessageHistory([FromQuery] int? ticketId)
        {
            if (!ticketId.HasValue)
            {
                return BadRequest("Ticket ID gerekli");
            }

            var messages = await _context.Messages
                .Where(m => m.TicketId == ticketId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return messages;
        }

        // GET: api/Messages/direct/{senderId}/{receiverId}
        [HttpGet("direct/{senderId}/{receiverId}")]
        public async Task<ActionResult<IEnumerable<Message>>> GetDirectMessages(int senderId, int receiverId)
        {
            // İki kullanıcı arasındaki tüm mesajları getir (ticket olmadan)
            var messages = await _context.Messages
                .Where(m => m.TicketId == null &&
                           ((m.SenderId == senderId.ToString() && m.ReceiverId == receiverId.ToString()) ||
                            (m.SenderId == receiverId.ToString() && m.ReceiverId == senderId.ToString())))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return messages;
        }

        // GET: api/Messages/unread-counts/{userId}
        [HttpGet("unread-counts/{userId}")]
        public async Task<ActionResult<Dictionary<string, int>>> GetUnreadMessageCounts(int userId)
        {
            var unreadCounts = await _context.Messages
                .Where(m => m.ReceiverId == userId.ToString() && !m.IsRead && m.TicketId == null)
                .GroupBy(m => m.SenderId)
                .Select(g => new { SenderId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SenderId, x => x.Count);

            return unreadCounts;
        }

        // PUT: api/Messages/mark-read/{userId}/{senderId}
        [HttpPut("mark-read/{userId}/{senderId}")]
        public async Task<IActionResult> MarkMessagesAsRead(int userId, int senderId)
        {
            var messages = await _context.Messages
                .Where(m => m.ReceiverId == userId.ToString() &&
                           m.SenderId == senderId.ToString() &&
                           !m.IsRead &&
                           m.TicketId == null)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/Messages
        [HttpPost]
        public async Task<ActionResult<Message>> PostMessage(Message message)
        {
            if (message == null)
            {
                return BadRequest("Mesaj verisi boş olamaz");
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // SignalR gönderimi ChatHub üzerinden yapılıyor, burada tekrar göndermeye gerek yok
            Console.WriteLine($"Mesaj veritabanına kaydedildi - ID: {message.Id}, Content: {message.Content}");

            return CreatedAtAction("GetMessage", new { id = message.Id }, message);
        }

        // PUT: api/Messages/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMessage(int id, Message message)
        {
            if (id != message.Id)
            {
                return BadRequest();
            }

            _context.Entry(message).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MessageExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Messages/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MessageExists(int id)
        {
            return _context.Messages.Any(e => e.Id == id);
        }
    }
}
