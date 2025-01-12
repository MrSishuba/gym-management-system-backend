using av_motion_api.Data;
using av_motion_api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using av_motion_api.Interfaces;
using av_motion_api.Models;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using SendGrid.Helpers.Mail;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Humanizer;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.InkML;

namespace av_motion_api.Services
{
    public class Repository : IRepository
    {
        private readonly AppDbContext _appDbContext;

        public Repository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }






        //Attendance List
        public async Task<ActionResult<IEnumerable<AttendanceListViewModel>>> GenerateAttendanceList(int timeSlotID)
        {

            var query2 = await (from al in _appDbContext.Attendance_Lists
                                join t in _appDbContext.Time_Slots on al.Time_Slot_ID equals t.Time_Slot_ID
                                join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                                join bts in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bts.Time_Slot_ID
                                join b in _appDbContext.Bookings on bts.Booking_ID equals b.Booking_ID
                                join m in _appDbContext.Members on b.Member_ID equals m.Member_ID
                                join u in _appDbContext.Users on m.User_ID equals u.User_ID
                                where al.Time_Slot_ID == timeSlotID
                                select new AttendanceListViewModel
                                {
                                    attendanceID = al.Attendance_ID,
                                    slotDate = t.Slot_Date,
                                    slotTime = t.Slot_Time,
                                    programName = lp.Program_Name,
                                    numberOfBookings = al.Number_Of_Bookings,
                                    member_ID = m.Member_ID,
                                    member_Name = u.Name + ' ' + u.Surname,
                                    bookingSlot_ID = al.Time_Slot_ID


                                }).ToListAsync();

            return query2;

        }


        public async Task<ActionResult<IEnumerable<AttendanceListViewModel>>> GenerateAttendanceLists()
        {

            var query2 = await (from al in _appDbContext.Attendance_Lists
                                join t in _appDbContext.Time_Slots on al.Time_Slot_ID equals t.Time_Slot_ID
                                join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                                join bts in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bts.Time_Slot_ID
                                join b in _appDbContext.Bookings on bts.Booking_ID equals b.Booking_ID
                                join m in _appDbContext.Members on b.Member_ID equals m.Member_ID
                                join u in _appDbContext.Users on m.User_ID equals u.User_ID

                                select new AttendanceListViewModel
                                {
                                    attendanceID = al.Attendance_ID,
                                    slotDate = t.Slot_Date,
                                    slotTime = t.Slot_Time,
                                    programName = lp.Program_Name,
                                    numberOfBookings = al.Number_Of_Bookings,
                                    member_ID = m.Member_ID,
                                    member_Name = u.Name,
                                    bookingSlot_ID = al.Time_Slot_ID

                                }).ToListAsync();

            return query2;

        }











        //Workout Category Code

        public async Task<ActionResult<Workout_Category>> GetWorkoutCategoryByName(string categoryName)
        {
            var workoutCategory = await (from wc in _appDbContext.Workout_Category
                                         where wc.Workout_Category_Name == categoryName
                                         select wc).FirstOrDefaultAsync();




            return workoutCategory;
        }



        //Workout Code
        public async Task<ActionResult<IEnumerable<ViewWorkoutViewModel>>> GetWorkoutCategories()
        {
            var workoutDetails = await (from w in _appDbContext.Workout
                                        join wc in _appDbContext.Workout_Category on w.Workout_Category_ID equals wc.Workout_Category_ID
                                        select new ViewWorkoutViewModel
                                        {
                                            Workout_ID = w.Workout_ID,
                                            Workout_Name = w.Workout_Name,
                                            Workout_Description = w.Workout_Description,
                                            Sets = w.Sets,
                                            Reps = w.Reps,
                                            workoutCategory = wc.Workout_Category_Name

                                        }).ToListAsync();
            return workoutDetails;
        }


        public async Task<ActionResult<IEnumerable<ViewWorkoutViewModel>>> GetWorkoutCategory(int workoutID)
        {
            var workoutDetails = await (from w in _appDbContext.Workout
                                        join wc in _appDbContext.Workout_Category on w.Workout_Category_ID equals wc.Workout_Category_ID
                                        where w.Workout_ID == workoutID
                                        select new ViewWorkoutViewModel
                                        {
                                            Workout_ID = w.Workout_ID,
                                            Workout_Name = w.Workout_Name,
                                            Workout_Description = w.Workout_Description,
                                            Sets = w.Sets,
                                            Reps = w.Reps,
                                            workoutCategory = wc.Workout_Category_Name,
                                            Workout_Category_ID = wc.Workout_Category_ID


                                        }).ToListAsync();
            return workoutDetails;
        }

        //LessonPlan Code
        public async Task<ActionResult<IEnumerable<LessonPlanViewModel>>> GetLessonPlanWithWorkouts(int lessonPlanId)
        {
            var lessonPlanDetails = await (from lp in _appDbContext.Lesson_Plans
                                           where lp.Lesson_Plan_ID == lessonPlanId
                                           join lpw in _appDbContext.lesson_Plan_Workout on lp.Lesson_Plan_ID equals lpw.Lesson_Plan_ID
                                           join w in _appDbContext.Workout on lpw.Workout_ID equals w.Workout_ID
                                           group w by new { lp.Lesson_Plan_ID, lp.Program_Name, lp.Program_Description } into g
                                           select new LessonPlanViewModel
                                           {
                                               lessonPlan_ID = g.Key.Lesson_Plan_ID,
                                               lessonPlanName = g.Key.Program_Name,
                                               program_Description = g.Key.Program_Description,
                                               workoutID = g.Select(w => w.Workout_ID).ToList(),
                                               workouts = g.Select(w => w.Workout_Name).ToList(),

                                           }).ToListAsync();

            // Now, create LessonPlanViewModel objects and assign them the workouts


            return lessonPlanDetails;
        }

        public async Task<ActionResult<IEnumerable<LessonPlanViewModel>>> GetLessonPlanWithWorkout()
        {
            var lessonPlanDetails = await (from lp in _appDbContext.Lesson_Plans
                                           join lpw in _appDbContext.lesson_Plan_Workout on lp.Lesson_Plan_ID equals lpw.Lesson_Plan_ID
                                           join w in _appDbContext.Workout on lpw.Workout_ID equals w.Workout_ID
                                           group w by new { lp.Lesson_Plan_ID, lp.Program_Name, lp.Program_Description } into g
                                           select new LessonPlanViewModel
                                           {
                                               lessonPlan_ID = g.Key.Lesson_Plan_ID,
                                               lessonPlanName = g.Key.Program_Name,
                                               program_Description = g.Key.Program_Description,
                                               workoutID = g.Select(w => w.Workout_ID).ToList(),
                                               workouts = g.Select(w => w.Workout_Name).ToList()
                                           }).ToListAsync();

            // Now, create LessonPlanViewModel objects and assign them the workouts


            return lessonPlanDetails;
        }

        //Bookings Code

        public async Task<ActionResult<IEnumerable<BookingViewModel>>> GetBookings()
        {
            var bookings = await (from b in _appDbContext.Booking_Time_Slots
                                  join t in _appDbContext.Time_Slots on b.Time_Slot_ID equals t.Time_Slot_ID
                                  join bk in _appDbContext.Bookings on b.Booking_ID equals bk.Booking_ID
                                  join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                                  select new BookingViewModel
                                  {
                                      booking_ID = bk.Booking_ID,
                                      date = t.Slot_Date,
                                      time = t.Slot_Time,
                                      lessonPlanName = lp.Program_Name,
                                      timeSlot_ID = t.Time_Slot_ID

                                  }).ToListAsync();





            return bookings;
        }

        public async Task<ActionResult<BookingViewModel>> GetBooking(int id, int memebrID)
        {
            //add the fact that it needs to select the bookings based on the memebr id as well
            var bookings = (from b in _appDbContext.Booking_Time_Slots
                            join t in _appDbContext.Time_Slots on b.Time_Slot_ID equals t.Time_Slot_ID
                            join bk in _appDbContext.Bookings on b.Booking_ID equals bk.Booking_ID
                            join m in _appDbContext.Members on bk.Member_ID equals m.Member_ID
                            join e in _appDbContext.Employees on t.Employee_ID equals e.Employee_ID
                            join u in _appDbContext.Users on e.User_ID equals u.User_ID
                            join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                            where b.Booking_ID == id
                            where m.User_ID == memebrID
                            select new BookingViewModel
                            {
                                booking_ID = b.Booking_ID,
                                date = t.Slot_Date,
                                time = t.Slot_Time,
                                lessonPlanName = lp.Program_Name,
                                timeSlot_ID = t.Time_Slot_ID,
                                instructorName = u.Name + ' ' + u.Surname
                            }).FirstOrDefault();





            return bookings;
        }

        public async Task<ActionResult<IEnumerable<BookingViewModel>>> GetMemberBookings(int memebrUserID)
        {
            //add the fact that it needs to select the bookings based on the memebr id as well
            var bookings = (from b in _appDbContext.Booking_Time_Slots
                            join t in _appDbContext.Time_Slots on b.Time_Slot_ID equals t.Time_Slot_ID
                            join bk in _appDbContext.Bookings on b.Booking_ID equals bk.Booking_ID
                            join m in _appDbContext.Members on bk.Member_ID equals m.Member_ID
                            join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                            where m.User_ID == memebrUserID
                            select new BookingViewModel
                            {
                                booking_ID = b.Booking_ID,
                                date = t.Slot_Date,
                                time = t.Slot_Time,
                                lessonPlanName = lp.Program_Name,
                                timeSlot_ID = t.Time_Slot_ID,
                                lessonPlanID = t.Lesson_Plan_ID
                                
                            }).ToList();





            return bookings;
        }
        //Timeslot code


        public async Task<ActionResult<TimeSlotCalanderViewModel>> GetTimeSlot(int id)
        {
            var query = await (from t in _appDbContext.Time_Slots
                               join e in _appDbContext.Employees on t.Employee_ID equals e.Employee_ID
                               join u in _appDbContext.Users on e.User_ID equals u.User_ID
                               join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                               join al in _appDbContext.Attendance_Lists on t.Time_Slot_ID equals al.Time_Slot_ID
                               where t.Time_Slot_ID == id
                               select new TimeSlotCalanderViewModel
                               {
                                   TimeSlotId = t.Time_Slot_ID,
                                   SlotTime = t.Slot_Time,
                                   SlotDate = t.Slot_Date,
                                   Availability = t.Availability,
                                   ProgramName = lp.Program_Name,
                                   Description = lp.Program_Description,
                                   Employee_Name = u.Name + ' ' + u.Surname,
                                   Employee_ID = t.Employee_ID,
                                   Lesson_Plan_ID = t.Lesson_Plan_ID,
                                   NumberOfBookings = al.Number_Of_Bookings
                               }).FirstOrDefaultAsync();
            return query;
        }

