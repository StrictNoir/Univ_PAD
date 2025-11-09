using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using System.Net.Http.Json;

namespace WebUI.Controllers;

public class EmployeeController : Controller
{
    private readonly HttpClient _http;
    public EmployeeController(IHttpClientFactory factory) => _http = factory.CreateClient();

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
}