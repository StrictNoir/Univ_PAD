using DataLayer.Dtos;
using Microsoft.AspNetCore.Mvc;
using Server.Services;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;

        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var employees = await _employeeService.GetAllAsync();
            return Ok(employees);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var employee = await _employeeService.GetByIdAsync(id);
            if (employee == null) return NotFound();

            return Ok(employee);
        }
        [HttpPost("add")]
        public async Task<IActionResult> AddAsync(UpsertEmployeeDto employee)
        {
            try
            {
                var id = await _employeeService.CreateAsync(employee);
                return CreatedAtAction(nameof(GetById),new {id},employee);
            }
            catch
            {
                return StatusCode(500, "An error occured while creating the employee.");
            }
        }
        [HttpPut("update/{id}")]
        public async Task<IActionResult> Upsert(string id,UpsertEmployeeDto employee)
        {
            try
            {
                 var isCreated = await _employeeService.UpsertAsync(employee,id);
                if (isCreated)
                    return CreatedAtAction(nameof(GetById), new { id }, employee);

                else return Ok(employee);
            }
            catch
            {
                return StatusCode(500, "Something went wrong...");
            }
        }
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _employeeService.DeleteAsync(id);
                if(result)
                    return NoContent();

                else return NotFound();
            }
            catch
            {
                return StatusCode(500, "An internal server occured...");
            }
        }

    }
}
