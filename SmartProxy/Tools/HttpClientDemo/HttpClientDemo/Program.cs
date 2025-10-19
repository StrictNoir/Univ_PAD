using System.Net.Http.Json;

namespace HttpClientDemo
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string Server1Url = "http://localhost:8080/api/Employee";
        private const string Server2Url = "http://localhost:8081/api/Employee";
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Employee Conflict Resolution Tester ===\n");

            // Step 1: Create an employee on Server1
            Console.WriteLine("Step 1: Creating initial employee on Server1...");
            var employeeId = await CreateEmployee(Server1Url, "John", "Doe", "john.doe@example.com");

            if (string.IsNullOrEmpty(employeeId))
            {
                Console.WriteLine("Failed to create employee. Exiting.");
                return;
            }

            Console.WriteLine($" Employee created with ID: {employeeId}\n");

            // Step 2: Wait for sync
            Console.WriteLine("Step 2: Waiting 3 seconds for sync between servers...");
            await Task.Delay(3000);

            // Step 3: Verify employee exists on both servers
            Console.WriteLine("\nStep 3: Verifying employee exists on both servers...");
            var employee1 = await GetEmployee(Server1Url, employeeId);
            var employee2 = await GetEmployee(Server2Url, employeeId);

            if (employee1 != null && employee2 != null)
            {
                Console.WriteLine($" Server1: {employee1.FirstName} {employee1.LastName} (LastChanged: {employee1.LastChangedAt:HH:mm:ss.fff})");
                Console.WriteLine($" Server2: {employee2.FirstName} {employee2.LastName} (LastChanged: {employee2.LastChangedAt:HH:mm:ss.fff})");
            }
            else
            {
                Console.WriteLine(" Employee not found on one or both servers. Exiting.");
                return;
            }

            // Step 4: Concurrent updates
            Console.WriteLine("\n=== CONFLICT TEST ===");
            Console.WriteLine("Step 4: Sending concurrent updates to BOTH servers...\n");

            var task1 = UpdateEmployeeAsync(Server1Url, employeeId, "Jane", "Smith", "jane.smith@example.com", "Server1");
            var task2 = UpdateEmployeeAsync(Server2Url, employeeId, "Jack", "Johnson", "jack.johnson@example.com", "Server2");

            // Wait for both to complete
            await Task.WhenAll(task1, task2);

            // Step 5: Wait for sync and conflict resolution
            Console.WriteLine("\nStep 5: Waiting 5 seconds for conflict resolution...");
            await Task.Delay(5000);

            // Step 6: Check final state on both servers
            Console.WriteLine("\n=== FINAL STATE ===");
            var final1 = await GetEmployee(Server1Url, employeeId);
            var final2 = await GetEmployee(Server2Url, employeeId);

            if (final1 != null && final2 != null)
            {
                Console.WriteLine($"\nServer1 Final State:");
                Console.WriteLine($"  Name: {final1.FirstName} {final1.LastName}");
                Console.WriteLine($"  Email: {final1.Email}");
                Console.WriteLine($"  LastChanged: {final1.LastChangedAt:HH:mm:ss.fff}");

                Console.WriteLine($"\nServer2 Final State:");
                Console.WriteLine($"  Name: {final2.FirstName} {final2.LastName}");
                Console.WriteLine($"  Email: {final2.Email}");
                Console.WriteLine($"  LastChanged: {final2.LastChangedAt:HH:mm:ss.fff}");

                // Check consistency
                if (final1.FirstName == final2.FirstName &&
                    final1.LastName == final2.LastName &&
                    final1.Email == final2.Email)
                {
                    Console.WriteLine("\n✓ SUCCESS: Both servers have consistent data!");
                    Console.WriteLine($"  Winner: {final1.FirstName} {final1.LastName}");
                }
                else
                {
                    Console.WriteLine("\n✗ CONFLICT: Servers have inconsistent data!");
                    Console.WriteLine("  This indicates a conflict resolution problem.");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        static async Task<string?> CreateEmployee(string baseUrl, string firstName, string lastName, string email,string position="position",decimal salary=0)
        {
            try
            {
                var dto = new EmployeeInsertDto
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email
                };

                var response = await httpClient.PostAsJsonAsync($"{baseUrl}/add", dto);
                response.EnsureSuccessStatusCode();

                var id = await response.Content.ReadAsStringAsync();
                
                return id.Trim('"'); // Remove quotes from JSON string
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error creating employee: {ex.Message}");
                return null;
            }
        }

        static async Task<EmployeeGetDto?> GetEmployee(string baseUrl, string id)
        {
            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<EmployeeGetDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error getting employee: {ex.Message}");
                return null;
            }
        }

        static async Task<bool> UpdateEmployeeAsync(string baseUrl, string id, string firstName, string lastName, string email, string serverName)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                Console.WriteLine($"[{startTime:HH:mm:ss.fff}] {serverName}: Sending update to {firstName} {lastName}...");

                var dto = new EmployeeInsertDto
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email
                };

                var response = await httpClient.PutAsJsonAsync($"{baseUrl}/update/{id}", dto);
                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalMilliseconds;

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{endTime:HH:mm:ss.fff}] {serverName}: ✓ Update successful ({duration:F0}ms)");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[{endTime:HH:mm:ss.fff}] {serverName}: ✗ Update failed - {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{serverName}: ✗ Error: {ex.Message}");
                return false;
            }
        }
    }
}