        public async Task<ActionResult<IEnumerable<TimeSlotCalanderViewModel>>> GetTimeSlots()
        {
            var query = await (from t in _appDbContext.Time_Slots
                               join e in _appDbContext.Employees on t.Employee_ID equals e.Employee_ID
                               join u in _appDbContext.Users on e.User_ID equals u.User_ID
                               join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                               join al in _appDbContext.Attendance_Lists on t.Time_Slot_ID equals al.Time_Slot_ID
                               select new TimeSlotCalanderViewModel
                               {
                                   TimeSlotId = t.Time_Slot_ID,
                                   SlotTime = t.Slot_Time,
                                   SlotDate = t.Slot_Date,
                                   Availability = t.Availability,
                                   ProgramName = lp.Program_Name,
                                   Description = lp.Program_Description,
                                   Employee_Name = u.Name,
                                   NumberOfBookings = al.Number_Of_Bookings
                               }).ToListAsync();
            return query;
        }

        public async Task<ActionResult<IEnumerable<TimeSlotCalanderViewModel>>> GetTimeSlotByLessonPlan(int id)
        {
            var timeSlotsToUpdate = _appDbContext.Time_Slots
                  .Where(ts => ts.Slot_Date < DateTime.Now);



            foreach (var timeSlot in timeSlotsToUpdate)
            {
                timeSlot.Availability = false;

            }

            await _appDbContext.SaveChangesAsync();

            var query = await (from t in _appDbContext.Time_Slots
                               join e in _appDbContext.Employees on t.Employee_ID equals e.Employee_ID
                               join u in _appDbContext.Users on e.User_ID equals u.User_ID
                               join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                               join al in _appDbContext.Attendance_Lists on t.Time_Slot_ID equals al.Time_Slot_ID
                               where t.Lesson_Plan_ID == id
                               select new TimeSlotCalanderViewModel
                               {
                                   TimeSlotId = t.Time_Slot_ID,
                                   SlotTime = t.Slot_Time,
                                   SlotDate = t.Slot_Date,
                                   Availability = t.Availability,
                                   ProgramName = lp.Program_Name,
                                   Description = lp.Program_Description,
                                   Employee_Name = u.Name,
                                   NumberOfBookings = al.Number_Of_Bookings
                               }).ToListAsync();
            return query;
        }

        public async Task<ActionResult<IEnumerable<TimeSlotCalanderViewModel>>> GetSlotByDate(DateTime date)
        {
            var query = await (from t in _appDbContext.Time_Slots
                               join e in _appDbContext.Employees on t.Employee_ID equals e.Employee_ID
                               join u in _appDbContext.Users on e.User_ID equals u.User_ID
                               join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                               join al in _appDbContext.Attendance_Lists on t.Time_Slot_ID equals al.Time_Slot_ID
                               where t.Slot_Date.Date == date.Date
                               select new TimeSlotCalanderViewModel
                               {
                                   TimeSlotId = t.Time_Slot_ID,
                                   SlotTime = t.Slot_Time,
                                   SlotDate = t.Slot_Date,
                                   Availability = t.Availability,
                                   ProgramName = lp.Program_Name,
                                   Description = lp.Program_Description,
                                   Employee_Name = u.Name,
                                   NumberOfBookings = al.Number_Of_Bookings
                               }).ToListAsync();
            return query;
        }



        public async Task<List<TimeSlotCalanderViewModel>> GetSlotByDateAndTime(DateTime date, int lessonPlanID)
        {
            var query = await (from t in _appDbContext.Time_Slots
                               join e in _appDbContext.Employees on t.Employee_ID equals e.Employee_ID
                               join u in _appDbContext.Users on e.User_ID equals u.User_ID
                               join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                               join al in _appDbContext.Attendance_Lists on t.Time_Slot_ID equals al.Time_Slot_ID
                               where t.Slot_Time == date && t.Lesson_Plan_ID == lessonPlanID
                               select new TimeSlotCalanderViewModel
                               {
                                   TimeSlotId = t.Time_Slot_ID,
                                   SlotTime = t.Slot_Time,
                                   SlotDate = t.Slot_Date,
                                   Availability = t.Availability,
                                   ProgramName = lp.Program_Name,
                                   Description = lp.Program_Description,
                                   Employee_Name = u.Name,
                                   NumberOfBookings = al.Number_Of_Bookings
                               }).ToListAsync();

            return query;
        }






        //Inspection code
        public async Task<ActionResult<IEnumerable<InspectionViewModel>>> GetEquipmentInspectionsDetails()
        {
            var inspectionsDetails = await (from i in _appDbContext.Inspection
                                            join e in _appDbContext.Equipment on i.Equipment_ID equals e.Equipment_ID
                                            join it in _appDbContext.Inspection_Type on i.Inspection_Type_ID equals it.Inspection_Type_ID
                                            join ins in _appDbContext.Inspection_Status on i.Inspection_Status_ID equals ins.Inspection_Status_ID
                                            // join inv in _appDbContext.Inventory on i.Inventory_ID equals inv.Inventory_ID
                                            select new InspectionViewModel
                                            {
                                                Inspection_ID = i.Inspection_ID,
                                                Inspection_Date = i.Inspection_Date,
                                                inspection_notes = i.Inspection_Notes,
                                                equipment_name = e.Equipment_Name,
                                                inspection_type_name = it.Inspection_Type_Name,
                                                inspection_status = ins.Inspection_Status_Description

                                            }).ToListAsync();


            return inspectionsDetails;

        }

        public async Task<ActionResult<IEnumerable<InspectionViewModel>>> GetEquipmentInspectionDetails(int inspectionID)
        {
            var inspectionsDetails = await (from i in _appDbContext.Inspection
                                            join e in _appDbContext.Equipment on i.Equipment_ID equals e.Equipment_ID
                                            join it in _appDbContext.Inspection_Type on i.Inspection_Type_ID equals it.Inspection_Type_ID
                                            join ins in _appDbContext.Inspection_Status on i.Inspection_Status_ID equals ins.Inspection_Status_ID
                                            where i.Inspection_ID == inspectionID
                                            select new InspectionViewModel
                                            {
                                                Inspection_ID = i.Inspection_ID,
                                                Inspection_Date = i.Inspection_Date,
                                                inspection_notes = i.Inspection_Notes,
                                                equipment_name = e.Equipment_Name,
                                                inspection_type_name = it.Inspection_Type_Name,
                                                inspection_status = ins.Inspection_Status_Description

                                            }).ToListAsync();


            return inspectionsDetails;

        }

        public async Task<ActionResult<IEnumerable<InspectionViewModel>>> GetInspectionsDetails()
        {
            var inspectionsDetails = await (from i in _appDbContext.Inspection
                                            join e in _appDbContext.Equipment on i.Equipment_ID equals e.Equipment_ID into equipmentGroup
                                            from e in equipmentGroup.DefaultIfEmpty()

                                            join it in _appDbContext.Inspection_Type on i.Inspection_Type_ID equals it.Inspection_Type_ID into inspectionTypeGroup
                                            from it in inspectionTypeGroup.DefaultIfEmpty()

                                            join ins in _appDbContext.Inspection_Status on i.Inspection_Status_ID equals ins.Inspection_Status_ID into inspectionStatusGroup
                                            from ins in inspectionStatusGroup.DefaultIfEmpty()

                                            join inv in _appDbContext.Inventory on i.Inventory_ID equals inv.Inventory_ID into inventoryGroup
                                            from inv in inventoryGroup.DefaultIfEmpty()

                                            select new InspectionViewModel
                                            {
                                                Inspection_ID = i.Inspection_ID,
                                                Inspection_Date = i.Inspection_Date,
                                                inspection_notes = i.Inspection_Notes,
                                                inventory_name = inv.Inventory_Item_Name,
                                                equipment_name = e.Equipment_Name,
                                                inventory_category = inv.Inventory_Item_Category,
                                                inspection_type_name = it.Inspection_Type_Name,
                                                inspection_status = ins.Inspection_Status_Description
                                            }).ToListAsync();


            return inspectionsDetails;

        }

        public async Task<ActionResult<IEnumerable<InspectionViewModel>>> GetInspectionDetails(int inspectionID)
        {
            var inspectionsDetails = await (from i in _appDbContext.Inspection
                                            join e in _appDbContext.Equipment on i.Equipment_ID equals e.Equipment_ID into equipmentGroup
                                            from e in equipmentGroup.DefaultIfEmpty()

                                            join it in _appDbContext.Inspection_Type on i.Inspection_Type_ID equals it.Inspection_Type_ID into inspectionTypeGroup
                                            from it in inspectionTypeGroup.DefaultIfEmpty()

                                            join ins in _appDbContext.Inspection_Status on i.Inspection_Status_ID equals ins.Inspection_Status_ID into inspectionStatusGroup
                                            from ins in inspectionStatusGroup.DefaultIfEmpty()

                                            join inv in _appDbContext.Inventory on i.Inventory_ID equals inv.Inventory_ID into inventoryGroup
                                            from inv in inventoryGroup.DefaultIfEmpty()
                                            where i.Inspection_ID == inspectionID
                                            select new InspectionViewModel
                                            {
                                                Inspection_ID = i.Inspection_ID,
                                                Inspection_Date = i.Inspection_Date,
                                                inspection_notes = i.Inspection_Notes,
                                                inventory_name = inv != null ? inv.Inventory_Item_Name : null,
                                                equipment_name = e != null ? e.Equipment_Name : null,
                                                inventory_category = inv != null ? inv.Inventory_Item_Category : null,
                                                inspection_type_name = it != null ? it.Inspection_Type_Name : null,
                                                inspection_status = ins != null ? ins.Inspection_Status_Description : null
                                            }).ToListAsync();



            return inspectionsDetails;

        }

        //Inventory
        public async Task<ActionResult<IEnumerable<InventoryViewModel>>> GetInventoryDetails()
        {

            var inventoryDetails = await (from inv in _appDbContext.Inventory
                                          join s in _appDbContext.Suppliers on inv.Supplier_ID equals s.Supplier_ID
                                          select new InventoryViewModel
                                          {
                                              inventoryID = inv.Inventory_ID,
                                              supplierName = s.Name,
                                              supplierID = s.Supplier_ID,
                                              category = inv.Inventory_Item_Category,
                                              itemName = inv.Inventory_Item_Name,
                                              quantity = inv.Inventory_Item_Quantity,
                                              photo = inv.Inventory_Item_Photo,
                                              received_supplier_order_id = inv.Received_Supplier_Order_ID

                                          }).ToListAsync();
            return inventoryDetails;
        }


