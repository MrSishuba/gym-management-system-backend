using av_motion_api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Security.Claims;
using System.Xml.Linq;

namespace av_motion_api.Data
{
    public class AppDbContext : IdentityDbContext<User, Role, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Attendance_List> Attendance_Lists { get; set; }
        public DbSet<Audit_Trail> Audit_Trails { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Booking_Time_Slot> Booking_Time_Slots { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Cart_Item> Cart_Items { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Contract_History> Contract_History { get; set; }
        public DbSet<Contract_Security> Contract_Securities { get; set; }
        public DbSet<Contract_Type> Contract_Types { get; set; }
        public DbSet<ContractDeletionSettings> ContractDeletionSettings { get; set; }
        public DbSet<ConsentForm> ConsentForms { get; set; }
        public DbSet<DeletionSettings> DeletionSettings { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Employee_Type> Employee_Types { get; set; }

        public DbSet<EmployeeShift> EmployeeShifts { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<Free_Trial_SignUp> Free_Trial_SignUps { get; set; }
        public DbSet<Inspection> Inspection { get; set; }
        public DbSet<Inspection_Status> Inspection_Status { get; set; }
        public DbSet<Inspection_Type> Inspection_Type { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<Lesson_Plan> Lesson_Plans { get; set; }
        public DbSet<Lesson_Plan_Workout> lesson_Plan_Workout { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Order_Line> Order_Lines { get; set; }
        public DbSet<Order_Status> Order_Status { get; set; }
        public DbSet<Outstanding_Payment> Outstanding_Payments { get; set; }
        public DbSet<OverdueSettings> OverdueSettings { get; set; }
        public DbSet<Owner> Owners { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Payment_Type> Payment_Types { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Product_Category> Product_Categories { get; set; }
        public DbSet<Product_Type> Product_Types { get; set; }
        public DbSet<Received_Supplier_Order> Received_Supplier_Orders { get; set; }
        public DbSet<Received_Supplier_Order_Line> Received_Supplier_Order_Lines { get; set; }

        public DbSet<TerminationRequest> TerminationRequests { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Reward_Member> Reward_Members { get; set; }
        public DbSet<Reward_Type> Reward_Types { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Supplier_Order> Supplier_Orders { get; set; }
        public DbSet<Supplier_Order_Line> Supplier_Order_Lines { get; set; }
        public DbSet<SurveyResponse> SurveyResponses { get; set; }
        public DbSet<Time_Slot> Time_Slots { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<User_Status> Users_Status{ get; set; }
        public DbSet<User_Type> User_Types { get; set; }
        public DbSet<VAT> VAT { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Wishlist_Item> Wishlist_Items { get; set; }
        public DbSet<Workout_Category> Workout_Category { get; set; }
        public DbSet<Workout> Workout { get; set; }  
        public DbSet<Write_Off> Write_Offs { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            //Renaming of Default asp Tables
            builder.Entity<User>().ToTable("Users");
            builder.Entity<IdentityUserRole<int>>().ToTable("User_Roles");
            builder.Entity<IdentityUserLogin<int>>().ToTable("User_Logins");
            builder.Entity<Role>().ToTable("Roles");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("Role_Claims");
            builder.Entity<IdentityUserClaim<int>>().ToTable("User_Claims");
            builder.Entity<IdentityUserToken<int>>().ToTable("Tokens");

            builder.Entity<VAT>()
            .Property(v => v.VAT_Percentage)
            .HasColumnType("decimal(18, 2)");

            builder.Entity<Order>()
           .Property(o => o.Total_Price)
           .HasColumnType("decimal(18, 2)");

            builder.Entity<Outstanding_Payment>()
           .Property(op => op.Amount_Due)
           .HasColumnType("decimal(18, 2)");


            builder.Entity<Payment>()
           .Property(pay => pay.Amount)
           .HasColumnType("decimal(18, 2)");

            builder.Entity<Product>()
            .Property(p => p.Unit_Price)
            .HasColumnType("decimal(18, 2)");

            builder.Entity<Product>()
            .Property(p => p.Purchase_Price)
            .HasColumnType("decimal(18, 2)");

            builder.Entity<Supplier_Order>()
           .Property(so => so.Total_Price)
           .HasColumnType("decimal(18, 2)");

            builder.Entity<Supplier_Order_Line>()
            .Property(sol => sol.Purchase_Price)
            .HasColumnType("decimal(18, 2)");

            builder.Entity<Supplier_Order_Line>()
            .Property(sol => sol.Unit_Price)
            .HasColumnType("decimal(18, 2)");

            builder.Entity<Discount>()
           .Property(d => d.Discount_Percentage)
           .HasColumnType("decimal(18, 2)");

            builder.Entity<Order_Line>()
           .Property(ol => ol.Unit_Price)
           .HasColumnType("decimal(18,2)");


            builder.Entity<Supplier_Order_Line>()
            .HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.Product_ID)
            .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Received_Supplier_Order_Line>()
            .HasOne(r => r.Received_Supplier_Order)
            .WithMany(r => r.Received_Supplier_Order_Lines)  // Assuming Received_Supplier_Order has a collection of Received_Supplier_Order_Lines
            .HasForeignKey(r => r.Received_Supplier_Order_ID)
            .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Received_Supplier_Order_Line>()
            .HasOne(r => r.Supplier_Order_Line)
            .WithMany()  // Assuming Supplier_Order_Line does not need a navigation property for Received_Supplier_Order_Lines
            .HasForeignKey(r => r.Supplier_Order_Line_ID)
            .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Received_Supplier_Order_Line>()
            .HasOne(r => r.Product)
            .WithMany()  // Assuming Product does not need a navigation property for Received_Supplier_Order_Lines
            .HasForeignKey(r => r.Product_ID)
            .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Outstanding_Payment>()
            .HasOne(op => op.Member)
            .WithMany()
            .HasForeignKey(op => op.Member_ID)
            .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Outstanding_Payment>()
            .HasOne(op => op.Payment)
            .WithMany()
            .HasForeignKey(op => op.Payment_ID)
            .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Booking>()
            .HasOne(b => b.Member)
            .WithMany()
            .HasForeignKey(b => b.Member_ID)
            .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Reward_Member>()
            .HasOne(rm => rm.Member)
            .WithMany()
            .HasForeignKey(rm => rm.Member_ID)
            .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Reward_Member>()
            .HasOne(rm => rm.Reward)
            .WithMany()
            .HasForeignKey(rm => rm.Reward_ID)
            .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Booking_Time_Slot>()
             .HasOne(bts => bts.Booking)
             .WithMany()
             .HasForeignKey(bts => bts.Booking_ID)
             .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Booking_Time_Slot>()
             .HasOne(bts => bts.Time_Slot)
             .WithMany()
             .HasForeignKey(bts => bts.Time_Slot_ID)
             .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Attendance_List>()
            .HasOne(b => b.Time_Slot)
            .WithMany()
            .HasForeignKey(b => b.Time_Slot_ID)
            .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Lesson_Plan_Workout>()
            .HasOne(lpw => lpw.Workout)
            .WithMany()
            .HasForeignKey(lpw => lpw.Workout_ID)
            .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Lesson_Plan_Workout>()
            .HasOne(lpw => lpw.Lesson_Plan)
            .WithMany()
            .HasForeignKey(lpw => lpw.Lesson_Plan_ID)
            .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Workout>()
           .HasOne(w => w.Workout_Category)
           .WithMany()
           .HasForeignKey(w => w.Workout_Category_ID)
           .IsRequired()
           .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(builder);

            builder.Entity<Owner>()
           .HasOne(o => o.User)
           .WithMany(u => u.Owners)
           .HasForeignKey(o => o.User_ID)
           .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Product>()
            .HasOne(p => p.Product_Category)
            .WithMany()
            .HasForeignKey(p => p.Product_Category_ID)
            .OnDelete(DeleteBehavior.NoAction);

            // Specify NO ACTION on delete for Product_Type
            builder.Entity<Product>()
            .HasOne(p => p.Product_Type)
            .WithMany()
            .HasForeignKey(p => p.Product_Type_ID)
            .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Product_Category>()
            .HasOne(c => c.Product_Type)
            .WithMany() // Assuming one type can have many categories
            .HasForeignKey(c => c.Product_Type_ID); // Specify the foreign key

            // Configure Contract_History entity
            builder.Entity<Contract_History>()
                .HasNoKey() // Remove the primary key if it exists
                .Property(ch => ch.Contract_ID) // Ensure Contract_ID is a simple property
                .IsRequired(false); // It can be nullable

            // Remove the foreign key relationship
            builder.Entity<Contract_History>()
                .HasOne(ch => ch.Contract)
                .WithMany() // No navigation property in Contract
                .HasForeignKey(ch => ch.Contract_ID)
                .OnDelete(DeleteBehavior.Restrict) // Adjust if necessary
                .IsRequired(false); // Allow null values

            builder.Entity<Contract>()
               .HasOne(c => c.Member)
               .WithMany()
               .HasForeignKey(c => c.Member_ID)
               .OnDelete(DeleteBehavior.Cascade); // Disable cascading deletes

            builder.Entity<Contract>()
                .HasOne(c => c.Contract_Type)
                .WithMany()
                .HasForeignKey(c => c.Contract_Type_ID)
                .OnDelete(DeleteBehavior.Restrict); // Disable cascading deletes

            builder.Entity<Contract>()
                .HasOne(c => c.Payment_Type)
                .WithMany()
                .HasForeignKey(c => c.Payment_Type_ID)
                .OnDelete(DeleteBehavior.Restrict); // Disable cascading deletes

            builder.Entity<Contract>()
                .HasOne(c => c.Employee)
                .WithMany()
                .HasForeignKey(c => c.Employee_ID)
                .OnDelete(DeleteBehavior.Restrict); // Disable cascading deletes

            builder.Entity<Contract>()
                .HasOne(c => c.Owner)
                .WithMany()
                .HasForeignKey(c => c.Owner_ID)
                .OnDelete(DeleteBehavior.Restrict); // Disable cascading deletes

            builder.Entity<Payment>()
                .HasOne(p => p.Contract)
                .WithMany()  // No navigation property on Contract for Payments
                .HasForeignKey(p => p.Contract_ID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure cascade delete for Payment -> Outstanding_Payment
            builder.Entity<Outstanding_Payment>()
                .HasOne(op => op.Payment)
                .WithMany()  // No navigation property on Payment for Outstanding_Payments
                .HasForeignKey(op => op.Payment_ID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuring TerminationRequest and Contract relationship
            builder.Entity<TerminationRequest>()
                .HasOne(tr => tr.Contract)
                .WithMany()
                .HasForeignKey(tr => tr.Contract_ID)
                .OnDelete(DeleteBehavior.Cascade); // Enable cascading deletes

            // Configuring TerminationRequest and Member relationship
            builder.Entity<TerminationRequest>()
                .HasOne(tr => tr.Member)
                .WithMany()
                .HasForeignKey(tr => tr.Member_ID)
                .OnDelete(DeleteBehavior.Restrict); // Enable cascading deletes


            builder.Entity<DeletionSettings>()
            .HasKey(ds => new { ds.DeletionTimeValue, ds.DeletionTimeUnit });

            builder.Entity<DeletionSettings>()
            .Property(ds => ds.DeletionTimeValue)
            .IsRequired();

            builder.Entity<DeletionSettings>()
            .Property(ds => ds.DeletionTimeUnit)
            .IsRequired()
            .HasMaxLength(50);

            builder.Entity<ContractDeletionSettings>()
                .HasKey(cds => new { cds.DeletionTimeValue, cds.DeletionTimeUnit });


            builder.Entity<ContractDeletionSettings>()
                .Property(cds => cds.DeletionTimeValue)
                .IsRequired();

            builder.Entity<ContractDeletionSettings>()
                .Property(cds => cds.DeletionTimeUnit)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<EmployeeShift>()
               .HasOne(es => es.Employee)
               .WithMany()
               .HasForeignKey(es => es.Employee_ID);

            builder.Entity<EmployeeShift>()
                .HasOne(es => es.Shift)
                .WithMany()
                .HasForeignKey(es => es.Shift_ID);

            builder.Entity<Contract_History>()
               .HasKey(ch => ch.Contract_History_ID);


            builder.Entity<Contract_History>()
                .Property(ch => ch.Contract_History_ID)
                .UseIdentityColumn();

            builder.Entity<OverdueSettings>()
            .HasKey(ds => new { ds.OverdueTimeValue, ds.OverdueTimeUnit });

            builder.Entity<OverdueSettings>()
            .Property(ds => ds.OverdueTimeValue)
            .IsRequired();

            builder.Entity<OverdueSettings>()
            .Property(ds => ds.OverdueTimeUnit)
            .IsRequired()
            .HasMaxLength(50);

            builder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.Order_ID)
            .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(builder);


            var Discounts = new Discount[]
            {
                new Discount
                {
                    Discount_ID = 1, Discount_Code = "SPR-123", Discount_Percentage = 10.00m, Discount_Date = new DateTime(2024, 8, 8), End_Date = new DateTime(2024, 8, 8).AddDays(30)
                }
            };
            builder.Entity<Discount>().HasData(Discounts);

            var Inspection_Type = new Inspection_Type[]
            {
                new Inspection_Type { Inspection_Type_ID = 1, Inspection_Type_Name= "Safety Inspection", Inspection_Type_Criteria = "Safety Inspection" },
                new Inspection_Type { Inspection_Type_ID = 2, Inspection_Type_Name= "Maintenance", Inspection_Type_Criteria = "Maintenance" },
                new Inspection_Type { Inspection_Type_ID = 3, Inspection_Type_Name= "Inventory Inspection", Inspection_Type_Criteria = "Inventory Inspection" }
            };
            builder.Entity<Inspection_Type>().HasData(Inspection_Type);

            var Inspection_Statuses = new Inspection_Status[]
            {
                new Inspection_Status { Inspection_Status_ID = 1, Inspection_Status_Description= "Pending" }
            };
            builder.Entity<Inspection_Status>().HasData(Inspection_Statuses);

            var Membership_Statuses = new Membership_Status[]
            {
                new Membership_Status { Membership_Status_ID = 1, Membership_Status_Description = "Subscribed" },
                new Membership_Status { Membership_Status_ID = 2, Membership_Status_Description = "Unsubscribed" },
                new Membership_Status { Membership_Status_ID = 3, Membership_Status_Description = "Blocked" },
                new Membership_Status { Membership_Status_ID = 4, Membership_Status_Description = "Suspended" },
                new Membership_Status { Membership_Status_ID = 5, Membership_Status_Description = "Banned" },

            };
            builder.Entity<Membership_Status>().HasData(Membership_Statuses);

      

            var Payment_Types = new Payment_Type[]
            {

                new Payment_Type { Payment_Type_ID = 1, Payment_Type_Name = "Payfast" },
                new Payment_Type { Payment_Type_ID = 2, Payment_Type_Name = "EFT" },
                new Payment_Type { Payment_Type_ID = 3, Payment_Type_Name = "Debit Order" }

            };
            builder.Entity<Payment_Type>().HasData(Payment_Types);

            var ProductTypes = new Product_Type[]
            {
                new Product_Type { Product_Type_ID = 1, Type_Name = "Clothing" },
                new Product_Type { Product_Type_ID = 2, Type_Name = "Accessories" }
            };

            builder.Entity<Product_Type>().HasData(ProductTypes);

            var ProductCategories = new Product_Category[]
            {
                new Product_Category { Product_Category_ID = 1, Category_Name = "Tops", Product_Type_ID = 1 }, // Clothing
                new Product_Category { Product_Category_ID = 2, Category_Name = "Bottoms", Product_Type_ID = 1 }, // Clothing
                new Product_Category { Product_Category_ID = 3, Category_Name = "Gear", Product_Type_ID = 2 } // Accessories
            };

            builder.Entity<Product_Category>().HasData(ProductCategories);


            var userStatus = new User_Status[]
            {
                new User_Status { User_Status_ID = 1, User_Status_Description = "Actived" },
                new User_Status { User_Status_ID = 2, User_Status_Description = "Deactivated" },
               
            };
            builder.Entity<User_Status>().HasData(userStatus);

            var userTypes = new User_Type[]
            {
                new User_Type { User_Type_ID = 1, User_Type_Name = "Owner" },
                new User_Type { User_Type_ID = 2, User_Type_Name = "Employee" },
                new User_Type { User_Type_ID = 3, User_Type_Name = "Member" }
            };
            builder.Entity<User_Type>().HasData(userTypes);

            var Contract_Types = new Contract_Type[]
            {
                new Contract_Type { Contract_Type_ID = 1, Contract_Type_Name = "3-Month Membership", Contract_Description = "Three-month gym membership contract" },
                new Contract_Type { Contract_Type_ID = 2, Contract_Type_Name = "6-Month Membership", Contract_Description = "six-month gym membership contract"},
                new Contract_Type { Contract_Type_ID = 3, Contract_Type_Name = "12-Month Membership", Contract_Description = "12-month gym membership contract" }
            };
            builder.Entity<Contract_Type>().HasData(Contract_Types);

            var shifts = new List<Shift>();

            // Default shift (placeholder)
            int defaultShiftId = 1;
            shifts.Add(new Shift
            {
                Shift_ID = defaultShiftId,
                Shift_Number = defaultShiftId,
                Start_Time = new TimeSpan(0, 0, 0),
                End_Time = new TimeSpan(0, 0, 0)
            });

            // Weekdays (Monday - Friday) shifts
            var startTime = new TimeSpan(6, 0, 0);
            var endTime = new TimeSpan(22, 0, 0);
            int shiftId = defaultShiftId + 1; // Start after the default shift ID

            while (startTime < endTime)
            {
                shifts.Add(new Shift
                {
                    Shift_ID = shiftId,
                    Shift_Number = shiftId,
                    Start_Time = startTime,
                    End_Time = startTime.Add(new TimeSpan(2, 0, 0))
                });
                startTime = startTime.Add(new TimeSpan(2, 0, 0));
                shiftId++;
            }

            // Saturday shifts
            startTime = new TimeSpan(6, 0, 0);
            endTime = new TimeSpan(20, 0, 0);

            while (startTime < endTime)
            {
                shifts.Add(new Shift
                {
                    Shift_ID = shiftId,
                    Shift_Number = shiftId,
                    Start_Time = startTime,
                    End_Time = startTime.Add(new TimeSpan(2, 0, 0))
                });
                startTime = startTime.Add(new TimeSpan(2, 0, 0));
                shiftId++;
            }

            // Sunday shifts
            startTime = new TimeSpan(6, 0, 0);
            endTime = new TimeSpan(14, 0, 0);

            while (startTime < endTime)
            {
                shifts.Add(new Shift
                {
                    Shift_ID = shiftId,
                    Shift_Number = shiftId,
                    Start_Time = startTime,
                    End_Time = startTime.Add(new TimeSpan(2, 0, 0))
                });
                startTime = startTime.Add(new TimeSpan(2, 0, 0));
                shiftId++;
            }

            builder.Entity<Shift>().HasData(shifts);

            var VAT = new VAT[]
            {
               new VAT { VAT_ID = 1, VAT_Percentage = 12.00m, VAT_Date = new DateTime(2024, 8, 8) }
            };
            builder.Entity<VAT>().HasData(VAT);

            var employeeTypes = new Employee_Type[]
            {
                new Employee_Type
                {
                    Employee_Type_ID = 1,
                    Job_Title = "Admin",
                    Job_Description = "Manages administrative tasks and oversees operations."
                },
                new Employee_Type
                {
                    Employee_Type_ID = 2,
                    Job_Title = "Trainer",
                    Job_Description = "Provides training and fitness guidance to members."
                }
            };
            builder.Entity<Employee_Type>().HasData(employeeTypes);

            var workoutcategories = new Workout_Category[]
            {
                new Workout_Category { Workout_Category_ID = 1, Workout_Category_Name = "Cardio", Workout_Category_Description = "Cardio workouts to improve endurance and burn calories." },
                new Workout_Category { Workout_Category_ID = 2, Workout_Category_Name = "Strength", Workout_Category_Description = "Strength training workouts to build muscle and increase strength." },
                new Workout_Category { Workout_Category_ID = 3, Workout_Category_Name = "Flexibility", Workout_Category_Description = "Flexibility workouts to improve range of motion and reduce injury risk." }
            };
            builder.Entity<Workout_Category>().HasData(workoutcategories);


            var workouts = new Workout[]
            {
                    new Workout
                    {
                        Workout_ID = 1,
                        Workout_Name = "Cardio Blast",
                        Workout_Description = "High-intensity cardio workout to burn calories and improve endurance.",
                        Sets = 4,
                        Reps = 10,
                        Workout_Category_ID = 1
                    },
                    new Workout
                    {
                        Workout_ID = 2,
                        Workout_Name = "Strength Training",
                        Workout_Description = "Build muscle strength and endurance.",
                        Sets = 3,
                        Reps = 12,
                        Workout_Category_ID = 2
                    },
                    new Workout
                    {
                        Workout_ID = 3,
                        Workout_Name = "Flexibility Routine",
                        Workout_Description = "Improve your flexibility with this stretching routine.",
                        Sets = 2,
                        Reps = 15,
                        Workout_Category_ID = 3
                    }
            };
            builder.Entity<Workout>().HasData(workouts);

            var Lesson_Plans = new Lesson_Plan[]
            {
                new Lesson_Plan { Lesson_Plan_ID = 1, Program_Name = "Base", Program_Description = "Base program description", }

            };
            builder.Entity<Lesson_Plan>().HasData(Lesson_Plans);


            var orderStatuses = new Order_Status[]
            {
                new Order_Status { Order_Status_ID = 1, Order_Status_Description = "Ready for Collection" },
                new Order_Status { Order_Status_ID = 2, Order_Status_Description = "Overdue for Collection" },
                new Order_Status { Order_Status_ID = 3, Order_Status_Description = "Collected" },
                new Order_Status { Order_Status_ID = 4, Order_Status_Description = "Late Collection" }
            };
            builder.Entity<Order_Status>().HasData(orderStatuses);

            var Roles = new Role[]
            {
                new Role{ Id = 1, Name = "Administrator", NormalizedName= "ADMINISTRATOR", isEditable = false},
                new Role{ Id = 2, Name = "Employee", NormalizedName= "EMPLOYEE", isEditable =true},
                new Role{ Id = 3, Name = "Member", NormalizedName= "MEMBER", isEditable =true}
            };
            builder.Entity<Role>().HasData(Roles);


            int claimId = 1;
            //Admin Claims
            //for each admin claim
            var adminClaims = new Claim[]

            {
                new Claim("Booking Manager", "Create"),
                new Claim("Booking Manager", "Read"),
                new Claim("Booking Manager", "Update"),
                new Claim("Booking Manager", "Delete"),

                new Claim("Equipment Manager", "Create"),
                new Claim("Equipment Manager", "Read"),
                new Claim("Equipment Manager", "Update"),
                new Claim("Equipment Manager", "Delete"),

                new Claim("Employee Manager", "Create"),
                new Claim("Employee Manager", "Read"),
                new Claim("Employee Manager", "Update"),
                new Claim("Employee Manager", "Delete"),


                new Claim("Inventory Manager", "Create"),
                new Claim("Inventory Manager", "Read"),
                new Claim("Inventory  Manager", "Update"),
                new Claim("Inventory Manager", "Delete"),

                new Claim("Gym Manager", "Create"),
                new Claim("Gym Manager", "Read"),
                new Claim("Gym  Manager", "Update"),
                new Claim("Gym Manager", "Delete"),
            };
            //create a refrence of it in the Role Claims table

            foreach (var claim in adminClaims) 
            {
                builder.Entity<IdentityRoleClaim<int>>().HasData(new IdentityRoleClaim<int>
                {
                    Id = claimId++,
                    RoleId = Roles[0].Id,
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });
            }

            //Employee Claims , they are admin too but just for separation        
            //for each employee claim
            var employeeClaims = new Claim[]
            {
                new Claim("Booking Manager", "Create"),
                new Claim("Booking Manager", "Read"),
                new Claim("Booking Manager", "Update"),
                new Claim("Booking Manager", "Delete"),

                new Claim("Equipment Manager", "Create"),
                new Claim("Equipment Manager", "Read"),
                new Claim("Equipment Manager", "Update"),
                new Claim("Equipment Manager", "Delete"),

                new Claim("Employee Manager", "Read"),
                new Claim("Employee Manager", "Update"),


                new Claim("Inventory Manager", "Create"),
                new Claim("Inventory Manager", "Read"),
                new Claim("Inventory  Manager", "Update"),
                new Claim("Inventory Manager", "Delete"),
            };
            //create a refrence of it in the Role Claims table
            foreach (var claim in employeeClaims)
            {
                builder.Entity<IdentityRoleClaim<int>>().HasData(new IdentityRoleClaim<int>
                {
                    Id = claimId++,
                    RoleId = Roles[1].Id,
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });
            }

            var memberClaims = new Claim[]
            {
                new Claim("Booking Interface", "Create"),
                new Claim("Booking Interface", "Read"),
                new Claim("Booking Interface", "Update"),
                new Claim("Booking Interface", "Delete"),

                new Claim("Profile", "Create"),
                new Claim("Profile", "Read"),
                new Claim("Profile", "Update"),

            };
            //create a refrence of it in the Role Claims table
            foreach (var claim in memberClaims)
            {
                builder.Entity<IdentityRoleClaim<int>>().HasData(new IdentityRoleClaim<int>
                {
                    Id = claimId++,
                    RoleId = Roles[2].Id,
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });
            }

            var Equipments = new Equipment[]
            {
                 new Equipment{ Equipment_ID = 1, Equipment_Name = "Treadmill", Equipment_Description = "A motorized device used for running or walking while staying in one place." }

            };
            builder.Entity<Equipment>().HasData(Equipments);

            var lessonPlanWorkOuts = new Lesson_Plan_Workout[]
            {
                new Lesson_Plan_Workout {Lesson_Plan_Workout_ID =1, Lesson_Plan_ID = 1, Workout_ID = 1}
            };
            builder.Entity<Lesson_Plan_Workout>().HasData(lessonPlanWorkOuts);
        }

    }
}

