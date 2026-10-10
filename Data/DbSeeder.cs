using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Enums;
using PropertyManagement.Models;

namespace PropertyManagement.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        PropertyManagementDbContext context,
        UserManager<AspNetUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);

        await SeedUnitTypesAsync(context);
        await SeedPropertiesAndUnitsAsync(context);
        await SeedApplicationsAsync(context, userManager);
    }

    private static async Task SeedRolesAsync(
        RoleManager<IdentityRole> roleManager)
    {
        string[] roles =
        [
            "Applicant",
            "Property Manager"
        ];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedUsersAsync(
        UserManager<AspNetUser> userManager)
    {
        await CreateUserAsync(
            userManager,
            email: "manager@example.com",
            firstName: "John",
            lastName: "Manager",
            role: "Property Manager");

        await CreateUserAsync(
            userManager,
            email: "applicant1@example.com",
            firstName: "Jane",
            lastName: "Applicant",
            role: "Applicant");

        await CreateUserAsync(
            userManager,
            email: "applicant2@example.com",
            firstName: "Bob",
            lastName: "Applicant",
            role: "Applicant");
    }

    private static async Task CreateUserAsync(
        UserManager<AspNetUser> userManager,
        string email,
        string firstName,
        string lastName,
        string role)
    {
        var existingUser = await userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            return;
        }

        var user = new AspNetUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(
            user,
            "Password123!");

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(e => e.Description));

            throw new InvalidOperationException(
                $"Failed to create seed user {email}: {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
    }

    private static async Task SeedUnitTypesAsync(
        PropertyManagementDbContext context)
    {
        if (await context.UnitTypes.AnyAsync())
        {
            return;
        }

        var unitTypes = new List<UnitType>
        {
            new()
            {
                Name = "Apartment",
                IsActive = true
            },
            new()
            {
                Name = "Townhouse",
                IsActive = true
            },
            new()
            {
                Name = "Condo",
                IsActive = true
            },
            new()
            {
                Name = "Loft",
                IsActive = false
            }
        };

        await context.UnitTypes.AddRangeAsync(unitTypes);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPropertiesAndUnitsAsync(
        PropertyManagementDbContext context)
    {
        if (await context.Properties.AnyAsync())
        {
            return;
        }

        var unitTypes = await context.UnitTypes
            .ToListAsync();

        var propertyFaker = new Faker<Property>()
            .RuleFor(
                p => p.Name,
                f => $"{f.Address.City()} {f.PickRandom("Apartments", "Residences", "Townhomes", "Heights")}")
            .RuleFor(
                p => p.Address,
                f => f.Address.StreetAddress())
            .RuleFor(
                p => p.City,
                f => f.Address.City())
            .RuleFor(
                p => p.State,
                f => f.Address.StateAbbr())
            .RuleFor(
                p => p.ZipCode,
                f => f.Address.ZipCode());

        var properties = propertyFaker.Generate(8);

        await context.Properties.AddRangeAsync(properties);
        await context.SaveChangesAsync();

        var unitFaker = new Faker<Unit>()
            .RuleFor(
                u => u.UnitNumber,
                f => f.Random.Number(1, 999).ToString())
            .RuleFor(
                u => u.Bedrooms,
                f => f.Random.Int(0, 4))
            .RuleFor(
                u => u.MonthlyRent,
                f => f.Finance.Amount(800, 3000, 2))
            .RuleFor(
                u => u.UnitTypeId,
                f => f.PickRandom(unitTypes).Id);

        var units = new List<Unit>();

        foreach (var property in properties)
        {
            var unitCount = Random.Shared.Next(5, 11);

            for (var i = 1; i <= unitCount; i++)
            {
                var unit = unitFaker.Generate();

                unit.UnitNumber = i.ToString();
                unit.PropertyId = property.Id;

                units.Add(unit);
            }
        }

        await context.Units.AddRangeAsync(units);
        await context.SaveChangesAsync();
    }

    private static async Task SeedApplicationsAsync(
        PropertyManagementDbContext context,
        UserManager<AspNetUser> userManager)
    {
        if (await context.RentalApplications.AnyAsync())
        {
            return;
        }

        var applicants = await userManager
            .GetUsersInRoleAsync("Applicant");

        var manager = (await userManager
            .GetUsersInRoleAsync("Property Manager"))
            .First();

        var units = await context.Units
            .Include(u => u.Property)
            .ToListAsync();

        if (applicants.Count == 0 || units.Count == 0)
        {
            throw new InvalidOperationException(
                "Applicants and units must exist before applications can be seeded.");
        }

        var faker = new Faker();

        var statuses = new[]
        {
        ApplicationStatus.Draft,
        ApplicationStatus.Submitted,
        ApplicationStatus.Returned,
        ApplicationStatus.Approved,
        ApplicationStatus.Denied,
        ApplicationStatus.Withdrawn
    };

        var applications = new List<RentalApplication>();

        for (var i = 0; i < statuses.Length; i++)
        {
            var applicant = applicants[i % applicants.Count];
            var unit = units[i];

            var application = new RentalApplication
            {
                ApplicantId = applicant.Id,
                UnitId = unit.Id,
                Status = statuses[i],

                CurrentAddress = faker.Address.StreetAddress(),
                CurrentCity = faker.Address.City(),
                CurrentState = faker.Address.StateAbbr(),
                CurrentZipCode = faker.Address.ZipCode(),

                CreatedAt = DateTime.UtcNow.AddDays(-(30 - i)),
                UpdatedAt = DateTime.UtcNow.AddDays(-(10 - i))
            };

            applications.Add(application);
        }

        await context.RentalApplications.AddRangeAsync(applications);
        await context.SaveChangesAsync();

        await SeedResidencesAsync(context, applications, faker);
        await SeedStatusHistoryAsync(
            context,
            applications,
            applicants,
            manager,
            faker);

        await SeedLeasesAsync(context, applications);
    }

    private static async Task SeedResidencesAsync(
        PropertyManagementDbContext context,
        List<RentalApplication> applications,
        Faker faker)
    {
        var residences = new List<Residence>();

        foreach (var application in applications)
        {
            var residenceCount = faker.Random.Int(1, 3);

            for (var i = 0; i < residenceCount; i++)
            {
                var moveInDate = DateTime.UtcNow
                    .AddYears(-(i + 1))
                    .AddMonths(-faker.Random.Int(0, 11));

                var residence = new Residence
                {
                    RentalApplicationId = application.Id,

                    Address = faker.Address.StreetAddress(),
                    City = faker.Address.City(),
                    State = faker.Address.StateAbbr(),
                    ZipCode = faker.Address.ZipCode(),

                    LandlordName = faker.Name.FullName(),
                    LandlordPhone = faker.Phone.PhoneNumber(),

                    MoveInDate = moveInDate,
                    MoveOutDate = i == 0
                        ? null
                        : moveInDate.AddMonths(
                            faker.Random.Int(6, 24))
                };

                residences.Add(residence);
            }
        }

        await context.Residences.AddRangeAsync(residences);
        await context.SaveChangesAsync();
    }

    private static async Task SeedStatusHistoryAsync(
        PropertyManagementDbContext context,
        List<RentalApplication> applications,
        IList<AspNetUser> applicants,
        AspNetUser manager,
        Faker faker)
    {
        var history = new List<ApplicationStatusHistory>();

        foreach (var application in applications)
        {
            var applicant = applicants
                .First(a => a.Id == application.ApplicantId);

            // Initial creation
            history.Add(new ApplicationStatusHistory
            {
                RentalApplicationId = application.Id,
                ChangedByUserId = applicant.Id,
                FromStatus = application.Status,
                ToStatus = application.Status,
                ChangedAt = application.CreatedAt,
                Comment = "Application created."
            });

            // Manager review history for non-draft applications
            if (application.Status != ApplicationStatus.Draft)
            {
                var comment = application.Status switch
                {
                    ApplicationStatus.Submitted =>
                        "Application submitted for review.",

                    ApplicationStatus.Returned =>
                        "Please provide additional residence information.",

                    ApplicationStatus.Approved =>
                        "Application approved.",

                    ApplicationStatus.Denied =>
                        "Application does not meet current qualification requirements.",

                    ApplicationStatus.Withdrawn =>
                        "Application withdrawn by applicant.",

                    _ => null
                };

                history.Add(new ApplicationStatusHistory
                {
                    RentalApplicationId = application.Id,
                    ChangedByUserId =
                        application.Status == ApplicationStatus.Withdrawn
                            ? applicant.Id
                            : manager.Id,

                    FromStatus = ApplicationStatus.Submitted,
                    ToStatus = application.Status,

                    ChangedAt = application.UpdatedAt,
                    Comment = comment
                });
            }
        }

        await context.ApplicationStatusHistories.AddRangeAsync(history);
        await context.SaveChangesAsync();
    }

    private static async Task SeedLeasesAsync(
        PropertyManagementDbContext context,
        List<RentalApplication> applications)
    {
        var approvedApplications = applications
            .Where(a => a.Status == ApplicationStatus.Approved)
            .ToList();

        var leases = approvedApplications
            .Select(application => new Lease
            {
                RentalApplicationId = application.Id,
                UnitId = application.UnitId,

                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(12)
            })
            .ToList();

        await context.Leases.AddRangeAsync(leases);
        await context.SaveChangesAsync();
    }
}