        public async Task<ActionResult<IEnumerable<InventoryViewModel>>> GetInventoryItem(int id)
        {

            var inventoryDetails = await (from inv in _appDbContext.Inventory
                                          join s in _appDbContext.Suppliers on inv.Supplier_ID equals s.Supplier_ID
                                          where inv.Inventory_ID == id
                                          select new InventoryViewModel
                                          {
                                              inventoryID = inv.Inventory_ID,
                                              supplierName = s.Name,
                                              supplierID = s.Supplier_ID,
                                              category = inv.Inventory_Item_Category,
                                              itemName = inv.Inventory_Item_Name,
                                              quantity = inv.Inventory_Item_Quantity,
                                              photo = inv.Inventory_Item_Photo,
                                              received_supplier_order_id = inv.Received_Supplier_Order_ID

                                          }).ToListAsync();
            return inventoryDetails;
        }


            
        //Writeoffs

        public async Task<ActionResult<IEnumerable<WriteoffViewModel>>> GetWriteOffs()
        {
            var writeOffs = await (from writeOff in _appDbContext.Write_Offs
                                   join inv in _appDbContext.Inventory on writeOff.Inventory_ID equals inv.Inventory_ID
                                   select new WriteoffViewModel
                                   {
                                       Write_Off_ID = writeOff.Write_Off_ID,
                                       Date = writeOff.Date,
                                       Write_Off_Reason = writeOff.Write_Off_Reason,
                                       Inventory_ID = inv.Inventory_ID,
                                       Inventory_Item_Name = inv.Inventory_Item_Name,
                                       Write_Off_Quantity = writeOff.Write_Off_Quantity

                                   }).ToListAsync();



            return writeOffs;
        }

        public async Task<ActionResult<IEnumerable<WriteoffViewModel>>> GetWriteOff(int id)
        {
            var writeOffs = await (from writeOff in _appDbContext.Write_Offs
                                   join inv in _appDbContext.Inventory on writeOff.Inventory_ID equals inv.Inventory_ID
                                   where writeOff.Write_Off_ID == id
                                   select new WriteoffViewModel
                                   {
                                       Write_Off_ID = writeOff.Write_Off_ID,
                                       Date = writeOff.Date,
                                       Write_Off_Reason = writeOff.Write_Off_Reason,
                                       Inventory_ID = inv.Inventory_ID,
                                       Inventory_Item_Name = inv.Inventory_Item_Name,
                                       Write_Off_Quantity = writeOff.Write_Off_Quantity
                                   }).ToListAsync();



            return writeOffs;
        }




        //Reporting

        public async Task<ActionResult<string>> GetReportGenerator(int userid)
        {

            IQueryable<string> generatorName = null;
            generatorName = from usr in _appDbContext.Users
                            where usr.User_ID == userid
                            select usr.Name + ' ' + usr.Surname;


            //if (generatorName == null)
            //{
            //    return NotFound("User not found");
            //}

            return generatorName.FirstOrDefault();
        }


        public async Task<ActionResult<IEnumerable<BookingsReportViewModel>>> MostPopularLessonPlans(string dateThreshold)
        {
            IQueryable<BookingsReportViewModel> mostPopularLessonPlans = null;

            if (dateThreshold.Equals("One Month"))
            {
                mostPopularLessonPlans = from t in _appDbContext.Time_Slots
                                         join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
                                         join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
                                         join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                                         where t.Slot_Date.Month == DateTime.Now.Month  && t.Slot_Date.Year == DateTime.Now.Year
                                         group new { lp.Program_Name, b.Booking_ID } by lp.Program_Name into g
                                         select new BookingsReportViewModel
                                         {
                                             Program_Name = g.Key,
                                             No_Of_Bookings = g.Count()
                                         };
            }
            else if (dateThreshold.Equals("Three Months"))
            {
                mostPopularLessonPlans = from t in _appDbContext.Time_Slots
                                         join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
                                         join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
                                         join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                                         where t.Slot_Date.Month >= DateTime.Now.Month - 3 && t.Slot_Date.Month <= DateTime.Now.Month && t.Slot_Date.Year == DateTime.Now.Year
                                         group new { lp.Program_Name, b.Booking_ID } by lp.Program_Name into g
                                         select new BookingsReportViewModel
                                         {
                                             Program_Name = g.Key,
                                             No_Of_Bookings = g.Count()
                                         };
            }
            else if (dateThreshold.Equals("Six Months"))
            {
                mostPopularLessonPlans = from t in _appDbContext.Time_Slots
                                         join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
                                         join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
                                         join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                                         where t.Slot_Date.Month >= DateTime.Now.Month - 6 && t.Slot_Date.Month <= DateTime.Now.Month && t.Slot_Date.Year == DateTime.Now.Year
                                         group new { lp.Program_Name, b.Booking_ID } by lp.Program_Name into g
                                         select new BookingsReportViewModel
                                         {
                                             Program_Name = g.Key,
                                             No_Of_Bookings = g.Count()
                                         };
            }
            else if (dateThreshold.Equals("Year"))
            {
                mostPopularLessonPlans = from t in _appDbContext.Time_Slots
                                         join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
                                         join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
                                         join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                                         where t.Slot_Date.Month >= DateTime.Now.Month - 12 && t.Slot_Date.Month <= DateTime.Now.Month && t.Slot_Date.Year == DateTime.Now.Year
                                         group new { lp.Program_Name, b.Booking_ID } by lp.Program_Name into g
                                         select new BookingsReportViewModel
                                         {
                                             Program_Name = g.Key,
                                             No_Of_Bookings = g.Count()
                                         };
            }

            var result = await mostPopularLessonPlans.ToListAsync();
            return result;
        }

        public async Task<ActionResult<int>> TotalBookings(string dateThreshold)
        {
            IQueryable<int> totalBookings = null;

            if (dateThreshold.Equals("One Month"))
            {
                totalBookings = from t in _appDbContext.Time_Slots
                                join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
                                join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
                                join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                                where t.Slot_Date.Month == DateTime.Now.Month && t.Slot_Date.Year == DateTime.Now.Year
                                select b.Booking_ID;
            }
            else if (dateThreshold.Equals("Three Months"))
            {
                totalBookings = from t in _appDbContext.Time_Slots
                                join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
                                join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
                                join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                                where t.Slot_Date.Month >= DateTime.Now.Month - 3 && t.Slot_Date.Month <= DateTime.Now.Month && t.Slot_Date.Year == DateTime.Now.Year
                                select b.Booking_ID;
            }
            else if (dateThreshold.Equals("Six Months"))
            {
                totalBookings = from t in _appDbContext.Time_Slots
                                join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
                                join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
                                join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                                where t.Slot_Date.Month >= DateTime.Now.Month - 6 && t.Slot_Date.Month <= DateTime.Now.Month && t.Slot_Date.Year == DateTime.Now.Year
                                select b.Booking_ID;
            }
            else if (dateThreshold.Equals("Year"))
            {
                totalBookings = from t in _appDbContext.Time_Slots
                                join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
                                join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
                                join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
                                where t.Slot_Date.Month >= DateTime.Now.Month - 12 && t.Slot_Date.Month <= DateTime.Now.Month && t.Slot_Date.Year == DateTime.Now.Year
                                select b.Booking_ID;
            }

            var result = await totalBookings.CountAsync();
            return result;
        }


        //public async Task<ActionResult<IEnumerable<BookingsReportViewModel>>> PopularBookingTime(string dateThreshold)
        //{
        //    IQueryable<BookingsReportViewModel> mostPopularBookingTime = null;

        //    if (dateThreshold.Equals("One Month"))
        //    {
        //        mostPopularBookingTime = from t in _appDbContext.Time_Slots
        //                                 join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
        //                                 join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
        //                                 join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
        //                                 where t.Slot_Date.Month >= DateTime.Now.Month - 1 && t.Slot_Date.Month <= DateTime.Now.Month
        //                                 group new { t.Slot_Time, b.Booking_ID } by t.Slot_Time into g
        //                                 select new BookingsReportViewModel
        //                                 {
        //                                     Time = g.Key.ToString("HH:mm:ss"),
        //                                     No_Of_Bookings = g.Count()
        //                                 };

        //    }
        //    else if (dateThreshold.Equals("Three Months"))
        //    {
        //        mostPopularBookingTime = from t in _appDbContext.Time_Slots
        //                                 join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
        //                                 join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
        //                                 join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
        //                                 where t.Slot_Date.Month >= DateTime.Now.Month - 3 && t.Slot_Date.Month <= DateTime.Now.Month
        //                                 group new { t.Slot_Time, b.Booking_ID } by t.Slot_Time into g
        //                                 select new BookingsReportViewModel
        //                                 {
        //                                     Time = g.Key.ToString("HH:mm:ss"),
        //                                     No_Of_Bookings = g.Count()
        //                                 };
        //    }
        //    else if (dateThreshold.Equals("Six Months"))
        //    {
        //        mostPopularBookingTime = from t in _appDbContext.Time_Slots
        //                                 join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
        //                                 join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
        //                                 join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
        //                                 where t.Slot_Date.Month >= DateTime.Now.Month - 6 && t.Slot_Date.Month <= DateTime.Now.Month
        //                                 group new { t.Slot_Time, b.Booking_ID } by t.Slot_Time into g
        //                                 select new BookingsReportViewModel
        //                                 {
        //                                     Time = g.Key.ToString("HH:mm:ss"),
        //                                     No_Of_Bookings = g.Count()
        //                                 };
        //    }
        //    else if (dateThreshold.Equals("Year"))
        //    {
        //        mostPopularBookingTime = from t in _appDbContext.Time_Slots
        //                                 join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
        //                                 join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
        //                                 join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
        //                                 where t.Slot_Date.Month >= DateTime.Now.Month - 12 && t.Slot_Date.Month <= DateTime.Now.Month
        //                                 group new { t.Slot_Time, b.Booking_ID } by t.Slot_Time into g
        //                                 select new BookingsReportViewModel
        //                                 {
        //                                     Time = g.Key.ToString("HH:mm:ss"),
        //                                     No_Of_Bookings = g.Count()
        //                                 };
        //    }


        //    var result = await mostPopularBookingTime.ToListAsync();
        //    return result;
        //}

        //public async Task<ActionResult<IEnumerable<BookingsReportViewModel>>> DateWithTheMostBookings(string dateThreshold)
        //{
        //    IQueryable<BookingsReportViewModel> dateWithMostBookings = null;

