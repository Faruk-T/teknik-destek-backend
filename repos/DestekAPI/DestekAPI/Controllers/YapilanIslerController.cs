using Microsoft.AspNetCore.Mvc;
using DestekAPI.Data;
using DestekAPI.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DestekAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YapilanIslerController : ControllerBase
    {
        private readonly DestekDbContext _context;
        public YapilanIslerController(DestekDbContext context)
        {
            _context = context;
        }

        [HttpGet("tum")]
        public async Task<IActionResult> GetAll()
        {
            var logs = await _context.YapilanIsler.OrderByDescending(x => x.Tarih).ToListAsync();
            return Ok(logs);
        }

        [HttpPost]
        public async Task<IActionResult> AddLog([FromBody] YapilanIs log)
        {
            log.Tarih = DateTime.Now;
            _context.YapilanIsler.Add(log);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}