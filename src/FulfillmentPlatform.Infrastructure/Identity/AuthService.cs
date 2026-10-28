using FulfillmentPlatform.Application.Auth;
using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Common.Exceptions;
using FulfillmentPlatform.Application.Tenants;
using FulfillmentPlatform.Domain.Entities;
using FulfillmentPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Infrastructure.Identity
{
    public class AuthService: IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        //private readonly IAppDbContext _db;
        private readonly AppDbContext _db;
        private readonly JwtTokenGenerator _tokenGenerator;
        public AuthService(UserManager<ApplicationUser> userManager, AppDbContext db, JwtTokenGenerator tokenGenerator)
        {
            _userManager = userManager;
            _db = db;
            _tokenGenerator = tokenGenerator;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
           
            // ── create the user object (not saved yet) ─────────────────────
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
            };

            // ── save the user: checks password rules, hashes, inserts ──────
            var result = await _userManager.CreateAsync(user, request.Password);

            // ── TODO 2: did it fail? ───────────────────────────────────────
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new BadRequestException(errors);
            }

            // ── give the new user the Customer role ────────────────────────
            await _userManager.AddToRoleAsync(user, Roles.Customer);

            // ── TODO 3: build the response ─────────────────────────────────
            return new RegisterResponse
            {
                UserId = user.Id,
                Email = request.Email
            };
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            // TODO 1: find the user by email
            var user = await _userManager.FindByEmailAsync(request.Email);

            // TODO 2: if the user is null → throw UnauthorizedException("Invalid email or password.")
            if(user is null)
            {
                throw new UnauthorizedException("Invalid email or password.");

            }

            // TODO 3: check the password → if wrong → throw the SAME exception with the SAME message
            var passcheck = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passcheck)
            {
                throw new UnauthorizedException("Invalid email or password.");
            }

            // TODO 4: load the roles and generate the token
            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiresAt) = _tokenGenerator.Generate(user, roles);

            // TODO 5: return a LoginResponse
            return new LoginResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                Email = user.Email!,
                FirstName = user.FirstName,
                Roles = roles.ToList(),
                TenantId = user.TenantId
            };
        }

        public async Task<RegisterCompanyResponse> RegisterCompanyAsync(RegisterCompanyRequest request)
        {
            // start the transaction: nothing below is permanent until CommitAsync
            await using var transaction = await _db.Database.BeginTransactionAsync();

            // ① normalise the slug: " ModeHaus " → "modehaus"
            var slug = ShopSlug.Normalize(request.Slug);

            // ② friendly check: is this shop link already taken? (the unique index is the safety net)
            if (await _db.Tenants.AnyAsync(t => t.Slug == slug))
            {
                throw new ConflictException($"The shop link '{slug}' is already taken.");
            }

            // ③ create the tenant with all its values
            var tenant = new Tenant
            {
                Name = request.CompanyName,
                Slug = slug,
                Description = request.Description,
                City = request.City,
                BrandColor = request.BrandColor
            };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();          // tenant.Id is filled now (but not yet permanent)

            // ④ create the admin user for this tenant
            var user = new ApplicationUser
            {
                UserName = request.AdminEmail,
                Email = request.AdminEmail,
                FirstName = request.AdminFirstName,
                LastName = request.AdminLastName,
                TenantId = tenant.Id
            };
            var result = await _userManager.CreateAsync(user, request.AdminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new BadRequestException(errors);
            }

            // ⑤ give the user the TenantAdmin role
            await _userManager.AddToRoleAsync(user, Roles.TenantAdmin);

            // everything worked → make it permanent
            await transaction.CommitAsync();

            return new RegisterCompanyResponse
            {
                TenantId = tenant.Id,
                Slug = tenant.Slug,
                UserId = user.Id,
                Email = request.AdminEmail
            };
        }

    }
}