        //    if (dateThreshold.Equals("One Month"))
        //    {
        //        dateWithMostBookings = from t in _appDbContext.Time_Slots
        //                               join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
        //                               join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
        //                               join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
        //                               where t.Slot_Date.Month >= DateTime.Now.Month - 1 && t.Slot_Date.Month <= DateTime.Now.Month
        //                               group new { t.Slot_Date, b.Booking_ID } by t.Slot_Date into g
        //                               select new BookingsReportViewModel
        //                               {
        //                                   Date = g.Key.Date,
        //                                   No_Of_Bookings = g.Count()
        //                               };

        //    }
        //    else if (dateThreshold.Equals("Three Months"))
        //    {
        //        dateWithMostBookings = from t in _appDbContext.Time_Slots
        //                               join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
        //                               join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
        //                               join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
        //                               where t.Slot_Date.Month >= DateTime.Now.Month - 3 && t.Slot_Date.Month <= DateTime.Now.Month
        //                               group new { t.Slot_Date, b.Booking_ID } by t.Slot_Date into g
        //                               select new BookingsReportViewModel
        //                               {
        //                                   Date = g.Key.Date,
        //                                   No_Of_Bookings = g.Count()
        //                               };
        //    }
        //    else if (dateThreshold.Equals("Six Months"))
        //    {
        //        dateWithMostBookings = from t in _appDbContext.Time_Slots
        //                               join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
        //                               join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
        //                               join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
        //                               where t.Slot_Date.Month >= DateTime.Now.Month - 6 && t.Slot_Date.Month <= DateTime.Now.Month
        //                               group new { t.Slot_Date, b.Booking_ID } by t.Slot_Date into g
        //                               select new BookingsReportViewModel
        //                               {
        //                                   Date = g.Key.Date,
        //                                   No_Of_Bookings = g.Count()
        //                               };
        //    }
        //    else if (dateThreshold.Equals("Year"))
        //    {
        //        dateWithMostBookings = from t in _appDbContext.Time_Slots
        //                               join bs in _appDbContext.Booking_Time_Slots on t.Time_Slot_ID equals bs.Time_Slot_ID
        //                               join b in _appDbContext.Bookings on bs.Booking_ID equals b.Booking_ID
        //                               join lp in _appDbContext.Lesson_Plans on t.Lesson_Plan_ID equals lp.Lesson_Plan_ID
        //                               where t.Slot_Date.Month >= DateTime.Now.Month - 12 && t.Slot_Date.Month <= DateTime.Now.Month
        //                               group new { t.Slot_Date, b.Booking_ID } by t.Slot_Date into g
        //                               select new BookingsReportViewModel
        //                               {
        //                                   Date = g.Key.Date,
        //                                   No_Of_Bookings = g.Count()
        //                               };
        //    }


        //    var result = await dateWithMostBookings.ToListAsync();
        //    return result;
        //}

        public async Task<ActionResult<int>> TotalOrders(string dateThreshold)
        {
            IQueryable<int> totalOrders = null;

            if (dateThreshold.Equals("One Month"))
            {
                totalOrders = from odr in _appDbContext.Orders
                              where odr.Order_Date.Month == DateTime.Now.Month && odr.Order_Date.Year == DateTime.Now.Year
                              select odr.Order_ID;

            }
            else if (dateThreshold.Equals("Three Months"))
            {
                totalOrders = from odr in _appDbContext.Orders
                              where odr.Order_Date.Month >= DateTime.Now.Month - 3 && odr.Order_Date.Month <= DateTime.Now.Month && odr.Order_Date.Year == DateTime.Now.Year
                              select odr.Order_ID;
            }
            else if (dateThreshold.Equals("Six Months"))
            {
                totalOrders = from odr in _appDbContext.Orders
                              where odr.Order_Date.Month >= DateTime.Now.Month - 6 && odr.Order_Date.Month <= DateTime.Now.Month && odr.Order_Date.Year == DateTime.Now.Year
                              select odr.Order_ID;
            }
            else if (dateThreshold.Equals("Year"))
            {
                totalOrders = from odr in _appDbContext.Orders
                              where odr.Order_Date.Month >= DateTime.Now.Month - 12 && odr.Order_Date.Month <= DateTime.Now.Month && odr.Order_Date.Year == DateTime.Now.Year
                              select odr.Order_ID;
            }

            var totalOrdersCount = await totalOrders.CountAsync();

            return totalOrdersCount;
        }

        public async Task<ActionResult<IEnumerable<OrderReportViewModel>>> ProductsPurchased(string dateThreshold)
        {
            IQueryable<OrderReportViewModel> productsPurchased = null;

            if (dateThreshold.Equals("One Month"))
            {
                productsPurchased = from ol in _appDbContext.Order_Lines
                                    join p in _appDbContext.Products on ol.Product_ID equals p.Product_ID
                                    join odr in _appDbContext.Orders on ol.Order_ID equals odr.Order_ID
                                    where odr.Order_Date.Month == DateTime.Now.Month && odr.Order_Date.Year == DateTime.Now.Year
                                    group new { p.Product_Name, ol.Product_ID, odr.Order_Date } by p.Product_Name into g
                                    select new OrderReportViewModel
                                    {

                                        Product_Name = g.Key,
                                        Total_Products_Purchased = g.Count(),
                                        
                                        
                                    };
            }
            else if (dateThreshold.Equals("Three Months"))
            {
                productsPurchased = from ol in _appDbContext.Order_Lines
                                    join p in _appDbContext.Products on ol.Product_ID equals p.Product_ID
                                    join odr in _appDbContext.Orders on ol.Order_ID equals odr.Order_ID
                                    where odr.Order_Date.Month >= DateTime.Now.Month - 3 && odr.Order_Date.Month <= DateTime.Now.Month && odr.Order_Date.Year == DateTime.Now.Year
                                    group new { p.Product_Name, ol.Product_ID } by p.Product_Name into g
                                    select new OrderReportViewModel
                                    {
                                        Product_Name = g.Key,
                                        Total_Products_Purchased = g.Count()
                                    };
            }
            else if (dateThreshold.Equals("Six Months"))
            {
                productsPurchased = from ol in _appDbContext.Order_Lines
                                    join p in _appDbContext.Products on ol.Product_ID equals p.Product_ID
                                    join odr in _appDbContext.Orders on ol.Order_ID equals odr.Order_ID
                                    where odr.Order_Date.Month >= DateTime.Now.Month - 6 && odr.Order_Date.Month <= DateTime.Now.Month && odr.Order_Date.Year == DateTime.Now.Year
                                    group new { p.Product_Name, ol.Product_ID } by p.Product_Name into g
                                    select new OrderReportViewModel
                                    {
                                        Product_Name = g.Key,
                                        Total_Products_Purchased = g.Count()
                                    };
            }
            else if (dateThreshold.Equals("Year"))
            {
                productsPurchased = from ol in _appDbContext.Order_Lines
                                    join p in _appDbContext.Products on ol.Product_ID equals p.Product_ID
                                    join odr in _appDbContext.Orders on ol.Order_ID equals odr.Order_ID
                                    where odr.Order_Date.Month >= DateTime.Now.Month - 12 && odr.Order_Date.Month <= DateTime.Now.Month && odr.Order_Date.Year == DateTime.Now.Year
                                    group new { p.Product_Name, ol.Product_ID } by p.Product_Name into g
                                    select new OrderReportViewModel
                                    {
                                        Product_Name = g.Key,
                                        Total_Products_Purchased = g.Count()
                                    };
            }

            var productsPurchasedList = await productsPurchased.ToListAsync();

            return productsPurchasedList;
        }

        public async Task<ActionResult<int>> TotalOrdersPlaced(string dateThreshold)
        {
            IQueryable<int> totalOrdersPlaced = null;

            if (dateThreshold.Equals("One Month"))
            {
                totalOrdersPlaced = from odr in _appDbContext.Orders
                                    where  odr.Order_Date.Month == DateTime.Now.Month && odr.Order_Date.Year == DateTime.Now.Year
                                    select odr.Order_ID;
            }
            else if (dateThreshold.Equals("Three Months"))
            {
                totalOrdersPlaced = from odr in _appDbContext.Orders
                                    where odr.Order_Date.Month >= DateTime.Now.Month - 3 && odr.Order_Date.Month <= DateTime.Now.Month && odr.Order_Date.Year == DateTime.Now.Year
                                    select odr.Order_ID;
            }
            else if (dateThreshold.Equals("Six Months"))
            {
                totalOrdersPlaced = from odr in _appDbContext.Orders
                                    where odr.Order_Date.Month >= DateTime.Now.Month - 6 && odr.Order_Date.Month <= DateTime.Now.Month && odr.Order_Date.Year == DateTime.Now.Year
                                    select odr.Order_ID;
            }
            else if (dateThreshold.Equals("Year"))
            {
                totalOrdersPlaced = from odr in _appDbContext.Orders
                                    where odr.Order_Date.Month >= DateTime.Now.Month - 12 && odr.Order_Date.Month <= DateTime.Now.Month && odr.Order_Date.Year == DateTime.Now.Year
                                    select odr.Order_ID;
            }

            var totalOrdersPlacedCount = await totalOrdersPlaced.CountAsync();

            return totalOrdersPlacedCount;
        }

