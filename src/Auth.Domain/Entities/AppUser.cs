using Microsoft.AspNetCore.Identity;

namespace Auth.Domain.Entities
{
    public class AppUser : IdentityUser<Guid>
    {
        public Guid TenantId { get; set; }
    }
}
