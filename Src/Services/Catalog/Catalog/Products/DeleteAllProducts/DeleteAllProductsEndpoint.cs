using BuildingBlocks.CQRS;
using Carter;
using Catalog.API.Models;
using Catalog.API.Products.DeleteProduct;
using FluentValidation;
using Mapster;
using Marten;
using MediatR;

namespace Catalog.API.Products.DeleteAllProducts;


public record DeleteAllProductsResponse(bool IsSuccess);
public class DeleteAllProductsEndpoint:ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/products", async (ISender sender) =>
        {
            var result = await sender.Send(new DeleteAllProductsCommand());

            var response = result.Adapt<DeleteAllProductsResponse>();

            return Results.Ok(response);
        })
        .WithName("DeleteAllProducts")
        .Produces<DeleteAllProductsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Delete All Products")
        .WithDescription("Delete All Products");
    }


}