        public async Task<List<OrderReportViewModel>> GetOrderSalesByProductAndCategory(string dateThreshold)
        {
            IQueryable<OrderReportViewModel> orders = null;

            if (dateThreshold.Equals("One Month"))
            {
                orders = from ol in _appDbContext.Order_Lines
                         join o in _appDbContext.Orders on ol.Order_ID equals o.Order_ID
                         join p in _appDbContext.Products on ol.Product_ID equals p.Product_ID
                         join pc in _appDbContext.Product_Categories on p.Product_Category_ID equals pc.Product_Category_ID
                         join m in _appDbContext.Members on o.Member_ID equals m.Member_ID
                         join u in _appDbContext.Users on m.User_ID equals u.User_ID
                         where o.Order_Date.Month == DateTime.Now.Month && o.Order_Date.Year == DateTime.Now.Year
                         group new { ol, p, o } by new { pc.Category_Name, p.Product_Name, p.Unit_Price, o.Order_Date } into g
                         select new OrderReportViewModel
                         {
                             Category_Name = g.Key.Category_Name,
                             Product_Name = g.Key.Product_Name,
                             Total_Ordered = g.Sum(x => x.ol.Quantity),
                             Total_Sales = g.Sum(x => x.p.Unit_Price * x.ol.Quantity),
                             Order_Date = g.Key.Order_Date.Month


                         };
            }
            else if (dateThreshold.Equals("Three Months"))
            {
                orders = from ol in _appDbContext.Order_Lines
                         join o in _appDbContext.Orders on ol.Order_ID equals o.Order_ID
                         join p in _appDbContext.Products on ol.Product_ID equals p.Product_ID
                         join pc in _appDbContext.Product_Categories on p.Product_Category_ID equals pc.Product_Category_ID
                         where o.Order_Date.Month >= DateTime.Now.Month - 3 && o.Order_Date.Month <= DateTime.Now.Month && o.Order_Date.Year == DateTime.Now.Year
                         group new { ol, p, o } by new { pc.Category_Name, p.Product_Name, p.Unit_Price, o.Order_Date } into g
                         select new OrderReportViewModel
                         {
                             Category_Name = g.Key.Category_Name,
                             Product_Name = g.Key.Product_Name,
                             Total_Ordered = g.Sum(x => x.ol.Quantity),
                             Total_Sales = g.Sum(x => x.p.Unit_Price * x.ol.Quantity),
                             Order_Date = g.Key.Order_Date.Month
                         };
            }
            else if (dateThreshold.Equals("Six Months"))
            {
                orders = from ol in _appDbContext.Order_Lines
                         join o in _appDbContext.Orders on ol.Order_ID equals o.Order_ID
                         join p in _appDbContext.Products on ol.Product_ID equals p.Product_ID
                         join pc in _appDbContext.Product_Categories on p.Product_Category_ID equals pc.Product_Category_ID
                         where o.Order_Date.Month >= DateTime.Now.Month - 6 && o.Order_Date.Month <= DateTime.Now.Month && o.Order_Date.Year == DateTime.Now.Year
                         group new { ol, p, o } by new { pc.Category_Name, p.Product_Name, p.Unit_Price, o.Order_Date } into g
                         select new OrderReportViewModel
                         {
                             Category_Name = g.Key.Category_Name,
                             Product_Name = g.Key.Product_Name,
                             Total_Ordered = g.Sum(x => x.ol.Quantity),
                             Total_Sales = g.Sum(x => x.p.Unit_Price * x.ol.Quantity),
                             Order_Date = g.Key.Order_Date.Month
                         };
            }
            else if (dateThreshold.Equals("Year"))
            {
                orders = from ol in _appDbContext.Order_Lines
                         join o in _appDbContext.Orders on ol.Order_ID equals o.Order_ID
                         join p in _appDbContext.Products on ol.Product_ID equals p.Product_ID
                         join pc in _appDbContext.Product_Categories on p.Product_Category_ID equals pc.Product_Category_ID
                         where o.Order_Date.Month >= DateTime.Now.Month - 12 && o.Order_Date.Month <= DateTime.Now.Month && o.Order_Date.Year == DateTime.Now.Year
                         group new { ol, p, o } by new { pc.Category_Name, p.Product_Name, p.Unit_Price, o.Order_Date } into g
                         select new OrderReportViewModel
                         {
                             Category_Name = g.Key.Category_Name,
                             Product_Name = g.Key.Product_Name,
                             Total_Ordered = g.Sum(x => x.ol.Quantity),
                             Total_Sales = g.Sum(x => x.p.Unit_Price * x.ol.Quantity),
                             Order_Date = g.Key.Order_Date.Month
                         };
            }

            return await orders.ToListAsync();
        }





        //Member
        public async Task<int> GetNewSubscriptions(string dateThreshold)
        {
            IQueryable<Contract> newSubscriptions = null;

            DateTime startDate;

            if (dateThreshold.Equals("One Month"))
            {
                startDate = DateTime.Now.AddMonths(-1);
            }
            else if (dateThreshold.Equals("Three Months"))
            {
                startDate = DateTime.Now.AddMonths(-3);
            }
            else if (dateThreshold.Equals("Six Months"))
            {
                startDate = DateTime.Now.AddMonths(-6);
            }
            else if (dateThreshold.Equals("Year"))
            {
                startDate = DateTime.Now.AddYears(-1);
            }
            else
            {
                throw new ArgumentException("Invalid date threshold.");
            }

            newSubscriptions = from c in _appDbContext.Contracts
                               where c.Subscription_Date >= startDate
                                     && c.Subscription_Date <= DateTime.Now
                                     && c.Approval_Status == true
                               select c;

            var totalNewSubscriptions = await newSubscriptions.CountAsync();

            return totalNewSubscriptions;
        }


        public async Task<ActionResult<IEnumerable<MemberBookingsViewModel>>> GetMemberBookings(string dateThreshold)
        {

            IQueryable<MemberBookingsViewModel> memberBookings = null;

            if (dateThreshold.Equals("One Month"))
            {
                memberBookings = from b in _appDbContext.Bookings
                                 join m in _appDbContext.Members on b.Member_ID equals m.Member_ID
                                 join u in _appDbContext.Users on m.User_ID equals u.User_ID
                                 join bts in _appDbContext.Booking_Time_Slots on b.Booking_ID equals bts.Booking_ID
                                 join ts in _appDbContext.Time_Slots on bts.Time_Slot_ID equals ts.Time_Slot_ID
                                 where ts.Slot_Date.Month == DateTime.Now.Month && ts.Slot_Date.Year == DateTime.Now.Year
                                 group b by new { m.Member_ID, u.Name, u.Surname } into g
                                 select new MemberBookingsViewModel
                                 {
                                     Member_ID = g.Key.Member_ID,
                                     Name = g.Key.Name,
                                     Surname = g.Key.Surname,
                                     NumberOfBookings = g.Count()
                                 };
            }

            else if (dateThreshold.Equals("Three Months"))
            {
                memberBookings = from b in _appDbContext.Bookings
                                 join m in _appDbContext.Members on b.Member_ID equals m.Member_ID
                                 join u in _appDbContext.Users on m.User_ID equals u.User_ID
                                 join bts in _appDbContext.Booking_Time_Slots on b.Booking_ID equals bts.Booking_ID
                                 join ts in _appDbContext.Time_Slots on bts.Time_Slot_ID equals ts.Time_Slot_ID
                                 where ts.Slot_Date.Month >= DateTime.Now.Month - 3 && ts.Slot_Date.Month <= DateTime.Now.Month && ts.Slot_Date.Year == DateTime.Now.Year
                                 group b by new { m.Member_ID, u.Name, u.Surname } into g
                                 select new MemberBookingsViewModel
                                 {
                                     Member_ID = g.Key.Member_ID,
                                     Name = g.Key.Name,
                                     Surname = g.Key.Surname,
                                     NumberOfBookings = g.Count()
                                 };
            }
            else if (dateThreshold.Equals("Six Months"))
            {
                memberBookings = from b in _appDbContext.Bookings
                                 join m in _appDbContext.Members on b.Member_ID equals m.Member_ID
                                 join u in _appDbContext.Users on m.User_ID equals u.User_ID
                                 join bts in _appDbContext.Booking_Time_Slots on b.Booking_ID equals bts.Booking_ID
                                 join ts in _appDbContext.Time_Slots on bts.Time_Slot_ID equals ts.Time_Slot_ID
                                 where ts.Slot_Date.Month >= DateTime.Now.Month - 6 && ts.Slot_Date.Month <= DateTime.Now.Month && ts.Slot_Date.Year == DateTime.Now.Year
                                 group b by new { m.Member_ID, u.Name, u.Surname } into g
                                 select new MemberBookingsViewModel
                                 {
                                     Member_ID = g.Key.Member_ID,
                                     Name = g.Key.Name,
                                     Surname = g.Key.Surname,
                                     NumberOfBookings = g.Count()
                                 };
            }
            else if (dateThreshold.Equals("Year"))
            {
                memberBookings = from b in _appDbContext.Bookings
                                 join m in _appDbContext.Members on b.Member_ID equals m.Member_ID
                                 join u in _appDbContext.Users on m.User_ID equals u.User_ID
                                 join bts in _appDbContext.Booking_Time_Slots on b.Booking_ID equals bts.Booking_ID
                                 join ts in _appDbContext.Time_Slots on bts.Time_Slot_ID equals ts.Time_Slot_ID
                                 where ts.Slot_Date.Month >= DateTime.Now.Month - 12 && ts.Slot_Date.Month <= DateTime.Now.Month && ts.Slot_Date.Year == DateTime.Now.Year
                                 group b by new { m.Member_ID, u.Name, u.Surname } into g
                                 select new MemberBookingsViewModel
                                 {
                                     Member_ID = g.Key.Member_ID,
                                     Name = g.Key.Name,
                                     Surname = g.Key.Surname,
                                     NumberOfBookings = g.Count()
                                 };
            }

            //  var totalMemberBookings = await memberBookings.ToListAsync();

            return await memberBookings
                .OrderByDescending(c => c.NumberOfBookings)
                .Take(5)
                .ToListAsync(); ;

        }

        public async Task<ActionResult<IEnumerable<MemberDemographicReportViewModel>>> GetMemberDemographic()
        {
            var ageGroups = from u in _appDbContext.Users
                            join m in _appDbContext.Members on u.User_ID equals m.User_ID
                            let age = DateTime.Now.Year - u.Date_of_Birth.Year
                            let ageGroup =
                                age < 18 ? "Under 18" :
                                age >= 18 && age <= 24 ? "18-24" :
                                age >= 25 && age <= 34 ? "25-34" :
                                age >= 35 && age <= 44 ? "35-44" :
                                age >= 45 && age <= 54 ? "45-54" :
                                age >= 55 && age <= 64 ? "55-64" :
                                "65 and over"
                            group u by ageGroup into g
                            select new MemberDemographicReportViewModel
                            {
                                AgeGroup = g.Key,
                                NumberOfMembers = g.Count()
                            };

            var result = ageGroups.OrderBy(ag => ag.AgeGroup).ToList();

            return result;

        }


        public async Task<int> GetNumberOfUnredeemedRewards()
        {
            var unredeemedRewards = await _appDbContext.Reward_Members
                .Where(rm => !rm.IsRedeemed) 
                .CountAsync();

            return unredeemedRewards;
        }


