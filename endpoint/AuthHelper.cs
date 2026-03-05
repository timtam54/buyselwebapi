using System.Security.Claims;
using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;

namespace buyselwebapi.endpoint
{
    public static class AuthHelper
    {
        /// <summary>
        /// Extracts the current user's email from JWT claims.
        /// Checks common claim types used by NextAuth, OAuth, and standard JWT.
        /// </summary>
        public static string? GetCurrentUserEmail(ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.FindFirstValue("email")
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub");
        }

        /// <summary>
        /// Looks up the current user from the database using JWT claims.
        /// Returns null if the user is not found.
        /// </summary>
        public static async Task<User?> GetCurrentUser(ClaimsPrincipal principal, dbcontext db)
        {
            var email = GetCurrentUserEmail(principal);
            if (string.IsNullOrEmpty(email)) return null;
            return await db.user.FirstOrDefaultAsync(u => u.email == email);
        }

        /// <summary>
        /// Returns true if the current user has admin privileges.
        /// </summary>
        public static async Task<bool> IsAdmin(ClaimsPrincipal principal, dbcontext db)
        {
            var user = await GetCurrentUser(principal, db);
            return user?.admin == true;
        }
    }
}
