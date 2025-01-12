using Microsoft.AspNetCore.Mvc;
using av_motion_api.ViewModels;
using av_motion_api.Models;

namespace av_motion_api.Interfaces
{
    public interface IRepository
    {

        //Attendance List
        Task<ActionResult<IEnumerable<AttendanceListViewModel>>> GenerateAttendanceList(int timeSlotID);
        Task<ActionResult<IEnumerable<AttendanceListViewModel>>> GenerateAttendanceLists();

        //Workout Category References 
        Task<ActionResult<Workout_Category>> GetWorkoutCategoryByName(string categoryName);

        //Lesson Plan References 
        Task<ActionResult<IEnumerable<LessonPlanViewModel>>> GetLessonPlanWithWorkouts(int lessonPlanId);
        Task<ActionResult<IEnumerable<LessonPlanViewModel>>> GetLessonPlanWithWorkout();

        //Workout References
        Task<ActionResult<IEnumerable<ViewWorkoutViewModel>>> GetWorkoutCategories();
        Task<ActionResult<IEnumerable<ViewWorkoutViewModel>>> GetWorkoutCategory(int workoutID);

        //Booking References
        Task<ActionResult<IEnumerable<BookingViewModel>>> GetBookings();
        Task<ActionResult<BookingViewModel>> GetBooking(int id, int memebrID);
        Task<ActionResult<IEnumerable<BookingViewModel>>> GetMemberBookings(int memebrUserID);

        //TimeSlots
        Task<ActionResult<IEnumerable<TimeSlotCalanderViewModel>>> GetTimeSlots();
        Task<ActionResult<TimeSlotCalanderViewModel>> GetTimeSlot(int id);
        Task<ActionResult<IEnumerable<TimeSlotCalanderViewModel>>> GetSlotByDate(DateTime date);
        Task<List<TimeSlotCalanderViewModel>> GetSlotByDateAndTime(DateTime date, int lessonPlanID);
        Task<ActionResult<IEnumerable<TimeSlotCalanderViewModel>>> GetTimeSlotByLessonPlan(int id);


        //Inspection References
        Task<ActionResult<IEnumerable<InspectionViewModel>>> GetInspectionsDetails();
        Task<ActionResult<IEnumerable<InspectionViewModel>>> GetInspectionDetails(int inspectionID);

        //Inventory References
        Task<ActionResult<IEnumerable<InventoryViewModel>>> GetInventoryDetails();
        Task<ActionResult<IEnumerable<InventoryViewModel>>> GetInventoryItem(int id);

        ////Product References
        //Task<ActionResult<IEnumerable<ProductViewModel>>> GetProducts();
        //Task<ActionResult<IEnumerable<ProductViewModel>>> GetProduct(int id);


        //Writeoff References
        Task<ActionResult<IEnumerable<WriteoffViewModel>>> GetWriteOffs();
        Task<ActionResult<IEnumerable<WriteoffViewModel>>> GetWriteOff(int id);

        //Reporting References
        Task<ActionResult<string>> GetReportGenerator(int userid);

        Task<ActionResult<IEnumerable<BookingsReportViewModel>>> MostPopularLessonPlans(string dateThreshold);
        Task<ActionResult<int>> TotalBookings(string dateThreshold);
        //Task<ActionResult<IEnumerable<BookingsReportViewModel>>> PopularBookingTime(string dateThreshold);
        //Task<ActionResult<IEnumerable<BookingsReportViewModel>>> DateWithTheMostBookings(string dateThreshold);

        Task<List<Audit_Trail>> GetAuditTrailData(string dateThreshold);
        Task<ActionResult<int>> TotalOrders(string dateThreshold);
        Task<ActionResult<IEnumerable<OrderReportViewModel>>> ProductsPurchased(string dateThreshold);
        //Task<IEnumerable<OrderReportViewModel>> GetOrdersByProductAndCategory(string dateThreshold);


        Task<int> GetNewSubscriptions(string dateThreshold);
        Task<ActionResult<IEnumerable<MemberBookingsViewModel>>> GetMemberBookings(string dateThreshold);
        Task<ActionResult<IEnumerable<MemberDemographicReportViewModel>>> GetMemberDemographic();
        //Task<IEnumerable<MemberDemographicReportViewModel>> GetContractsByType(string dateThreshold);
        Task<int> GetNumberOfUnredeemedRewards();


        Task<List<InspectionReportViewModel>> GetInventoryInspections(string dateThreshold);
        Task<List<InspectionReportViewModel>> GetEqupimentInspections(string dateThreshold);



        Task<List<InventoryReportViewModel>> GetInventoryReportData();



        Task<List<OrderReportViewModel>> GetOrderSalesByProductAndCategory(string dateThreshold);
        Task<decimal> GetTotalReceived(string dateThreshold);
        Task<decimal> GetTotalOutstanding(string dateThreshold);
        Task<List<FinancialReportViewModel>> GetPaymentsByType(string dateThreshold);


        Task<IEnumerable<DashboardViewModel>> GetSalesData(string filter);
        Task<IEnumerable<DashboardViewModel>> GetTopMembers(string filter);
        Task<IEnumerable<DashboardViewModel>> GetSubscriptionData(string filter);
        Task<IEnumerable<DashboardViewModel>> GetPopularProducts(string filter);


    }

}