        //inspections
        public async Task<List<InspectionReportViewModel>> GetInventoryInspections(string dateThreshold)
        {
            IQueryable<InspectionReportViewModel> inventoryInspections = null;

            if (dateThreshold.Equals("One Month"))
            {
                inventoryInspections = from ins in _appDbContext.Inspection
                                       join inv in _appDbContext.Inventory on ins.Inventory_ID equals inv.Inventory_ID
                                       join inst in _appDbContext.Inspection_Type on ins.Inspection_Type_ID equals inst.Inspection_Type_ID
                                       where ins.Inspection_Date.Month == DateTime.Now.Month && ins.Inspection_Date.Year == DateTime.Now.Year
                                       group ins by new { inv.Inventory_ID, inv.Inventory_Item_Name, ins.Inspection_Date, ins.Inspection_Notes, inst.Inspection_Type_Name } into grouped

                                       select new InspectionReportViewModel
                                       {
                                           Number_Of_Inspections = grouped.Count(),
                                           Inventory_Item_Name = grouped.Key.Inventory_Item_Name,
                                           Inspection_Date = grouped.Key.Inspection_Date,
                                           Inspection_Notes = grouped.Key.Inspection_Notes,
                                           Inspection_Type = grouped.Key.Inspection_Type_Name


                                       };

              
            }else if(dateThreshold.Equals("Three Months")){

                inventoryInspections = from ins in _appDbContext.Inspection
                                       join inv in _appDbContext.Inventory on ins.Inventory_ID equals inv.Inventory_ID
                                       join inst in _appDbContext.Inspection_Type on ins.Inspection_Type_ID equals inst.Inspection_Type_ID
                                       where ins.Inspection_Date.Month >= DateTime.Now.Month - 3 && ins.Inspection_Date.Month <= DateTime.Now.Month && ins.Inspection_Date.Year == DateTime.Now.Year
                                       group ins by new { inv.Inventory_ID, inv.Inventory_Item_Name, ins.Inspection_Date, ins.Inspection_Notes, inst.Inspection_Type_Name } into grouped

                                       select new InspectionReportViewModel
                                       {
                                           Number_Of_Inspections = grouped.Count(),
                                           Inventory_Item_Name = grouped.Key.Inventory_Item_Name,
                                           Inspection_Date = grouped.Key.Inspection_Date,
                                           Inspection_Notes = grouped.Key.Inspection_Notes,
                                           Inspection_Type = grouped.Key.Inspection_Type_Name

                                       };
            }else if(dateThreshold.Equals("Six Months"))
            {
                inventoryInspections = from ins in _appDbContext.Inspection
                                       join inv in _appDbContext.Inventory on ins.Inventory_ID equals inv.Inventory_ID
                                       join inst in _appDbContext.Inspection_Type on ins.Inspection_Type_ID equals inst.Inspection_Type_ID
                                       where ins.Inspection_Date.Month >= DateTime.Now.Month - 6 && ins.Inspection_Date.Month <= DateTime.Now.Month && ins.Inspection_Date.Year == DateTime.Now.Year
                                       group ins by new { inv.Inventory_ID, inv.Inventory_Item_Name, ins.Inspection_Date, ins.Inspection_Notes, inst.Inspection_Type_Name } into grouped

                                       select new InspectionReportViewModel
                                       {
                                           Number_Of_Inspections = grouped.Count(),
                                           Inventory_Item_Name = grouped.Key.Inventory_Item_Name,
                                           Inspection_Date = grouped.Key.Inspection_Date,
                                           Inspection_Notes = grouped.Key.Inspection_Notes,
                                           Inspection_Type = grouped.Key.Inspection_Type_Name

                                       };
            }else if (dateThreshold.Equals("Year")){

                inventoryInspections = from ins in _appDbContext.Inspection
                                       join inv in _appDbContext.Inventory on ins.Inventory_ID equals inv.Inventory_ID
                                       join inst in _appDbContext.Inspection_Type on ins.Inspection_Type_ID equals inst.Inspection_Type_ID
                                       where ins.Inspection_Date.Month >= DateTime.Now.Month - 12 && ins.Inspection_Date.Month <= DateTime.Now.Month && ins.Inspection_Date.Year == DateTime.Now.Year
                                       group ins by new { inv.Inventory_ID, inv.Inventory_Item_Name, ins.Inspection_Date, ins.Inspection_Notes, inst.Inspection_Type_Name } into grouped

                                       select new InspectionReportViewModel
                                       {
                                           Number_Of_Inspections = grouped.Count(),
                                           Inventory_Item_Name = grouped.Key.Inventory_Item_Name,
                                           Inspection_Date = grouped.Key.Inspection_Date,
                                           Inspection_Notes = grouped.Key.Inspection_Notes,
                                           Inspection_Type = grouped.Key.Inspection_Type_Name

                                       };
            }


            return inventoryInspections.ToList();
        }



        public async Task<List<InspectionReportViewModel>> GetEqupimentInspections(string dateThreshold)
        {
            IQueryable<InspectionReportViewModel> equpimentInspections = null;

            if (dateThreshold.Equals("One Month"))
            {
                 equpimentInspections = from ins in _appDbContext.Inspection
                                                    join equi in _appDbContext.Equipment on ins.Equipment_ID equals equi.Equipment_ID
                                                    join inst in _appDbContext.Inspection_Type on ins.Inspection_Type_ID equals inst.Inspection_Type_ID
                                                    where ins.Inspection_Date.Month == DateTime.Now.Month && ins.Inspection_Date.Year == DateTime.Now.Year
                                        group ins by new { equi.Equipment_ID, equi.Equipment_Name, ins.Inspection_Date, ins.Inspection_Notes, inst.Inspection_Type_Name } into grouped
                                                    select new InspectionReportViewModel
                                                    {
                                                        Number_Of_Inspections = grouped.Count(),
                                                        Equipment_Name = grouped.Key.Equipment_Name,
                                                        Inspection_Date = grouped.Key.Inspection_Date,
                                                        Inspection_Notes = grouped.Key.Inspection_Notes,
                                                        Inspection_Type = grouped.Key.Inspection_Type_Name

                                                    };


            }
            else if (dateThreshold.Equals("Three Months"))
            {

                equpimentInspections = from ins in _appDbContext.Inspection
                                       join equi in _appDbContext.Equipment on ins.Equipment_ID equals equi.Equipment_ID
                                       join inst in _appDbContext.Inspection_Type on ins.Inspection_Type_ID equals inst.Inspection_Type_ID
                                       where ins.Inspection_Date.Month >= DateTime.Now.Month - 3 && ins.Inspection_Date.Month <= DateTime.Now.Month && ins.Inspection_Date.Year == DateTime.Now.Year
                                       group ins by new { equi.Equipment_ID, equi.Equipment_Name, ins.Inspection_Date, ins.Inspection_Notes, inst.Inspection_Type_Name } into grouped
                                       select new InspectionReportViewModel
                                       {
                                           Number_Of_Inspections = grouped.Count(),
                                           Equipment_Name = grouped.Key.Equipment_Name,
                                           Inspection_Date = grouped.Key.Inspection_Date,
                                           Inspection_Notes = grouped.Key.Inspection_Notes,
                                           Inspection_Type = grouped.Key.Inspection_Type_Name

                                       };
            }
            else if (dateThreshold.Equals("Six Months"))
            {
                equpimentInspections = from ins in _appDbContext.Inspection
                                       join equi in _appDbContext.Equipment on ins.Equipment_ID equals equi.Equipment_ID
                                       join inst in _appDbContext.Inspection_Type on ins.Inspection_Type_ID equals inst.Inspection_Type_ID
                                       where ins.Inspection_Date.Month >= DateTime.Now.Month - 6 && ins.Inspection_Date.Month <= DateTime.Now.Month && ins.Inspection_Date.Year == DateTime.Now.Year
                                       group ins by new { equi.Equipment_ID, equi.Equipment_Name, ins.Inspection_Date, ins.Inspection_Notes, inst.Inspection_Type_Name } into grouped
                                       select new InspectionReportViewModel
                                       {
                                           Number_Of_Inspections = grouped.Count(),
                                           Equipment_Name = grouped.Key.Equipment_Name,
                                           Inspection_Date = grouped.Key.Inspection_Date,
                                           Inspection_Notes = grouped.Key.Inspection_Notes,
                                           Inspection_Type = grouped.Key.Inspection_Type_Name

                                       };
            }
            else if (dateThreshold.Equals("Year"))
            {

                equpimentInspections = from ins in _appDbContext.Inspection
                                       join equi in _appDbContext.Equipment on ins.Equipment_ID equals equi.Equipment_ID
                                       join inst in _appDbContext.Inspection_Type on ins.Inspection_Type_ID equals inst.Inspection_Type_ID
                                       where ins.Inspection_Date.Month >= DateTime.Now.Month - 12 && ins.Inspection_Date.Month <= DateTime.Now.Month && ins.Inspection_Date.Year == DateTime.Now.Year
                                       group ins by new { equi.Equipment_ID, equi.Equipment_Name, ins.Inspection_Date, ins.Inspection_Notes, inst.Inspection_Type_Name } into grouped
                                       select new InspectionReportViewModel
                                       {
                                           Number_Of_Inspections = grouped.Count(),
                                           Equipment_Name = grouped.Key.Equipment_Name,
                                           Inspection_Date = grouped.Key.Inspection_Date,
                                           Inspection_Notes = grouped.Key.Inspection_Notes,
                                           Inspection_Type = grouped.Key.Inspection_Type_Name

                                       };
            }


            return equpimentInspections.ToList();
        }


        //Inventory

        public async Task<List<InventoryReportViewModel>> GetInventoryReportData()
        {
            IQueryable<InventoryReportViewModel> inventoryData = null;

             inventoryData = from wf in _appDbContext.Write_Offs
                         join inv in _appDbContext.Inventory on wf.Inventory_ID equals inv.Inventory_ID
                         join prod in _appDbContext.Products on inv.Product_ID equals prod.Product_ID
                         join sol in _appDbContext.Supplier_Order_Lines on prod.Product_ID equals sol.Product_ID
                         join so in _appDbContext.Supplier_Orders on sol.Supplier_Order_ID equals so.Supplier_Order_ID
                         //where so.Date == DateTime.Now
                         group new { wf, inv, prod, sol } by new
                         {
                             inv.Inventory_Item_Name,
                             prod.Product_Description,
                             prod.Unit_Price,
                             inv.Inventory_Item_Category,
                             inv.Inventory_Item_Quantity,
                             sol.Supplier_Quantity
                         } into g
                         select new InventoryReportViewModel
                         {
                             Name = g.Key.Inventory_Item_Name,
                             Description = g.Key.Product_Description,
                             UnitPrice = g.Key.Unit_Price,
                             QuantityInStock = g.Key.Inventory_Item_Quantity,
                             TotalStockValue = g.Key.Inventory_Item_Quantity * g.Key.Unit_Price,
                             QuantityWrittenOff = g.Sum(x => x.wf.Write_Off_Quantity),
                             TotalWriteOffValue = g.Sum(x => x.wf.Write_Off_Quantity) * g.Key.Unit_Price,
                             QuantityOrdered = g.Key.Supplier_Quantity,
                             TotalValue = g.Key.Supplier_Quantity * g.Key.Unit_Price
                         };






            return await inventoryData.ToListAsync();

        }

            //financial



