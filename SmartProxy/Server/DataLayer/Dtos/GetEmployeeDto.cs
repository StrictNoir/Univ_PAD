
using DataLayer.Entities;

namespace DataLayer.Dtos
{
    public class GetEmployeeDto : Document
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal Salary { get; set; } = 0;
    }
}
