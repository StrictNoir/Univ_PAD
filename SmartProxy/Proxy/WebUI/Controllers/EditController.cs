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
        var res = await _http.PostAsJsonAsync("http://178.62.201.122:8080/api/employee/add", employee);
        if (res.IsSuccessStatusCode)
            return RedirectToAction("Index", "Home");

        ModelState.AddModelError("", "Error creating employee.");
        return View(employee);
    }

    // GET: /Edit/Edit?email=someone@example.com
    [HttpGet]
    public async Task<IActionResult> Edit([FromQuery] string id)
    {
        if (string.IsNullOrEmpty(id))
            return BadRequest("Id is required.");

        var employee = await _http.GetFromJsonAsync<Employee>($"http://178.62.201.122:8080/api/employee/{id}");
        if (employee == null)
            return NotFound();

        return View("Edit", employee);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Employee employee)
    {
        if (string.IsNullOrEmpty(employee.Id))
            return BadRequest();

        var res = await _http.PutAsJsonAsync($"http://178.62.201.122:8080/api/employee/update/{employee.Id}", employee);
        if (res.IsSuccessStatusCode)
            return RedirectToAction("Index", "Home");

        ModelState.AddModelError("", "Error updating employee.");
        Console.Write(employee);
        return View(employee);
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromForm] string id)
    {
        if (string.IsNullOrEmpty(id))
            return BadRequest();

        var res = await _http.DeleteAsync($"http://178.62.201.122:8080/api/employee/delete/{id}");
        if (res.IsSuccessStatusCode)
            return RedirectToAction("Index", "Home");

        ModelState.AddModelError("", "Error deleting employee.");
        return RedirectToAction("Index", "Home");
    }
}