            public async Task<decimal> GetTotalReceived(string dateThreshold)
            {
                IQueryable<Payment> payments = _appDbContext.Payments;

                if (dateThreshold.Equals("One Month"))
                {
                    payments = payments.Where(p => p.Payment_Date.Month == DateTime.Now.Month && p.Payment_Date.Year == DateTime.Now.Year);
                }
                else if (dateThreshold.Equals("Three Months"))
                {
                    payments = payments.Where(p => p.Payment_Date.Month >= DateTime.Now.Month - 3 && p.Payment_Date.Month <= DateTime.Now.Month && p.Payment_Date.Year == DateTime.Now.Year);
                }
                else if (dateThreshold.Equals("Six Months"))
                {
                    payments = payments.Where(p => p.Payment_Date.Month >= DateTime.Now.Month - 6 && p.Payment_Date.Month <= DateTime.Now.Month && p.Payment_Date.Year == DateTime.Now.Year) ;
                }
                else if (dateThreshold.Equals("Year"))
                {
                    payments = payments.Where(p => p.Payment_Date.Month >= DateTime.Now.Month - 12 && p.Payment_Date.Month <= DateTime.Now.Month && p.Payment_Date.Year == DateTime.Now.Year);
                }

                return await payments.SumAsync(p => p.Amount);
            }


            public async Task<decimal> GetTotalOutstanding(string dateThreshold)
            {
                IQueryable<Outstanding_Payment> outstandingPayments = _appDbContext.Outstanding_Payments;

                if (dateThreshold.Equals("One Month"))
                {
                    outstandingPayments = outstandingPayments.Where(op => op.Due_Date.Month == DateTime.Now.Month && op.Due_Date.Year <= DateTime.Now.Year);
                }
                else if (dateThreshold.Equals("Three Months"))
                {
                    outstandingPayments = outstandingPayments.Where(op => op.Due_Date.Month >= DateTime.Now.Month - 3 && op.Due_Date.Month <= DateTime.Now.Month && op.Due_Date.Year <= DateTime.Now.Year);
                }
                else if (dateThreshold.Equals("Six Months"))
                {
                    outstandingPayments = outstandingPayments.Where(op => op.Due_Date.Month >= DateTime.Now.Month - 6 && op.Due_Date.Month <= DateTime.Now.Month && op.Due_Date.Year <= DateTime.Now.Year);
                }
                else if (dateThreshold.Equals("Year"))
                {
                    outstandingPayments = outstandingPayments.Where(op => op.Due_Date.Month >= DateTime.Now.Month - 12 && op.Due_Date.Month <= DateTime.Now.Month && op.Due_Date.Year <= DateTime.Now.Year);
                }

                return await outstandingPayments.SumAsync(op => op.Amount_Due);
            }



            public async Task<List<FinancialReportViewModel>> GetPaymentsByType(string dateThreshold)
            {
                IQueryable<FinancialReportViewModel> paymentsByType = null;

                if (dateThreshold.Equals("One Month"))
                {
                    paymentsByType = from p in _appDbContext.Payments
                                     join pt in _appDbContext.Payment_Types on p.Payment_Type_ID equals pt.Payment_Type_ID
                                     join o in _appDbContext.Orders on p.Order_ID equals o.Order_ID
                                     join m in _appDbContext.Members on o.Member_ID equals m.Member_ID
                                     join u in _appDbContext.Users on m.User_ID equals u.User_ID
                                     where p.Payment_Date.Month == DateTime.Now.Month && p.Payment_Date.Year == DateTime.Now.Year
                                     group p by new { pt.Payment_Type_Name, u.Name, u.Surname, m.Member_ID } into g
                                     select new FinancialReportViewModel
                                     {
                                         Payment_Type_Name = g.Key.Payment_Type_Name,
                                         Total_Received = g.Sum(p => p.Amount),
                                         NumberOfPayments = g.Count(),
                                         Payer = g.Key.Name + ' ' + g.Key.Surname,
                                         Member_ID = g.Key.Member_ID

                                     };
                }
                else if (dateThreshold.Equals("Three Months"))
                {
                    paymentsByType = from p in _appDbContext.Payments
                                     join pt in _appDbContext.Payment_Types on p.Payment_Type_ID equals pt.Payment_Type_ID
                                     join o in _appDbContext.Orders on p.Order_ID equals o.Order_ID
                                     join m in _appDbContext.Members on o.Member_ID equals m.Member_ID
                                     join u in _appDbContext.Users on m.User_ID equals u.User_ID
                                     where p.Payment_Date.Month >= DateTime.Now.Month - 3 && p.Payment_Date.Month <= DateTime.Now.Month && p.Payment_Date.Year == DateTime.Now.Year
                                     group p by new { pt.Payment_Type_Name, u.Name, u.Surname, m.Member_ID } into g
                                     select new FinancialReportViewModel
                                     {
                                         Payment_Type_Name = g.Key.Payment_Type_Name,
                                         Total_Received = g.Sum(p => p.Amount),
                                         NumberOfPayments = g.Count(),
                                         Payer = g.Key.Name + ' ' + g.Key.Surname,
                                         Member_ID = g.Key.Member_ID
                                     };
                }
                else if (dateThreshold.Equals("Six Months"))
                {
                    paymentsByType = from p in _appDbContext.Payments
                                     join pt in _appDbContext.Payment_Types on p.Payment_Type_ID equals pt.Payment_Type_ID
                                     join o in _appDbContext.Orders on p.Order_ID equals o.Order_ID
                                     join m in _appDbContext.Members on o.Member_ID equals m.Member_ID
                                     join u in _appDbContext.Users on m.User_ID equals u.User_ID
                                     where p.Payment_Date.Month >= DateTime.Now.Month - 6 && p.Payment_Date.Month <= DateTime.Now.Month && p.Payment_Date.Year == DateTime.Now.Year
                                     group p by new { pt.Payment_Type_Name, u.Name, u.Surname, m.Member_ID } into g
                                     select new FinancialReportViewModel
                                     {
                                         Payment_Type_Name = g.Key.Payment_Type_Name,
                                         Total_Received = g.Sum(p => p.Amount),
                                         NumberOfPayments = g.Count(),
                                         Payer = g.Key.Name + ' ' + g.Key.Surname,
                                         Member_ID = g.Key.Member_ID
                                     };
                }
                else if (dateThreshold.Equals("Year"))
                {
                    paymentsByType = from p in _appDbContext.Payments
                                     join pt in _appDbContext.Payment_Types on p.Payment_Type_ID equals pt.Payment_Type_ID
                                     join o in _appDbContext.Orders on p.Order_ID equals o.Order_ID
                                     join m in _appDbContext.Members on o.Member_ID equals m.Member_ID
                                     join u in _appDbContext.Users on m.User_ID equals u.User_ID
                                     where p.Payment_Date.Month >= DateTime.Now.Month - 12 && p.Payment_Date.Month <= DateTime.Now.Month && p.Payment_Date.Year == DateTime.Now.Year
                                     group p by new { pt.Payment_Type_Name, u.Name, u.Surname, m.Member_ID } into g
                                     select new FinancialReportViewModel
                                     {
                                         Payment_Type_Name = g.Key.Payment_Type_Name,
                                         Total_Received = g.Sum(p => p.Amount),
                                         NumberOfPayments = g.Count(),
                                         Payer = g.Key.Name + ' ' + g.Key.Surname,
                                         Member_ID = g.Key.Member_ID
                                     };
                }

                return await paymentsByType.ToListAsync();
            }


        //audit trail
        public async Task<List<Audit_Trail>> GetAuditTrailData(string dateThreshold)
        {
            IQueryable<Audit_Trail> auditTrailQuery = null;

            if (dateThreshold.Equals("One Month"))
            {

                auditTrailQuery = from audit in _appDbContext.Audit_Trails
                                  where audit.Timestamp.Month == DateTime.Now.Month && audit.Timestamp.Year == DateTime.Now.Year
                                  orderby audit.Timestamp descending
                                  select new Audit_Trail
                                  {
                                      Transaction_Type = audit.Transaction_Type,
                                      Critical_Data = audit.Critical_Data,
                                      Changed_By = audit.Changed_By,
                                      Table_Name = audit.Table_Name,
                                      Timestamp = audit.Timestamp
                                  };

            }
            else if (dateThreshold.Equals("Three Months"))
            {
                auditTrailQuery = from audit in _appDbContext.Audit_Trails
                                  where audit.Timestamp.Month >= DateTime.Now.Month - 3 && audit.Timestamp.Month <= DateTime.Now.Month && audit.Timestamp.Year == DateTime.Now.Year
                                  orderby audit.Timestamp descending
                                  select new Audit_Trail
                                  {
                                      Transaction_Type = audit.Transaction_Type,
                                      Critical_Data = audit.Critical_Data,
                                      Changed_By = audit.Changed_By,
                                      Table_Name = audit.Table_Name,
                                      Timestamp = audit.Timestamp
                                  };
            }
            else if (dateThreshold.Equals("Six Months"))
            {
                auditTrailQuery = from audit in _appDbContext.Audit_Trails
                                  where audit.Timestamp.Month >= DateTime.Now.Month - 6 && audit.Timestamp.Month <= DateTime.Now.Month && audit.Timestamp.Year == DateTime.Now.Year
                                  orderby audit.Timestamp descending
                                  select new Audit_Trail
                                  {
                                      Transaction_Type = audit.Transaction_Type,
                                      Critical_Data = audit.Critical_Data,
                                      Changed_By = audit.Changed_By,
                                      Table_Name = audit.Table_Name,
                                      Timestamp = audit.Timestamp
                                  };
            }
            else if (dateThreshold.Equals("Year"))
            {
                auditTrailQuery = from audit in _appDbContext.Audit_Trails
                                  where audit.Timestamp.Year == DateTime.Now.Year
                                  orderby audit.Timestamp descending
                                  select new Audit_Trail
                                  {
                                      Transaction_Type = audit.Transaction_Type,
                                      Critical_Data = audit.Critical_Data,
                                      Changed_By = audit.Changed_By,
                                      Table_Name = audit.Table_Name,
                                      Timestamp = audit.Timestamp
                                  };
            }

            return await auditTrailQuery.ToListAsync();
        }






