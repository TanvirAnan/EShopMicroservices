using BuildingBlocks.CQRS;
using Catalog.API.Models;
using FluentValidation;
using Marten;

namespace Catalog.API.Products.DeleteAllProducts;

public record DeleteAllProductsCommand() : ICommand<DeleteAllProductsResult>;
public record DeleteAllProductsResult(bool IsSuccess);

internal class DeleteAllProductsHandler
    (IDocumentSession session)
    : ICommandHandler<DeleteAllProductsCommand, DeleteAllProductsResult>
{
    public async Task<DeleteAllProductsResult> Handle(DeleteAllProductsCommand command, CancellationToken cancellationToken)
    {
    
        session.DeleteWhere<Product>(_ => true);

        await session.SaveChangesAsync(cancellationToken);

        return new DeleteAllProductsResult(true);
    }
}
