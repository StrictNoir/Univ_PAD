using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using System.Net.Http.Json;

namespace WebUI.Controllers;

public class EditController : Controller
{
    private readonly HttpClient _http;
    public EditController(IHttpClientFactory factory) => _http = factory.CreateClient();

    [HttpGet]
    public IActionResult Add() => View();

    [HttpPost]
    public async Task<IActionResult> Add(Employee employee)
    {
        var res = await _http.PostAsJsonAsync("http://proxy:9000/api/employee/add", employee);
        if (res.IsSuccessStatusCode)
            return RedirectToAction("Index", "Home");

        ModelState.AddModelError("", "Error creating employee.");
        return View(employee);
    }

    // GET: /Edit/Edit?email=someone@example.com
    [HttpGet]
    public async Task<IActionResult> Edit([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest("Email is required.");

        var employee = await _http.GetFromJsonAsync<Employee>($"http://proxy:9000/api/employee/{email}");
        if (employee == null)
            return NotFound();

        return View("Edit", employee); // Edit.cshtml expects @model Employee
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Employee employee)
    {
        if (employee == null || string.IsNullOrEmpty(employee.Email))
            return BadRequest();

        var res = await _http.PutAsJsonAsync($"http://proxy:9000/api/employee/{employee.Email}", employee);
        if (res.IsSuccessStatusCode)
            return RedirectToAction("Index", "Home");

        ModelState.AddModelError("", "Error updating employee.");
        return View(employee);
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromForm] string email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest();

        var res = await _http.DeleteAsync($"http://proxy:9000/api/employee/delete/0");
        if (res.IsSuccessStatusCode)
            return RedirectToAction("Index", "Home");

        ModelState.AddModelError("", "Error deleting employee.");
        return RedirectToAction("Index", "Home");
    }
}