        //dashboard
        public async Task<IEnumerable<DashboardViewModel>> GetPopularProducts(string filter)
            {
                var today = DateTime.Now.Date;
                IQueryable<DashboardViewModel> query = null;
                if (filter == "day")
                {
                    //DateTime startDate = GetStartDate(filter);
                    query = (from ol in _appDbContext.Order_Lines
                             join p in _appDbContext.Products on ol.Product_ID equals p.Product_ID
                             join o in _appDbContext.Orders on ol.Order_ID equals o.Order_ID
                             where o.Order_Date.Day == DateTime.Now.Day && o.Order_Date.Month == DateTime.Now.Month && o.Order_Date.Year == DateTime.Now.Year
                             group ol by new { ol.Product_ID, p.Product_Name } into g
                             orderby g.Count() descending
                             select new DashboardViewModel
                             {
                                 ProductName = g.Key.Product_Name,
                                 ProductOrderCount = g.Sum(s => s.Quantity)
                             }).Take(5);
                } else if (filter == "month")
                {
                    query = (from ol in _appDbContext.Order_Lines
                             join p in _appDbContext.Products on ol.Product_ID equals p.Product_ID
                             join o in _appDbContext.Orders on ol.Order_ID equals o.Order_ID
                             where o.Order_Date.Month == DateTime.Now.Month && o.Order_Date.Year == DateTime.Now.Year
                             group ol by new { ol.Product_ID, p.Product_Name } into g
                             orderby g.Count() descending
                             select new DashboardViewModel
                             {
                                 // ProductId = g.Key.ProductId,
                                 ProductName = g.Key.Product_Name,
                                 ProductOrderCount = g.Sum(s => s.Quantity)
                             }).Take(5);
                }
                else {
                    query = (from ol in _appDbContext.Order_Lines
                             join p in _appDbContext.Products on ol.Product_ID equals p.Product_ID
                             join o in _appDbContext.Orders on ol.Order_ID equals o.Order_ID
                             where o.Order_Date.Day >= DateTime.Now.Day - 7 && o.Order_Date.Day <= DateTime.Now.Day && o.Order_Date.Month == DateTime.Now.Month && o.Order_Date.Year == DateTime.Now.Year
                             group ol by new { ol.Product_ID, p.Product_Name } into g
                             orderby g.Count() descending
                             select new DashboardViewModel
                             {
                                 // ProductId = g.Key.ProductId,
                                 ProductName = g.Key.Product_Name,
                                 ProductOrderCount = g.Sum(s => s.Quantity)
                             }).Take(5);


                }


                return query.ToList();
            }

            public async Task<IEnumerable<DashboardViewModel>> GetTopMembers(string filter)
            {
                IQueryable<DashboardViewModel> memberBookings = null;
                if (filter == "day")
                {
                    //DateTime startDate = GetStartDate(filter);
                    memberBookings = from b in _appDbContext.Bookings
                                     join m in _appDbContext.Members on b.Member_ID equals m.Member_ID
                                     join u in _appDbContext.Users on m.User_ID equals u.User_ID
                                     join bts in _appDbContext.Booking_Time_Slots on b.Booking_ID equals bts.Booking_ID
                                     join ts in _appDbContext.Time_Slots on bts.Time_Slot_ID equals ts.Time_Slot_ID
                                     group b by new { m.Member_ID, u.Name, u.Surname } into g
                                     select new DashboardViewModel
                                     {
                                         Member_ID = g.Key.Member_ID,
                                         Name = g.Key.Name,
                                         Surname = g.Key.Surname,
                                         BookingsCount = g.Count()
                                     };
                }

                else if (filter == "month")
                {
                    memberBookings = from b in _appDbContext.Bookings
                                     join m in _appDbContext.Members on b.Member_ID equals m.Member_ID
                                     join u in _appDbContext.Users on m.User_ID equals u.User_ID
                                     join bts in _appDbContext.Booking_Time_Slots on b.Booking_ID equals bts.Booking_ID
                                     join ts in _appDbContext.Time_Slots on bts.Time_Slot_ID equals ts.Time_Slot_ID
                                     group b by new { m.Member_ID, u.Name, u.Surname } into g
                                     select new DashboardViewModel
                                     {
                                         Member_ID = g.Key.Member_ID,
                                         Name = g.Key.Name,
                                         Surname = g.Key.Surname,
                                         BookingsCount = g.Count()
                                     };
                }
                else
                {
                    memberBookings = from b in _appDbContext.Bookings
                                     join m in _appDbContext.Members on b.Member_ID equals m.Member_ID
                                     join u in _appDbContext.Users on m.User_ID equals u.User_ID
                                     join bts in _appDbContext.Booking_Time_Slots on b.Booking_ID equals bts.Booking_ID
                                     join ts in _appDbContext.Time_Slots on bts.Time_Slot_ID equals ts.Time_Slot_ID
                                     group b by new { m.Member_ID, u.Name, u.Surname } into g
                                     select new DashboardViewModel
                                     {
                                         Member_ID = g.Key.Member_ID,
                                         Name = g.Key.Name,
                                         Surname = g.Key.Surname,
                                         BookingsCount = g.Count()
                                     };


                }




                return await memberBookings
                    .OrderByDescending(c => c.BookingsCount)
                    .Take(5)
                    .ToListAsync();

            }
            public async Task<IEnumerable<DashboardViewModel>> GetSubscriptionData(string filter)
            {


                //DateTime startDate = GetStartDate(filter);
                //DateTime previousPeriodStartDate = GetPreviousPeriodStartDate(filter);

                IQueryable<DashboardViewModel> currentPeriodSubscriptions = null;
                if (filter == "day")
                {
                    currentPeriodSubscriptions = from c in _appDbContext.Contracts
                                                 where c.Subscription_Date.Day == DateTime.Now.Day && c.Subscription_Date.Month == DateTime.Now.Month && c.Subscription_Date.Year == DateTime.Now.Year
                                                 group c by c.Subscription_Date.Date into g
                                                 select new DashboardViewModel
                                                 {
                                                     Date = g.Key,
                                                     SubscriptionsCount = g.Count()
                                                 };
                } else if (filter == "month")
                {
                    currentPeriodSubscriptions = from c in _appDbContext.Contracts
                                                 where c.Subscription_Date.Month == DateTime.Now.Month && c.Subscription_Date.Year == DateTime.Now.Year
                                                 group c by c.Subscription_Date.Date into g
                                                 select new DashboardViewModel
                                                 {
                                                     Date = g.Key,
                                                     SubscriptionsCount = g.Count()
                                                 };
                }
                else
                {
                    currentPeriodSubscriptions = from c in _appDbContext.Contracts
                                                 where c.Subscription_Date.Day >= DateTime.Now.Day - 7 && c.Subscription_Date.Day <= DateTime.Now.Day && c.Subscription_Date.Year == DateTime.Now.Year
                                                 group c by c.Subscription_Date.Date into g
                                                 select new DashboardViewModel
                                                 {
                                                     Date = g.Key,
                                                     SubscriptionsCount = g.Count()
                                                 };
                }
                // Get current period subscriptions



                //var currentPeriodSubscriptionsList = await currentPeriodSubscriptions.ToListAsync();
                //int currentPeriodSubscriptionsCount = currentPeriodSubscriptionsList.Sum(d => d.SubscriptionsCount);

                //// Get previous period subscriptions
                //var previousPeriodSubscriptionsCount = await (from c in _appDbContext.Contracts
                //                                              where c.Approval_Date >= previousPeriodStartDate && c.Approval_Date < startDate
                //                                              select c.Contract_ID).CountAsync();

                //// Calculate percentage change
                //decimal percentageChange = previousPeriodSubscriptionsCount != 0
                //    ? ((decimal)(currentPeriodSubscriptionsCount - previousPeriodSubscriptionsCount) / previousPeriodSubscriptionsCount) * 100
                //    : 0;

                //// Attach percentage change to the result
                //foreach (var item in currentPeriodSubscriptionsList)
                //{
                //    item.PercentageChange = percentageChange;
                //}

                return currentPeriodSubscriptions;
            }



            public async Task<IEnumerable<DashboardViewModel>> GetSalesData(string filter)
            {

                IQueryable<DashboardViewModel> currentPeriodSales = null;
                if (filter == "day")
                {
                    currentPeriodSales = from o in _appDbContext.Orders
                                         where o.Order_Date.Day == DateTime.Now.Day && o.Order_Date.Month == DateTime.Now.Month && o.Order_Date.Year == DateTime.Now.Year
                                         group o by o.Order_Date.Date into g
                                         select new DashboardViewModel
                                         {
                                             Date = g.Key,
                                             SalesCount = g.Count(),
                                             TotalSales = g.Sum(o => o.Total_Price)
                                         };
                } else if (filter == "month")
                {
                    currentPeriodSales = from o in _appDbContext.Orders
                                         where o.Order_Date.Month == DateTime.Now.Month && o.Order_Date.Year == DateTime.Now.Year
                                         group o by o.Order_Date.Date into g
                                         select new DashboardViewModel
                                         {
                                             Date = g.Key,
                                             SalesCount = g.Count(),
                                             TotalSales = g.Sum(o => o.Total_Price)
                                         };
                }
                else
                {
                    currentPeriodSales = from o in _appDbContext.Orders
                                         where o.Order_Date.Day >= DateTime.Now.Day - 7 && o.Order_Date.Day <= DateTime.Now.Day && o.Order_Date.Month == DateTime.Now.Month && o.Order_Date.Year == DateTime.Now.Year
                                         group o by o.Order_Date.Date into g
                                         select new DashboardViewModel
                                         {
                                             Date = g.Key,
                                             SalesCount = g.Count(),
                                             TotalSales = g.Sum(o => o.Total_Price)
                                         };
                }




                //    DateTime startDate = GetStartDate(filter);
                //DateTime previousPeriodStartDate = GetPreviousPeriodStartDate(filter);

                // Get current period sales


                //var currentPeriodSalesList = await currentPeriodSales.ToListAsync();
                //int currentPeriodSalesCount = currentPeriodSalesList.Sum(d => d.SalesCount);

                //// Get previous period sales
                //var previousPeriodSalesCount = await (from o in _appDbContext.Orders
                //                                      where o.Order_Date >= previousPeriodStartDate && o.Order_Date < startDate
                //                                      select o.Order_ID).CountAsync();

                //// Calculate percentage change
                //decimal percentageChange = previousPeriodSalesCount != 0
                //    ? ((decimal)(currentPeriodSalesCount - previousPeriodSalesCount) / previousPeriodSalesCount) * 100
                //    : 0;

                //// Attach percentage change to the result
                //foreach (var item in currentPeriodSalesList)
                //{
                //    item.PercentageChange = percentageChange;
                //}

                return currentPeriodSales.ToList();
            }


            private DateTime GetStartDate(string filter)
            {
                switch (filter)
                {
                    case "day":
                        return DateTime.Today.AddDays(-1);
                    case "week":
                        return DateTime.Today.AddDays(-7);
                    case "month":
                        return DateTime.Today.AddMonths(-1);
                    default:
                        return DateTime.Today.AddDays(-7);
                }
            }

        private DateTime GetPreviousPeriodStartDate(string filter)
        {
            switch (filter)
            {
                case "day":
                    return DateTime.Today.AddDays(-2);
                case "week":
                    return DateTime.Today.AddDays(-14);
                case "month":
                    return DateTime.Today.AddMonths(-2);
                default:
                    return DateTime.Today.AddDays(-14);
            }


        }
    }
    }














