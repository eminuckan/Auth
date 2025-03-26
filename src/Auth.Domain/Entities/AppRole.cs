using Microsoft.AspNetCore.Identity;

namespace Auth.Domain.Entities
{
    public class AppRole : IdentityRole<Guid>
    {
        public Guid TenantId { get; set; }
    }
}
