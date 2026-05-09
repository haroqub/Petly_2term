using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Petly.DataAccess.Data;
using Petly.Models;

namespace Petly.DataAccess;

public static class FullDbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        string[] roles = { "user", "shelter_admin", "system_admin" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
        }

        var adminEmail = "admin@petly.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(admin, "Admin123!");
        }

        var adminRoles = await userManager.GetRolesAsync(admin);
        await userManager.RemoveFromRolesAsync(admin, adminRoles);
        await userManager.AddToRoleAsync(admin, "system_admin");

        string[] shelterAdmins =
        {
            "happypaws@petly.com",
            "domivka@petly.com",
            "bestfriends@petly.com"
        };

        foreach (var email in shelterAdmins)
        {
            var shelterAdmin = await userManager.FindByEmailAsync(email);

            if (shelterAdmin == null)
            {
                shelterAdmin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(shelterAdmin, "Admin123!");
            }

            var rolesUser = await userManager.GetRolesAsync(shelterAdmin);
            await userManager.RemoveFromRolesAsync(shelterAdmin, rolesUser);
            await userManager.AddToRoleAsync(shelterAdmin, "shelter_admin");
        }

        string[] users =
        {
            "skorobogatuh.dara@gmail.com",
            "karpiakmarta23@gmail.com",
            "pzakutynskyi@gmail.com",
            "haroqub@gmail.com",
            "magockadiana@gmail.com"
        };

        foreach (var email in users)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, "User123!");
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            await userManager.AddToRoleAsync(user, "user");
        }

        var admin1 = await userManager.FindByEmailAsync("happypaws@petly.com");
        var admin2 = await userManager.FindByEmailAsync("domivka@petly.com");
        var admin3 = await userManager.FindByEmailAsync("bestfriends@petly.com");

        // ОЧИЩЕННЯ
        context.ShelterNeeds.RemoveRange(context.ShelterNeeds);
        context.Pets.RemoveRange(context.Pets);
        context.Shelters.RemoveRange(context.Shelters);
        await context.SaveChangesAsync();

        // ПРИТУЛКИ
        var shelter1 = new Shelter
        {
            AccountId = admin1.Id,
            ShelterName = "Happy Paws",
            Location = "Lviv",
            AdminName = "Admin"
        };

        var shelter2 = new Shelter
        {
            AccountId = admin2.Id,
            ShelterName = "Домівка",
            Location = "Kyiv",
            AdminName = "Admin"
        };

        var shelter3 = new Shelter
        {
            AccountId = admin3.Id,
            ShelterName = "Best Friends",
            Location = "Lviv",
            AdminName = "Admin"
        };

        context.Shelters.AddRange(shelter1, shelter2, shelter3);
        await context.SaveChangesAsync();

        // ТВАРИНИ
        context.Pets.AddRange(

            new Pet { ShelterId = shelter1.AccountId, PetName = "Макс", Type = "Собака", Breed = "Лабрадор", Gender = "Чоловіча", Age = 3, Size = "Великий", Vaccinated = true, Sterilized = true, Status = "Доступний", PhotoUrl = "/images/pets/max.jpg", Description = "Дуже добрий та енергійний пес, який обожнює людей і завжди радий новим знайомствам. Макс виріс у сім’ї, але через переїзд господарів опинився в притулку. Любить довгі прогулянки, гру з м’ячиком і чудово ладнає з дітьми. Стане ідеальним другом для активної сім’ї." },
            new Pet { ShelterId = shelter1.AccountId, PetName = "Луна", Type = "Кіт", Breed = "Мішана", Gender = "Жіноча", Age = 2, Size = "Малий", Vaccinated = true, Sterilized = false, Status = "Доступний", PhotoUrl = "/images/pets/luna.jpg", Description = "Ніжна та трохи сором’язлива кішечка, яка потребує часу, щоб звикнути до нових людей. Луну знайшли на вулиці ще маленькою, тому вона дуже цінує тепло і турботу. Любить спокій, м’які пледи і тихі вечори поруч із людиною." },
            new Pet { ShelterId = shelter1.AccountId, PetName = "Рокі", Type = "Собака", Breed = "Вівчарка", Gender = "Чоловіча", Age = 4, Size = "Великий", Vaccinated = true, Sterilized = true, Status = "Доступний", PhotoUrl = "/images/pets/rocky.jpg", Description = "Розумний, відданий і дуже уважний пес. Рокі добре піддається навчанню і має сильний охоронний інстинкт. Потрапив у притулок після того, як його залишили на дачі. Підійде для людей, які хочуть надійного друга і захисника." },

            new Pet { ShelterId = shelter2.AccountId, PetName = "Белла", Type = "Собака", Breed = "Родезійський ріджбек", Gender = "Жіноча", Age = 4, Size = "Середній", Vaccinated = true, Sterilized = true, Status = "Доступний", PhotoUrl = "/images/pets/bella.jpg", Description = "Активна і впевнена в собі собака з яскравим характером. Белла дуже любить рух і потребує регулярних прогулянок. Була врятована з поганих умов, але не втратила довіру до людей. Ідеально підійде для активних власників." },
            new Pet { ShelterId = shelter2.AccountId, PetName = "Чарлі", Type = "Собака", Breed = "Такса", Gender = "Чоловіча", Age = 4, Size = "Малий", Vaccinated = true, Sterilized = true, Status = "Доступний", PhotoUrl = "/images/pets/charlie.jpg", Description = "Маленький, але дуже сміливий і веселий песик. Чарлі обожнює увагу і завжди хоче бути поруч із людиною. Потрапив у притулок через алергію у попередніх власників. Ідеальний варіант для квартири." },
            new Pet { ShelterId = shelter2.AccountId, PetName = "Дейзі", Type = "Собака", Breed = "Золотистий ретривер", Gender = "Жіноча", Age = 12, Size = "Середній", Vaccinated = true, Sterilized = true, Status = "Доступний", PhotoUrl = "/images/pets/daisy.jpg", Description = "Спокійна, мудра і дуже ласкава собака. Дейзі вже в поважному віці, але все ще любить прогулянки і спілкування. Вона втратила свого господаря і тепер шукає тихий дім, де зможе провести старість у любові." },

            new Pet { ShelterId = shelter3.AccountId, PetName = "Люсі", Type = "Кіт", Breed = "Мішана", Gender = "Жіноча", Age = 4, Size = "Малий", Vaccinated = true, Sterilized = true, Status = "Доступний", PhotoUrl = "/images/pets/lucy.jpg", Description = "Лагідна і дружелюбна кішка, яка швидко знаходить спільну мову з людьми. Любить, коли її гладять, і часто муркоче. Була знайдена біля під’їзду, але одразу показала, що дуже домашня." },
            new Pet { ShelterId = shelter3.AccountId, PetName = "Олівер", Type = "Кіт", Breed = "Мішаний", Gender = "Чоловіча", Age = 8, Size = "Малий", Vaccinated = true, Sterilized = true, Status = "Доступний", PhotoUrl = "/images/pets/oliver.jpg", Description = "Спокійний і незалежний кіт, який любить спостерігати за всім навколо. Олівер не нав’язливий, але дуже цінує увагу. Добре підійде для людей, які шукають тихого компаньйона." },
            new Pet { ShelterId = shelter3.AccountId, PetName = "Сімба", Type = "Кіт", Breed = "Мішаний", Gender = "Чоловіча", Age = 5, Size = "Середній", Vaccinated = true, Sterilized = true, Status = "Доступний", PhotoUrl = "/images/pets/simba.jpg", Description = "Грайливий і допитливий кіт із яскравим характером. Любить досліджувати нові місця і гратися з іграшками. Потрапив у притулок ще кошеням і виріс тут, тому дуже чекає свій дім." },
            new Pet { ShelterId = shelter3.AccountId, PetName = "Майло", Type = "Кіт", Breed = "Мішаний", Gender = "Чоловіча", Age = 1, Size = "Малий", Vaccinated = true, Sterilized = false, Status = "Доступний", PhotoUrl = "/images/pets/milo.jpg", Description = "Молодий, енергійний і дуже дружелюбний котик. Майло обожнює гратися і швидко прив’язується до людей. Його знайшли зовсім маленьким, і тепер він готовий стати частиною люблячої сім’ї." }
        );

        // ПОТРЕБИ
        context.ShelterNeeds.AddRange(
            new ShelterNeed
            {
                ShelterId = shelter1.AccountId,
                Description = "Потрібен корм для собак",
                PaymentDetails = "Можливий банківський переказ за реквізитами: 4444 4444 4444 4444"
            },
            new ShelterNeed
            {
                ShelterId = shelter1.AccountId,
                Description = "Теплі ковдри та матраци для тварин",
                PaymentDetails = "Можливий банківський переказ за реквізитами: 4444 4444 4444 4444"
            },
            new ShelterNeed
            {
                ShelterId = shelter2.AccountId,
                Description = "Потрібні ліки та вітаміни",
                PaymentDetails = "Передача напряму або відправка новою поштою"
            },
            new ShelterNeed
            {
                ShelterId = shelter2.AccountId,
                Description = "Потрібні засоби для прибирання та дезінфекції",
                PaymentDetails = "Пожертва через сайт або можна принести у притулок або відправка новою поштою"
            },
            new ShelterNeed
            {
                ShelterId = shelter3.AccountId,
                Description = "Потрібен корм для котів",
                PaymentDetails = "Передача напряму або відправка новою поштою"
            }
        );

        await context.SaveChangesAsync();
    }
}
