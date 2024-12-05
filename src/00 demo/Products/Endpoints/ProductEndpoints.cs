using SearchEntities;
using DataEntities;
using Microsoft.EntityFrameworkCore;
using Products.Data;
using System.Diagnostics;

namespace Products.Endpoints;

public static class ProductEndpoints
{
    /// <summary>
    /// Maps the product endpoints to the specified route builder.
    /// </summary>
    /// <param name="routes">The route builder to map the endpoints to.</param>
    public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Product");

        /// <summary>
        /// Gets all products.
        /// </summary>
        /// <param name="db">The product data context.</param>
        /// <returns>A list of all products.</returns>
        group.MapGet("/", async (ProductDataContext db) =>
        {
            return await db.Product.ToListAsync();
        })
        .WithName("GetAllProducts")
        .Produces<List<Product>>(StatusCodes.Status200OK);

        /// <summary>
        /// Gets a product by its ID.
        /// </summary>
        /// <param name="id">The ID of the product.</param>
        /// <param name="db">The product data context.</param>
        /// <returns>The product with the specified ID, or a 404 status if not found.</returns>
        group.MapGet("/{id}", async (int id, ProductDataContext db) =>
        {
            return await db.Product.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Id == id)
                is Product model
                    ? Results.Ok(model)
                    : Results.NotFound();
        })
        .WithName("GetProductById")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        /// <summary>
        /// Updates a product by its ID.
        /// </summary>
        /// <param name="id">The ID of the product to update.</param>
        /// <param name="product">The updated product data.</param>
        /// <param name="db">The product data context.</param>
        /// <returns>A 200 status if the update was successful, or a 404 status if not found.</returns>
        group.MapPut("/{id}", async (int id, Product product, ProductDataContext db) =>
        {
            var affected = await db.Product
                .Where(model => model.Id == id)
                .ExecuteUpdateAsync(setters => setters
                  .SetProperty(m => m.Id, product.Id)
                  .SetProperty(m => m.Name, product.Name)
                  .SetProperty(m => m.Description, product.Description)
                  .SetProperty(m => m.Price, product.Price)
                  .SetProperty(m => m.ImageUrl, product.ImageUrl)
                );

            return affected == 1 ? Results.Ok() : Results.NotFound();
        })
        .WithName("UpdateProduct")
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status204NoContent);

        /// <summary>
        /// Creates a new product.
        /// </summary>
        /// <param name="product">The product to create.</param>
        /// <param name="db">The product data context.</param>
        /// <returns>The created product with a 201 status.</returns>
        group.MapPost("/", async (Product product, ProductDataContext db) =>
        {
            db.Product.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/Product/{product.Id}", product);
        })
        .WithName("CreateProduct")
        .Produces<Product>(StatusCodes.Status201Created);

        /// <summary>
        /// Deletes a product by its ID.
        /// </summary>
        /// <param name="id">The ID of the product to delete.</param>
        /// <param name="db">The product data context.</param>
        /// <returns>A 200 status if the deletion was successful, or a 404 status if not found.</returns>
        group.MapDelete("/{id}", async (int id, ProductDataContext db) =>
        {
            var affected = await db.Product
                .Where(model => model.Id == id)
                .ExecuteDeleteAsync();

            return affected == 1 ? Results.Ok() : Results.NotFound();
        })
        .WithName("DeleteProduct")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        /// <summary>
        /// Searches for products by name.
        /// </summary>
        /// <param name="search">The search term.</param>
        /// <param name="db">The product data context.</param>
        /// <returns>A list of products that match the search term.</returns>
        group.MapGet("/search/{search}", async (string search, ProductDataContext db) =>
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            List<Product> products = await db.Product
            .Where(p => EF.Functions.Like(p.Name, $"%{search}%"))
            .ToListAsync();

            stopwatch.Stop();

            var response = new SearchResponse();
            response.Products = products;
            response.Response = products.Count > 0 ?
                $"{products.Count} Products found for [{search}]" :
                $"No products found for [{search}]";
            response.ElapsedTime = stopwatch.Elapsed;
            return response;
        })
            .WithName("SearchAllProducts")
            .Produces<List<Product>>(StatusCodes.Status200OK);
    }
}
