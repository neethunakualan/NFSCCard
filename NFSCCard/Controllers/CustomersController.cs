using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFSCCard.DTOs.Customer;
using NFSCCard.Services;
using Dapper;
using System.Security.Claims;
using System.Linq;

namespace NFSCCard.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _svc;

        public CustomersController(ICustomerService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _svc.GetAllAsync();
            return Ok(customers);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _svc.GetByIdAsync(id);
            if (customer == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (userId != customer.UserId)
                {
                    return Forbid();
                }
            }

            return Ok(customer);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyCustomer()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var customer = await _svc.GetByUserIdAsync(userId);
            if (customer == null) return NotFound();
            return Ok(customer);
        }

        [HttpGet("debug/me")]
        public IActionResult DebugMe()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value });
            return Ok(new { IsAuthenticated = User.Identity?.IsAuthenticated ?? false, Claims = claims });
        }

        [AllowAnonymous]
        [HttpGet("/public/customers/{id:int}")]
        public async Task<IActionResult> GetPublicCustomer(int id)
        {
            var customer = await _svc.GetByIdAsync(id);
            if (customer == null) return NotFound();
            return Ok(customer);
        }

        [HttpPost]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CustomerDto dto)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CustomerId", 0);
            parameters.Add("@UserId", dbType: System.Data.DbType.Int32, value: null);
            parameters.Add("@FirstName", dto.FirstName);
            parameters.Add("@LastName", dto.LastName);
            parameters.Add("@Email", dto.Email);
            parameters.Add("@PhoneNumber", dto.PhoneNumber);
            parameters.Add("@WhatsAppNumber", dto.WhatsAppNumber);
            parameters.Add("@CompanyName", dto.CompanyName);
            parameters.Add("@JobTitle", dto.JobTitle);
            parameters.Add("@Website", dto.Website);
            parameters.Add("@Instagram", dto.Instagram);
            parameters.Add("@LinkedIn", dto.LinkedIn);
            parameters.Add("@Facebook", dto.Facebook);
            parameters.Add("@Bio", dto.Bio);
            parameters.Add("@ProfileImageUrl", dto.ProfileImageUrl);
            parameters.Add("@PasswordHash", dto.PasswordHash);

            await _svc.SaveAsync(parameters);
            return Ok(dto);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerDto dto)
        {
            var existing = await _svc.GetByIdAsync(id);
            if (existing == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (userId != existing.UserId)
                {
                    return Forbid();
                }
            }

            var parameters = new DynamicParameters();
            parameters.Add("@CustomerId", id);
            parameters.Add("@UserId", existing.UserId);
            parameters.Add("@FirstName", dto.FirstName);
            parameters.Add("@LastName", dto.LastName);
            parameters.Add("@Email", dto.Email);
            parameters.Add("@PhoneNumber", dto.PhoneNumber);
            parameters.Add("@WhatsAppNumber", dto.WhatsAppNumber);
            parameters.Add("@CompanyName", dto.CompanyName);
            parameters.Add("@JobTitle", dto.JobTitle);
            parameters.Add("@Website", dto.Website);
            parameters.Add("@Instagram", dto.Instagram);
            parameters.Add("@LinkedIn", dto.LinkedIn);
            parameters.Add("@Facebook", dto.Facebook);
            parameters.Add("@Bio", dto.Bio);
            parameters.Add("@ProfileImageUrl", dto.ProfileImageUrl);
            parameters.Add("@PasswordHash", dto.PasswordHash);

            await _svc.SaveAsync(parameters);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _svc.DeleteAsync(id);
            return NoContent();
        }
    }
}
