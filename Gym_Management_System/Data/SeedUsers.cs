
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GymManagement.Models;

namespace GymManagement.Data
{
    public static class SeedUsers
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            await context.Database.EnsureCreatedAsync();

            string[] roles = { "Admin", "Trainer", "Receptionist", "Customer" };
            foreach (var role in roles)
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            var branches = context.GymBranches.OrderBy(b => b.BranchId).ToList();
            if (branches.Count < 5)
            {
                Console.WriteLine("❌ 请先确保你在 OnModelCreating 中添加了 5 个 GymBranch。");
                return;
            }

            // ✅ Admin
            string adminEmail = "admin@gym.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new User
                {
                    UserName = "admin",
                    Email = adminEmail,
                    Name = "Admin User",
                    JoinDate = DateTime.UtcNow,
                    DOB = new DateTime(1985, 1, 1)
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ✅ Trainers
            var trainerNames = new[] {
                "Emily Chen", "Jack Wang", "Sophia Liu", "Leo Zhang", "Olivia Huang",
                "Daniel Guo", "Mia Sun", "Jason Lin", "Emma Yu", "Lucas Ma",
                "Grace Zhao", "Henry Luo", "Lily Xu", "Eric Gao", "Chloe Tang",
                "Aaron He", "Natalie Qiu", "Victor Jin", "Ella Zhou", "Brian Deng",
                "Isabella Liang", "Owen Fan", "Hannah Zhu", "Kevin Shen", "Jasmine Bai"
            };
            var trainerSpecs = new[] { "Yoga", "Cardio", "HIIT", "Strength", "Pilates" };

            for (int i = 0; i < trainerNames.Length; i++)
            {
                var email = $"{trainerNames[i].ToLower().Replace(" ", "")}@trainer.com";
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new Trainer
                    {
                        UserName = email.Split('@')[0],
                        Email = email,
                        Name = trainerNames[i],
                        JoinDate = DateTime.UtcNow,
                        DOB = new DateTime(1980 + (i % 10), 4, (i % 28) + 1),
                        Specialization = trainerSpecs[i % 5],
                        ExperienceStarted = DateTime.UtcNow.AddYears(-2),
                        BranchId = branches[i % 5].BranchId
                    };
                    var result = await userManager.CreateAsync(user, "Trainer@123");
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(user, "Trainer");
                }
            }

            // ✅ Customers
            var customerNames = new[] {
                "Amy Zhou", "Brian Li", "Chloe Wu", "David Xu", "Ella Feng",
                "Frank Zhao", "Grace Huang", "Henry Yang", "Isla Chen", "Jake Luo",
                "Kira Zhang", "Leo Ma", "Mandy Sun", "Nathan Wang", "Olive Lin",
                "Peter Qiu", "Queenie Tang", "Ryan Guo", "Selena Gao", "Tommy Deng",
                "Una Yu", "Victor He", "Wendy Shen", "Xander Liang", "Yvonne Bai",
                "Zack Zhu", "Bella Fan", "Chris Zhao", "Daisy Zhou", "Ethan Luo",
                "Fiona Lin", "George Sun", "Helen Zhang", "Ivan Wu", "Julia Xu",
                "Kevin Yang", "Linda Qiu", "Mark Tang", "Nina Huang", "Oscar Ma",
                "Polly Chen", "Quincy Bai", "Rachel Deng", "Sam He", "Tina Yu",
                "Ulysses Gao", "Vera Shen", "Will Liang", "Xia Zhou", "Yoyo Guo"
            };

            var membershipTypes = new[] { MembershipType.Monthly, MembershipType.Quarterly, MembershipType.Yearly };
            var membershipStatuses = new[] { MembershipStatus.Active, MembershipStatus.Expired, MembershipStatus.Suspended };
            var paymentMethods = new[] { "Credit Card", "Cash", "Debit", "Online" };
            var rnd = new Random();

            for (int i = 0; i < customerNames.Length; i++)
            {
                var email = $"{customerNames[i].ToLower().Replace(" ", "")}@cust.com";
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    // 🎯 类型 + 状态
                    var type = membershipTypes[i % membershipTypes.Length];
                    var status = membershipStatuses[i % membershipStatuses.Length];

                    // 🕒 有效期逻辑
                    DateTime? expiry = status switch
                    {
                        MembershipStatus.Active => type switch
                        {
                            MembershipType.Monthly => DateTime.UtcNow.AddMonths(1),
                            MembershipType.Quarterly => DateTime.UtcNow.AddMonths(3),
                            MembershipType.Yearly => DateTime.UtcNow.AddYears(1),
                            _ => null
                        },
                        MembershipStatus.Expired => DateTime.UtcNow.AddDays(-rnd.Next(5, 30)), // 已过期日期
                        MembershipStatus.Suspended => null, // 暂停状态不设定过期
                        _ => null
                    };

                    var user = new Customer
                    {
                        UserName = email.Split('@')[0],
                        Email = email,
                        Name = customerNames[i],
                        JoinDate = DateTime.UtcNow,
                        DOB = new DateTime(1990 + (i % 15), 5, (i % 28) + 1),
                        MembershipType = type,
                        MembershipStatus = status,
                        MembershipExpiry = expiry,
                        SubscriptionDate = DateTime.UtcNow,
                        GymBranchId = branches[i % 5].BranchId
                    };

                    var result = await userManager.CreateAsync(user, "Customer@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Customer");

                        // 💳 只有 Active 和 Expired 才生成付款记录
                        if (status == MembershipStatus.Active || status == MembershipStatus.Expired)
                        {
                            decimal price = type switch
                            {
                                MembershipType.Monthly => 59.99m,
                                MembershipType.Quarterly => 149.99m,
                                MembershipType.Yearly => 499.99m,
                                _ => 0m
                            };

                            var payment = new Payment
                            {
                                CustomerId = user.Id,
                                Price = price,
                                PaymentMethod = paymentMethods[rnd.Next(paymentMethods.Length)],
                                PaymentDate = status == MembershipStatus.Expired ? DateTime.UtcNow.AddMonths(-1) : DateTime.UtcNow
                            };

                            context.Payments.Add(payment);
                        }
                    }
                }
            }



            // ✅ Receptionists
            var receptionistNames = new[] { "Sophie Desk", "Liam Front", "Emily Welcome", "Jay Counter", "Nina Greet" };

            for (int i = 0; i < 5; i++)
            {
                var email = $"{receptionistNames[i].ToLower().Replace(" ", "")}@gym.com";
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new Receptionist
                    {
                        UserName = email.Split('@')[0],
                        Email = email,
                        Name = receptionistNames[i],
                        JoinDate = DateTime.UtcNow,
                        DOB = new DateTime(1991 + i, 6, 10 + i),
                        Responsibilities = "Front desk, Booking, Customer Support",
                        Notes = i % 2 == 0 ? "Always early" : "On vacation",
                        IsAvailable = i % 2 == 0,
                        BranchId = branches[i].BranchId
                    };
                    var result = await userManager.CreateAsync(user, "Receptionist@123");
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(user, "Receptionist");
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
