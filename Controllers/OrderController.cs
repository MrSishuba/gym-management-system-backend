using av_motion_api.Data;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using EllipticCurve.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        //Products
        [HttpGet]
        [Route("GetAllProducts")]        
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _context.Products.ToListAsync();

                return Ok(products);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }


        [HttpGet]
        [Route("GetProductById/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                return Ok(product);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        //Cart
        [HttpGet]
        [Route("GetCart")]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirstValue("userId");

            if (userId == null)
            {
                return Unauthorized("User not logged in");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == int.Parse(userId));
            if (member == null)
            {
                return Unauthorized("User is not a member");
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.Member_ID == member.Member_ID);
            if (cart == null)
            {
                return Ok(new List<CartItemViewModel>()); // Return an empty list if no cart exists
            }

            var cartItems = await _context.Cart_Items
                .Where(c => c.Cart_ID == cart.Cart_ID)
                .Select(c => new CartItemViewModel
                {
                    Product_ID = c.Product_ID,
                    Product_Name = c.Product.Product_Name,
                    Product_Description = c.Product.Product_Description,
                    Product_Img = c.Product.Product_Img,
                    Quantity = c.Quantity,
                    Unit_Price = c.Product.Unit_Price,
                    Size = c.Product.Size
                })
                .ToListAsync();

            return Ok(cartItems);
        }

        [HttpPost]
        [Route("AddToCart")]
        public async Task<ActionResult<Cart_Item>> AddToCart([FromBody] CartViewModel cv)
        {
            var userId = User.FindFirstValue("userId");

            if (userId == null)
            {
                return Unauthorized("User not logged in");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == int.Parse(userId));
            if (member == null)
            {
                return Unauthorized("User is not a member");
            }

            // Retrieve the product
            var product = await _context.Products.FindAsync(cv.Product_ID);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            // Check if the requested quantity exceeds the available quantity
            if (cv.Quantity > product.Quantity)
            {
                return BadRequest($"Requested quantity ({cv.Quantity}) exceeds available quantity ({product.Quantity})");
            }

            // Retrieve or create the member's cart
            var cart = await _context.Carts
                .Include(c => c.Cart_Items)
                .FirstOrDefaultAsync(c => c.Member_ID == member.Member_ID);

            if (cart == null)
            {
                cart = new Cart
                {
                    Member_ID = member.Member_ID,
                    Cart_Items = new List<Cart_Item>()
                };
                _context.Carts.Add(cart);

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail for creating a new cart
                var cartAudit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"New Cart Created: For '{member.User.Name}', Member ID '{member.Member_ID}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Cart),
                    Timestamp = DateTime.UtcNow
                };

                _context.Audit_Trails.Add(cartAudit);

            }

            // Check if the product is already in the cart
            var existingCartItem = cart.Cart_Items.FirstOrDefault(c => c.Product_ID == cv.Product_ID);

            if (existingCartItem != null)
            {
                // If the product is already in the cart, check the total quantity after adding the new quantity
                if (existingCartItem.Quantity + cv.Quantity > product.Quantity)
                {
                    return BadRequest($"This quantity exceeds available stock. Current quantity in cart: {existingCartItem.Quantity}, available quantity: {product.Quantity}");
                }
                existingCartItem.Quantity += cv.Quantity;

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail for updating an existing cart item
                var cartItemAudit = new Audit_Trail
                {
                    Transaction_Type = "UPDATE",
                    Critical_Data = $"Cart Item Updated: Product ID '{existingCartItem.Product_ID}', New Quantity '{existingCartItem.Quantity}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Cart_Item),
                    Timestamp = DateTime.UtcNow
                };

                _context.Audit_Trails.Add(cartItemAudit);

            }
            else
            {
                // If the product is not in the cart, add it as a new item
                var cartItem = new Cart_Item
                {
                    Product_ID = cv.Product_ID,
                    Quantity = cv.Quantity,
                    Cart = cart
                };
                cart.Cart_Items.Add(cartItem);

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail for inserting a new cart item
                var cartItemAudit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"New Cart Item Added: Product ID '{cartItem.Product_ID}', Quantity '{cartItem.Quantity}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Cart_Item),
                    Timestamp = DateTime.UtcNow
                };

                _context.Audit_Trails.Add(cartItemAudit);

                await _context.SaveChangesAsync();  // Ensure the cart item is saved and ID is generated
                return Ok(cartItem);  // Return the newly added cart item
            }

            await _context.SaveChangesAsync();  // Ensure the cart item is saved and ID is updated
            return Ok(existingCartItem);  // Return the updated cart item
        }

        [HttpPut]
        [Route("UpdateCart")]
        public async Task<IActionResult> UpdateCart([FromBody] CartViewModel cv)
        {
            var userId = User.FindFirstValue("userId");

            if (userId == null)
            {
                return Unauthorized("User not logged in");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == int.Parse(userId));
            if (member == null)
            {
                return Unauthorized("User is not a member");
            }

            var cart = await _context.Carts
                .Include(c => c.Cart_Items)
                .FirstOrDefaultAsync(c => c.Member_ID == member.Member_ID);

            if (cart == null)
            {
                return NotFound("Cart not found");
            }

            var cartItem = await _context.Cart_Items
                .FirstOrDefaultAsync(c => c.Product_ID == cv.Product_ID && c.Cart_ID == cart.Cart_ID);

            if (cartItem == null)
            {
                return NotFound("Product not found in the cart");
            }

            // Check if the requested quantity is valid
            var product = await _context.Products.FindAsync(cv.Product_ID);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            if (cv.Quantity > product.Quantity)
            {
                return BadRequest($"Requested quantity ({cv.Quantity}) exceeds available quantity ({product.Quantity})");
            }

            // Log the current quantity before updating
            var oldQuantity = cartItem.Quantity;

            // Update the quantity of the cart item
            cartItem.Quantity = cv.Quantity;

            _context.Cart_Items.Update(cartItem);
            await _context.SaveChangesAsync();

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail for updating the cart item
            var cartItemAudit = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"Cart Item Updated: Product ID '{cartItem.Product_ID}', Quantity changed from '{oldQuantity}' to '{cartItem.Quantity}'",
                Changed_By = changedBy,
                Table_Name = nameof(Cart_Item),
                Timestamp = DateTime.UtcNow
            };

            _context.Audit_Trails.Add(cartItemAudit);
            await _context.SaveChangesAsync();  // Ensure the audit trail is saved

            return Ok(cartItem);
        }

        [HttpDelete]
        [Route("RemoveFromCart/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = User.FindFirstValue("userId");

            if (userId == null)
            {
                return Unauthorized("User not logged in");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == int.Parse(userId));
            if (member == null)
            {
                return Unauthorized("User is not a member");
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.Member_ID == member.Member_ID);
            if (cart == null)
            {
                return NotFound("Cart not found");
            }

            var cartItem = await _context.Cart_Items
                .FirstOrDefaultAsync(c => c.Product_ID == productId && c.Cart_ID == cart.Cart_ID);

            if (cartItem == null)
            {
                return NotFound("Product not found in the cart");
            }

            // Log the quantity before removing the item
            var removedQuantity = cartItem.Quantity;

            _context.Cart_Items.Remove(cartItem);
            await _context.SaveChangesAsync();

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail for removing the cart item
            var cartItemAudit = new Audit_Trail
            {
                Transaction_Type = "DELETE",
                Critical_Data = $"Cart Item Removed: Product ID '{cartItem.Product_ID}', Quantity removed '{removedQuantity}'",
                Changed_By = changedBy,
                Table_Name = nameof(Cart_Item),
                Timestamp = DateTime.UtcNow
            };

            _context.Audit_Trails.Add(cartItemAudit);
            await _context.SaveChangesAsync();  // Ensure the audit trail is saved

            return Ok("Product removed from the cart");
        }

        //Wishlist
        [HttpGet]
        [Route("GetWishlist")]
        public async Task<IActionResult> GetWishlist()
        {
            var userId = User.FindFirstValue("userId");

            if (userId == null)
            {
                return Unauthorized("User not logged in");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == int.Parse(userId));
            if (member == null)
            {
                return Unauthorized("User is not a member");
            }

            var wishlist = await _context.Wishlists.FirstOrDefaultAsync(w => w.Member_ID == member.Member_ID);
            if (wishlist == null)
            {
                return Ok(new List<WishlistItemViewModel>()); // Return an empty list if no wishlist exists
            }

            var wishlistItems = await _context.Wishlist_Items
                .Where(w => w.Wishlist_ID == wishlist.Wishlist_ID)
                .Select(w => new WishlistItemViewModel
                {
                    Product_ID = w.Product_ID,
                    Product_Name = w.Product.Product_Name,
                    Product_Description = w.Product.Product_Description,
                    Product_Img = w.Product.Product_Img,
                    Unit_Price = w.Product.Unit_Price,
                    Size = w.Size
                })
                .ToListAsync();

            return Ok(wishlistItems);
        }

        [HttpPost]
        [Route("AddToWishlist")]
        public async Task<ActionResult<Wishlist_Item>> AddToWishlist([FromBody] WishlistViewModel wv)
        {
            var userId = User.FindFirstValue("userId");

            if (userId == null)
            {
                return Unauthorized("User not logged in");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == int.Parse(userId));
            if (member == null)
            {
                return Unauthorized("User is not a member");
            }

            // Retrieve or create a wishlist for the member
            var wishlist = await _context.Wishlists.FirstOrDefaultAsync(w => w.Member_ID == member.Member_ID);
            if (wishlist == null)
            {
                wishlist = new Wishlist { Member_ID = member.Member_ID };
                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();
            }

            // Check if the product is already in the wishlist
            var existingWishlistItem = await _context.Wishlist_Items
                .FirstOrDefaultAsync(w => w.Product_ID == wv.Product_ID && w.Wishlist_ID == wishlist.Wishlist_ID);

            if (existingWishlistItem != null)
            {
                return Conflict("Product is already in the wishlist");
            }

            // If the product is not in the wishlist, add it as a new item
            var wishlistItem = new Wishlist_Item
            {
                Product_ID = wv.Product_ID,
                Wishlist_ID = wishlist.Wishlist_ID,
                Size = wv.Size
            };

            _context.Wishlist_Items.Add(wishlistItem);
            await _context.SaveChangesAsync();

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail for adding to wishlist
            var wishlistAudit = new Audit_Trail
            {
                Transaction_Type = "INSERT",
                Critical_Data = $"Product added to Wishlist: Product ID '{wishlistItem.Product_ID}', Size '{wishlistItem.Size}'",
                Changed_By = changedBy,
                Table_Name = nameof(Wishlist_Item),
                Timestamp = DateTime.UtcNow
            };

            _context.Audit_Trails.Add(wishlistAudit);
            await _context.SaveChangesAsync();  // Ensure the audit trail is saved

            return Ok(wishlistItem);
        }

        [HttpDelete]
        [Route("RemoveFromWishlist/{productId}")]
        public async Task<IActionResult> RemoveFromWishlist(int productId)
        {
            var userId = User.FindFirstValue("userId");

            if (userId == null)
            {
                return Unauthorized("User not logged in");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == int.Parse(userId));
            if (member == null)
            {
                return Unauthorized("User is not a member");
            }

            var wishlist = await _context.Wishlists.FirstOrDefaultAsync(w => w.Member_ID == member.Member_ID);
            if (wishlist == null)
            {
                return NotFound("Wishlist not found");
            }

            var wishlistItem = await _context.Wishlist_Items
                .FirstOrDefaultAsync(w => w.Product_ID == productId && w.Wishlist_ID == wishlist.Wishlist_ID);

            if (wishlistItem == null)
            {
                return NotFound("Product not found in the wishlist");
            }

            _context.Wishlist_Items.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail for removing from wishlist
            var wishlistAudit = new Audit_Trail
            {
                Transaction_Type = "DELETE",
                Critical_Data = $"Product removed from Wishlist: Product_ID '{wishlistItem.Product_ID}'",
                Changed_By = changedBy,
                Table_Name = nameof(Wishlist_Item),
                Timestamp = DateTime.UtcNow
            };

            _context.Audit_Trails.Add(wishlistAudit);
            await _context.SaveChangesAsync();  // Ensure the audit trail is saved

            return Ok("Product removed from the wishlist");
        }

        [HttpPost]
        [Route("MoveFromWishlistToCart")]
        public async Task<IActionResult> MoveFromWishlistToCart([FromBody] CartViewModel model)
        {
            var userId = User.FindFirstValue("userId");

            if (userId == null)
            {
                return Unauthorized("User not logged in");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == int.Parse(userId));
            if (member == null)
            {
                return Unauthorized("User is not a member");
            }

            var wishlist = await _context.Wishlists.FirstOrDefaultAsync(w => w.Member_ID == member.Member_ID);
            if (wishlist == null)
            {
                return NotFound("Wishlist not found");
            }

            var wishlistItem = await _context.Wishlist_Items
                .FirstOrDefaultAsync(w => w.Product_ID == model.Product_ID && w.Wishlist_ID == wishlist.Wishlist_ID);

            if (wishlistItem == null)
            {
                return NotFound("Product not found in the wishlist");
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Product_ID == model.Product_ID);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            // Check if the requested quantity exceeds available stock
            if (model.Quantity > product.Quantity)
            {
                return BadRequest("Requested quantity exceeds available stock");
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.Member_ID == member.Member_ID);
            if (cart == null)
            {
                cart = new Cart { Member_ID = member.Member_ID };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingCartItem = await _context.Cart_Items
                .FirstOrDefaultAsync(c => c.Product_ID == model.Product_ID && c.Cart_ID == cart.Cart_ID);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += model.Quantity;
                if (existingCartItem.Quantity > product.Quantity)
                {
                    return BadRequest("Total quantity in cart exceeds available stock");
                }
                _context.Cart_Items.Update(existingCartItem);
            }
            else
            {
                var cartItem = new Cart_Item
                {
                    Product_ID = model.Product_ID,
                    Quantity = model.Quantity,
                    Cart_ID = cart.Cart_ID
                };

                _context.Cart_Items.Add(cartItem);
            }

            _context.Wishlist_Items.Remove(wishlistItem);

            await _context.SaveChangesAsync();

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail for moving product from wishlist to cart
            var auditTrail = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"Product moved from Wishlist to Cart: Product ID '{model.Product_ID}', Quantity '{model.Quantity}'",
                Changed_By = changedBy,
                Table_Name = $"{nameof(Wishlist_Item)} to {nameof(Cart_Item)}",
                Timestamp = DateTime.UtcNow
            };

            _context.Audit_Trails.Add(auditTrail);
            await _context.SaveChangesAsync();  // Ensure the audit trail is saved

            return Content("Product moved from wishlist to cart", "text/plain");
        }


        //Order
        [HttpGet]
        [Route("GetMemberOrders")]
        public async Task<IActionResult> GetMemberOrders()
        {
            var userId = User.FindFirstValue("userId");

            if (userId == null)
            {
                return Unauthorized("User not logged in");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == int.Parse(userId));
            if (member == null)
            {
                return Unauthorized("User is not a member");
            }

            var orders = await _context.Orders
                .Where(o => o.Member_ID == member.Member_ID)
                .Include(o => o.Order_Status)
                .Include(o => o.Order_Lines)
                .ThenInclude(od => od.Product)
                .ToListAsync();

            var orderViewModels = orders.Select(order => new OrderViewModel
            {
                Order_ID = order.Order_ID,
                Order_Date = order.Order_Date,
                Order_Status_ID = order.Order_Status.Order_Status_ID,
                IsCollected = order.IsCollected,
                OrderLines = order.Order_Lines.Select(ol => new OrderLineViewModel
                {
                    Order_Line_ID = ol.Order_Line_ID,
                    Product_ID = ol.Product_ID,
                    Product_Name = ol.Product.Product_Name,
                    Quantity = ol.Quantity,
                    Unit_Price = ol.Unit_Price,
                }).ToList(),
                Total_Price = order.Order_Lines.Sum(ol => ol.Quantity * ol.Unit_Price)
            }).ToList();

            return Ok(orderViewModels);
        }

        [HttpGet]
        [Route("GetOrders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Order_Status)
                .Include(o => o.Order_Lines)
                .ThenInclude(od => od.Product)
                .ToListAsync();

            var orderViewModels = orders.Select(order => new OrderViewModel
            {
                Order_ID = order.Order_ID,
                Member_ID = order.Member_ID,
                Order_Date = order.Order_Date,
                Order_Status_ID = order.Order_Status.Order_Status_ID,
                IsCollected = order.IsCollected,
                OrderLines = order.Order_Lines.Select(ol => new OrderLineViewModel
                {
                    Order_Line_ID = ol.Order_Line_ID,
                    Product_ID = ol.Product_ID,
                    Product_Name = ol.Product.Product_Name,
                    Quantity = ol.Quantity,
                    Unit_Price = ol.Unit_Price,
                }).ToList(),
                Total_Price = order.Order_Lines.Sum(ol => ol.Quantity * ol.Unit_Price)
            }).ToList();

            return Ok(orderViewModels);
        }

        [HttpPost]
        [Route("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderViewModel ov)
        {
            // Validate input
            if (ov == null || ov.OrderLines == null || !ov.OrderLines.Any())
            {
                return BadRequest("OrderViewModel or OrderLines field is required.");
            }

            // Extract user ID from claims
            var userId = User.FindFirstValue("userId");
            if (userId == null)
            {
                return Unauthorized("User not logged in");
            }

            // Retrieve member
            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == int.Parse(userId));
            if (member == null)
            {
                return Unauthorized("User is not a member");
            }

            // Begin a transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create and save the order
                var order = new Order
                {
                    Member_ID = member.Member_ID,
                    Order_Date = DateTime.UtcNow,
                    Order_Status_ID = ov.Order_Status_ID, // Use the Order_Status_ID from the view model
                    IsCollected = ov.IsCollected,
                    Total_Price = ov.Total_Price // Ensure Total_Price is also set
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Save to get Order_ID

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Add audit trail for order creation
                var audit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Order Created: Order ID '{order.Order_ID}', Total Price '{order.Total_Price}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Order),
                    Timestamp = DateTime.UtcNow
                };

                _context.Audit_Trails.Add(audit);
                await _context.SaveChangesAsync();

                // Add order lines and update product quantities
                foreach (var orderLineViewModel in ov.OrderLines)
                {
                    var product = await _context.Products.FindAsync(orderLineViewModel.Product_ID);
                    if (product == null)
                    {
                        return BadRequest($"Product with ID {orderLineViewModel.Product_ID} not found.");
                    }

                    if (product.Quantity < orderLineViewModel.Quantity)
                    {
                        return BadRequest($"Insufficient quantity for product {product.Product_Name}.");
                    }

                    // Decrease product quantity
                    product.Quantity -= orderLineViewModel.Quantity;

                    // Update the corresponding inventory item
                    var inventoryItem = await _context.Inventory
                        .FirstOrDefaultAsync(i => i.Product_ID == orderLineViewModel.Product_ID);

                    if (inventoryItem != null)
                    {
                        inventoryItem.Inventory_Item_Quantity -= orderLineViewModel.Quantity;
                    }

                    var orderLine = new Order_Line
                    {
                        Product_ID = orderLineViewModel.Product_ID,
                        Quantity = orderLineViewModel.Quantity,
                        Unit_Price = orderLineViewModel.Unit_Price,
                        Order_ID = order.Order_ID // Link to the created order
                    };

                    _context.Order_Lines.Add(orderLine);

                    // Add audit trail for order line creation
                    var orderLineAudit = new Audit_Trail
                    {
                        Transaction_Type = "INSERT",
                        Critical_Data = $"Order Line Added: Product ID '{orderLineViewModel.Product_ID}', Quantity '{orderLineViewModel.Quantity}'",
                        Changed_By = changedBy,
                        Table_Name = nameof(Order_Line),
                        Timestamp = DateTime.UtcNow
                    };

                    _context.Audit_Trails.Add(orderLineAudit);
                }

                await _context.SaveChangesAsync(); // Save order lines and updated products

                // Clear the member's cart
                var cart = await _context.Carts
                    .Include(c => c.Cart_Items)
                    .FirstOrDefaultAsync(c => c.Member_ID == member.Member_ID);

                if (cart != null)
                {
                    _context.Cart_Items.RemoveRange(cart.Cart_Items);
                    await _context.SaveChangesAsync();

                    // Add audit trail for cart clearance
                    var cartAudit = new Audit_Trail
                    {
                        Transaction_Type = "DELETE",
                        Critical_Data = $"Cart Cleared for '{member.User.Name}',  Member_ID '{member.Member_ID}'",
                        Changed_By = changedBy,
                        Table_Name = nameof(Cart),
                        Timestamp = DateTime.UtcNow
                    };

                    _context.Audit_Trails.Add(cartAudit);
                }

                // Commit the transaction
                await transaction.CommitAsync();

                return Ok(order);
            }
            catch (Exception ex)
            {
                // Rollback the transaction if there's an error
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //Member
        [HttpPost]
        [Route("CollectOrder/{orderId}")]
        public async Task<IActionResult> CollectOrder(int orderId)
        {
            var userId = User.FindFirstValue("userId");

            if (userId == null)
            {
                return Unauthorized("User not logged in");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == int.Parse(userId));
            if (member == null)
            {
                return Unauthorized("User is not a member");
            }

            var order = await _context.Orders
                .Include(o => o.Order_Status)
                .FirstOrDefaultAsync(o => o.Order_ID == orderId && o.Member_ID == member.Member_ID);

            if (order == null)
            {
                return NotFound("Order not found");
            }

            if (order.Order_Date.AddDays(7) >= DateTime.UtcNow)
            {
                order.Order_Status_ID = 3; // Collected
            }
            else
            {
                order.Order_Status_ID = 4; // Late Collection
            }

            order.IsCollected = true;

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Add audit trail for the order collection
            var audit = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"Order collected for '{order.Member.User.Name}', Order ID '{order.Order_ID}'",
                Changed_By = changedBy,
                Table_Name = nameof(Order),
                Timestamp = DateTime.UtcNow
            };

            // Save the audit trail
            _context.Audit_Trails.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(order);
        }

        //Admin
        [HttpPost]
        [Route("OrderCollect/{orderId}")]
        public async Task<IActionResult> OrderCollect(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Order_Status)
                .FirstOrDefaultAsync(o => o.Order_ID == orderId);

            if (order == null)
            {
                return NotFound("Order not found");
            }

            if (order.Order_Date.AddDays(7) >= DateTime.UtcNow)
            {
                order.Order_Status_ID = 3; // Collected
            }
            else
            {
                order.Order_Status_ID = 4; // Late Collection
            }

            order.IsCollected = true;

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Add audit trail for the order collection
            var audit = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"Order collected for Member ID '{order.Member_ID}', Order ID '{order.Order_ID}'",
                Changed_By = changedBy,
                Table_Name = nameof(Order),
                Timestamp = DateTime.UtcNow
            };

            await _context.SaveChangesAsync();

            // Save the audit trail
            _context.Audit_Trails.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(order);
        }

        //Changed_By Methods
        private async Task<string> GetChangedByAsync()
        {
            var userId = User.FindFirstValue("userId");
            if (userId == null)
            {
                return "Unknown"; // Default value if userId is not available
            }

            // Convert userId to integer
            if (!int.TryParse(userId, out var parsedUserId))
            {
                return "Unknown";
            }

            // Retrieve the user
            var user = await _context.Users.FindAsync(parsedUserId);
            if (user == null)
            {
                return "Unknown";
            }

            // Check associated roles
            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.User_ID == user.User_ID);
            if (owner != null)
            {
                return $"{owner.User.Name} {owner.User.Surname} (Owner)";
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.User_ID == user.User_ID);
            if (employee != null)
            {
                return $"{employee.User.Name} {employee.User.Surname} (Employee)";
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == user.User_ID);
            if (member != null)
            {
                return $"{member.User.Name} {member.User.Surname} (Member)";
            }

            return "Unknown";
        }
    }
}
