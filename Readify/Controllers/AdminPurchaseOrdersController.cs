using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Readify.Data;
using Readify.Models;
using Readify.Services;
using System.Linq;

namespace Readify.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/purchase-orders")]
    public class AdminPurchaseOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminPurchaseOrdersController> _logger;
        private readonly IAuditService _audit;
        private readonly IPurchaseOrderService _poService;

        public AdminPurchaseOrdersController(AppDbContext context, ILogger<AdminPurchaseOrdersController> logger, IAuditService audit, IPurchaseOrderService poService)
        {
            _context = context;
            _logger = logger;
            _audit = audit;
            _poService = poService;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var items = await _poService.ListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var po = await _poService.GetAsync(id);
            if (po == null) return NotFound(new { message = "Purchase order not found" });
            return Ok(po);
        }

        public class CreatePoDto { public int SupplierId { get; set; } public List<CreatePoItemDto> Items { get; set; } = new(); }
        public class CreatePoItemDto { public int ProductId { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePoDto dto)
        {
            if (dto == null || dto.SupplierId <= 0 || dto.Items == null || !dto.Items.Any()) return BadRequest(new { message = "Invalid payload" });
            var sup = await _context.Suppliers.FindAsync(dto.SupplierId);
            if (sup == null) return BadRequest(new { message = "Supplier not found" });

            try
            {
                var items = dto.Items.Where(i => i.ProductId > 0 && i.Quantity > 0)
                    .Select(i => (i.ProductId, i.Quantity, i.UnitPrice));
                var po = await _poService.CreateAsync(dto.SupplierId, items);
                try { await _audit.WriteAsync("PurchaseOrderCreated", nameof(PurchaseOrder), po.Id, $"SupplierId={po.SupplierId}; Items={po.Items.Count}"); } catch { }
                return CreatedAtAction(nameof(Get), new { id = po.Id }, po);
            }
            catch (InvalidOperationException ix)
            {
                return BadRequest(new { message = ix.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create purchase order");
                return StatusCode(500, new { message = "Failed to create purchase order" });
            }
        }

        [HttpPost("{id}/receive")]
        public async Task<IActionResult> Receive(int id)
        {
            try
            {
                var po = await _poService.ReceiveAsync(id);
                if (po == null) return NotFound();
                try { await _audit.WriteAsync("PurchaseOrderReceived", nameof(PurchaseOrder), po.Id, $"ReceivedAt={po.ReceivedAt}"); } catch { }
                return Ok(po);
            }
            catch (InvalidOperationException ix)
            {
                return BadRequest(new { message = ix.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive purchase order {Id}", id);
                return StatusCode(500, new { message = "Failed to receive purchase order" });
            }
        }

        public class PartialReceiveItemDto { public int PurchaseOrderItemId { get; set; } public int ReceivedQuantity { get; set; } }

        [HttpPost("{id}/receive-partial")]
        public async Task<IActionResult> ReceivePartial(int id, [FromBody] List<PartialReceiveItemDto> items)
        {
            if (items == null || items.Count == 0) return BadRequest(new { message = "Invalid payload" });
            try
            {
                var mapped = items.Select(i => (i.PurchaseOrderItemId, i.ReceivedQuantity));
                var po = await _poService.ReceivePartialAsync(id, mapped, receivedByUserId: null);
                if (po == null) return NotFound();
                try { await _audit.WriteAsync("PurchaseOrderPartiallyReceived", nameof(PurchaseOrder), po.Id, $"ItemsReceived={items.Count}"); } catch { }
                return Ok(po);
            }
            catch (InvalidOperationException ix)
            {
                return BadRequest(new { message = ix.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to partially receive purchase order {Id}", id);
                return StatusCode(500, new { message = "Failed to partially receive purchase order" });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                await _poService.CancelAsync(id);
                try { await _audit.WriteAsync("PurchaseOrderCancelled", nameof(PurchaseOrder), id, null); } catch { }
                return Ok(new { id });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ix)
            {
                return BadRequest(new { message = ix.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel purchase order {Id}", id);
                return StatusCode(500, new { message = "Failed to cancel purchase order" });
            }
        }
    }
}
