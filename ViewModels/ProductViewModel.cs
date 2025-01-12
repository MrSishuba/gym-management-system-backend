namespace av_motion_api.ViewModels
{
    public class ProductViewModel
    { 
        public string Product_Name { get; set; }
        public string Product_Description { get; set; }
        public IFormFile Product_Img { get; set; }
        public int Quantity { get; set; }
        public decimal Unit_Price { get; set; }
        public decimal? Purchase_Price { get; set; }
        public string Size { get; set; }
        public int Product_Category_ID { get; set; }
        public int Product_Type_ID { get; set; }
    }
}
