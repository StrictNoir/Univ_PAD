
namespace HttpClientDemo
{
    public class EmployeeGetDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public string Position { get; set; } = string.Empty;
        public DateTime LastChangedAt { get; set; }
    }
}
