using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using System.Net.Http.Json;

namespace WebUI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HttpClient _http;

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory factory)
    {
        _logger = logger;
        _http = factory.CreateClient();
    }

    public async Task<IActionResult> Index()
    {
        var employees = await _http.GetFromJsonAsync<List<Employee>>("http://proxy:9000/api/employee/all");
        return View(employees);
    }
    public IActionResult Add()
    {
        return RedirectToAction("Add", "Edit");
    }

    [HttpGet]
    public IActionResult Edit(string id)
    {
        return Redirect($"~/Edit/Edit?id={Uri.EscapeDataString(id)}");
    }
    
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
            return BadRequest();

        var res = await _http.DeleteAsync($"http://proxy:9000/api/employee/delete/{id}");
        if (res.IsSuccessStatusCode)
            return RedirectToAction("Index", "Home");

        ModelState.AddModelError("", "Error deleting employee.");
        return RedirectToAction("Index", "Home");
    }

    
    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}