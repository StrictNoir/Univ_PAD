
namespace HttpClientDemo
{
    public class EmployeeInsertDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public string Position { get; set; } = string.Empty;
    }
}
