using FulfillmentPlatform.Application.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FulfillmentPlatform.Infrastructure.Identity
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { Roles.TenantAdmin, Roles.Customer };

            foreach (var role in roles)
            {
                // TODO: if the role does NOT exist yet → create it
                if(!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!result.Succeeded)
                     {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Could not create role '{role}': {errors}");
                     }
                }
                // use: await roleManager.RoleExistsAsync(role)       → true/false
                // and: await roleManager.CreateAsync(new IdentityRole(role))
            }
        }
    }
}
