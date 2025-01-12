using av_motion_api.Data;
using av_motion_api.Interfaces;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        
        public readonly IRepository _repository;
        public ReportsController(AppDbContext _context, IRepository repository)
        {
            _repository = repository;
        }
        //number of bookings per lesson plan
        [HttpGet]
        [Route("mostPopularLessonPlans")]
        public async Task<ActionResult<IEnumerable<BookingsReportViewModel>>> GetBookings([FromQuery] string dateThreshold)
        {
            var popularLessonPlans = await _repository.MostPopularLessonPlans(dateThreshold);
            return popularLessonPlans;

        }

        //total number of bookings for the time period
        [HttpGet]
        [Route("totalBookings")]
        public async Task<ActionResult<int>> GetTotalBookings([FromQuery] string dateThreshold)
        {
            var totalBookings = await _repository.TotalBookings(dateThreshold);
            return totalBookings;

        }


        //number of products purchased
        [HttpGet]
        [Route("numberOfProductsPurchased")]
        public async Task<ActionResult<IEnumerable<OrderReportViewModel>>> GetProductsPurchased([FromQuery] string dateThreshold)
        {
            var purchasedProduct = await _repository.ProductsPurchased(dateThreshold);
            return purchasedProduct;

        }

        //gets the total orders
        [HttpGet]
        [Route("totalOrders")]
        public async Task<ActionResult<int>> GetTotalOrders([FromQuery] string dateThreshold)
        {
            var totalOrders = await _repository.TotalOrders(dateThreshold);
            return totalOrders;

        }


        //get the new subscritpions in the time frame

        [HttpGet]
        [Route("totalNewContracts")]
        public async Task<ActionResult<int>> GetTotalContracts([FromQuery] string dateThreshold)
        {
            var totalNewSubscriptions = await _repository.GetNewSubscriptions(dateThreshold);
            return totalNewSubscriptions;

        }


        //gets the number of bookings per member
        [HttpGet]
        [Route("memberBookings")]
        public async Task<ActionResult<IEnumerable<MemberBookingsViewModel>>> GetMemberBookings([FromQuery] string dateThreshold)
        {
            var memberBookings = await _repository.GetMemberBookings(dateThreshold);
            return memberBookings;

        }

        //gets the member demographic
        [HttpGet]
        [Route("memberAgeDemographic")]
        public async Task<ActionResult<IEnumerable<MemberDemographicReportViewModel>>> GetMemberAgeDemographic()
        {
            var memberDemographic = await _repository.GetMemberDemographic();
            return memberDemographic;

        }


        // Gets the number of unredeemed rewards
        [HttpGet]
        [Route("unredeemedRewards")]
        public async Task<ActionResult<int>> GetNumberOfUnredeemedRewards()
        {
            var unredeemedRewards = await _repository.GetNumberOfUnredeemedRewards();
            return Ok(unredeemedRewards);
        }



        //gets number of inventory inspections
        [HttpGet]
        [Route("inventoryInspections")]
        public async Task<ActionResult<IEnumerable<InspectionReportViewModel>>> GetNumberOfInventoryInspections([FromQuery] string dateThreshold)
        {
            var inventoryInspections = await _repository.GetInventoryInspections(dateThreshold);
            return Ok(inventoryInspections);
        }

        ////gets number of equpiment inspections
        [HttpGet]
        [Route("equipmentInspections")]
        public async Task<ActionResult<IEnumerable<InspectionReportViewModel>>> GetNumberOfEquipmentInspections([FromQuery] string dateThreshold)
        {
            var equpimentInspections = await _repository.GetEqupimentInspections(dateThreshold);
            return Ok(equpimentInspections);
        }


        ////gets inventory stock data
        [HttpGet]
        [Route("stockData")]
        public async Task<ActionResult<IEnumerable<InventoryReportViewModel>>> GetInventoryReportData()
        {
            var reportData = await _repository.GetInventoryReportData();
            return Ok(reportData);
        }




        //get product order sales by categories
        [HttpGet]
        [Route("orderSalesByProductAndCategory")]
        public async Task<ActionResult<IEnumerable<OrderReportViewModel>>> GetOrderSalesByProductAndCategory([FromQuery] string dateThreshold)
        {
            var orders = await _repository.GetOrderSalesByProductAndCategory(dateThreshold);
            return orders;
        }

        //total revenue recieved
        [HttpGet]
        [Route("totalReceived")]
        public async Task<ActionResult<decimal>> GetTotalReceived([FromQuery] string dateThreshold)
        {
            var totalReceived = await _repository.GetTotalReceived(dateThreshold);
            return Ok(totalReceived);
        }

        //total outstanding amount
        [HttpGet]
        [Route("totalOutstanding")]
        public async Task<ActionResult<decimal>> GetTotalOutstanding([FromQuery] string dateThreshold)
        {
            var totalOutstanding = await _repository.GetTotalOutstanding(dateThreshold);
            return Ok(totalOutstanding);
        }


        //payments by type
        [HttpGet]
        [Route("paymentsByType")]
        public async Task<ActionResult<IEnumerable<FinancialReportViewModel>>> GetPaymentsByType([FromQuery] string dateThreshold)
        {
            var paymentsByType = await _repository.GetPaymentsByType(dateThreshold);
            return Ok(paymentsByType);
        }


        [HttpGet]
        [Route("getUsersName")]
        public async Task<ActionResult<string>> GetGeneratorsName([FromQuery] int userID)
        {

            var user = await _repository.GetReportGenerator(userID);
            return Ok(user);
        }


        //audit trail
        [HttpGet]
        [Route("auditTrail")]
        public async Task<ActionResult<IEnumerable<Audit_Trail>>> GetAUditTrailData([FromQuery] string dateThreshold)
        {
            var auditTrail = await _repository.GetAuditTrailData(dateThreshold);
            return Ok(auditTrail);
        }




        //Dashboard
        [HttpGet]
        [Route("salesDashboard")]
        public Task<IEnumerable<DashboardViewModel>> GetSalesData([FromQuery] string filter)
        {
            var salesData = _repository.GetSalesData(filter);

            return salesData;
        }

        [HttpGet]
        [Route("popular-products")]
        public Task<IEnumerable<DashboardViewModel>> GetPopularProducts([FromQuery] string filter)
        {
            var popularProducts = _repository.GetPopularProducts(filter);

            return popularProducts;
        }


        [HttpGet]
        [Route("subscriptions")]
        public Task<IEnumerable<DashboardViewModel>> GetSubscriptionData([FromQuery] string filter)
        {
            var subscriptionData = _repository.GetSubscriptionData(filter);
            return subscriptionData;
        }

        [HttpGet]
        [Route("top-members")]
        public async Task<IActionResult> GetTopMembers([FromQuery] string filter)
        {
            var topMembers = await _repository.GetTopMembers(filter);
            return Ok(new { members = topMembers });
        }
    }
}
