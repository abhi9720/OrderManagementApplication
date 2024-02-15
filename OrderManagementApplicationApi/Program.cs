using OrderManagementApplicationApi.Data;
using Microsoft.EntityFrameworkCore;
using Mapster;
using OrderManagementApplicationApi.DTOs;
using OrderManagementApplicationApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Connecting to SQL Server 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();




// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

TypeAdapterConfig<Customer, CustomerDto>.NewConfig();
TypeAdapterConfig<Product, ProductDto>.NewConfig();

TypeAdapterConfig<CustomerDto, Customer>.NewConfig()
    .Map(dest => dest.Name, src =>"Mr. "+ src.fName + "  " + src.lName);
// unqiue -  methodtype + url
app.MapPost("/api/customers", async (CustomerDto customerDto, AppDbContext dbContext) =>
{
    // Converting and saving to db
    var customer = customerDto.Adapt<Customer>();
    dbContext.Customers.Add(customer);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/customers/{customer.CustomerId}", customer.CustomerId);
});

// Define the API endpoint to get all customers
app.MapGet("/api/customers", async (AppDbContext dbContext) =>
{
    // Query the database to fetch all customers
    var customers = await dbContext.Customers.ToListAsync();

    // Map customers to DTOs
    var customerDtos = customers.Adapt<List<CustomerResponseDto>>();

    // Return the mapped DTOs as the response
    return Results.Ok(customers);
});




app.MapPost("/api/products", async (ProductDto productDto, AppDbContext dbContext) =>
{
    var product = productDto.Adapt<Product>();
    dbContext.Products.Add(product);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/api/products/{product.ProductId}", product.Adapt<ProductDto>());
});

app.MapGet("/api/products", async (AppDbContext dbContext) =>
{
    var products = await dbContext.Products.ToListAsync();
    var productDtos = products.Adapt<List<ProductDto>>();
    return Results.Ok(productDtos);
});



app.MapPost("/api/orders", async (PlaceOrderDto placeOrderDto, AppDbContext dbContext) =>
{
    var order = placeOrderDto.Adapt<Order>();
    dbContext.Orders.Add(order);
    await dbContext.SaveChangesAsync();

    // Fetch the customer name and order items
    var customer = await dbContext.Customers.FindAsync(order.CustomerId);
    var orderItems = order.OrderItems.ToList(); // Materialize the order items
    var orderItemDtos = orderItems.Adapt<List<OrderItemDto>>();


    var productIds = orderItems.Select(item => item.ProductId).ToList();
    // Fetch product prices
    var productPrices = await dbContext.Products
        .Where(p => productIds.Contains(p.ProductId))
        .Select(p => new { p.ProductId, p.Price })
        .ToDictionaryAsync(p => p.ProductId, p => p.Price);


    TypeAdapterConfig<Order, BillDto>.NewConfig()
       .Map(dest => dest.OrderItems, src => orderItemDtos)
       .Map(dest => dest.OrderTotal, src => src.OrderItems.Sum(item => item.Quantity * productPrices[item.ProductId]));


    // Map to BillDto using Mapster
    var billDto = order.Adapt<BillDto>();

    billDto.CustomerName = customer.Name;


    return Results.Created($"/api/orders/{order.OrderId}", billDto);
});


// Define the API endpoint to get all orders of a customer
app.MapGet("/api/customers/{customerId}/orders", async (int customerId, AppDbContext dbContext) =>
{
    // Query the database to fetch orders of the customer
    var orders = await dbContext.Orders
                                .Include(o => o.Customer) // Include related customer
                                .Include(o => o.OrderItems) // Include related order items
                                    .ThenInclude(oi => oi.Product) // Include related product for each order item
                                .Where(o => o.CustomerId == customerId)
                                .ToListAsync();

    // Mapster configuration for Order to BillDto
    TypeAdapterConfig<Order, BillDto>.NewConfig()
        .Map(dest => dest.OrderItems, src => src.OrderItems.Select(oi => oi.Adapt<OrderItemDto>()).ToList())
        .Map(dest => dest.CustomerName, src => src.Customer.Name)
        .Map(dest => dest.OrderTotal, src => src.OrderItems.Sum(oi => oi.Quantity * oi.Product.Price));

    // Map orders to DTOs
    var orderDtos = orders.Adapt<List<BillDto>>();

    // Return the mapped DTOs as the response
    return Results.Ok(orderDtos);
});


app.Run();
