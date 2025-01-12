namespace av_motion_api.ViewModels
{
    public class CartViewModel
    {
        public int Product_ID { get; set; }

        public int Quantity { get; set; }
    }

    public class CartItemViewModel
    {
        public int Product_ID { get; set; }
        public string Product_Name { get; set; }
        public string Product_Description { get; set; }
        public string Product_Img { get; set; }
        public int Quantity { get; set; }
        public decimal Unit_Price { get; set; }
        public string Size { get; set; }
    }
}
