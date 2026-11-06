using FluentValidation;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Application.Catalog;

public sealed record ProductInput(string Code, string Description, ProductType Type, string Unit, decimal UnitPrice, Guid TaxRateId);

public sealed class ProductInputValidator : AbstractValidator<ProductInput>
{
    public ProductInputValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).PrecisionScale(18, 4, ignoreTrailingZeros: true);
        RuleFor(x => x.TaxRateId).NotEmpty();
    }
}

public static class ProductMappings
{
    public static ProductDto ToDto(this Product product) =>
        new(product.Id, product.Code, product.Description, product.Type, product.Unit, product.UnitPrice, product.TaxRateId);
}

public sealed class CreateProduct(IProductRepository products, ITaxRateRepository taxRates, IUnitOfWork unitOfWork)
{
    public async Task<Result<ProductDto>> HandleAsync(ProductInput input, CancellationToken cancellationToken)
    {
        if (!await taxRates.ExistsAsync(input.TaxRateId, cancellationToken))
        {
            return TaxErrors.NotFound(input.TaxRateId);
        }

        var product = Product.Create(input.Code, input.Description, input.Type, input.Unit, input.UnitPrice, input.TaxRateId);
        if (product.IsFailure)
        {
            return product.Error;
        }

        if (await products.CodeExistsAsync(product.Value.Code, cancellationToken))
        {
            return ProductErrors.DuplicateCode;
        }

        products.Add(product.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.Value.ToDto();
    }
}

/// <summary>The code is the product's identity in SAF-T, so it cannot change once created.</summary>
public sealed class UpdateProduct(IProductRepository products, ITaxRateRepository taxRates, IUnitOfWork unitOfWork)
{
    public async Task<Result<ProductDto>> HandleAsync(Guid id, ProductInput input, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(id);
        }

        if (!await taxRates.ExistsAsync(input.TaxRateId, cancellationToken))
        {
            return TaxErrors.NotFound(input.TaxRateId);
        }

        var updated = product.Update(input.Description, input.Type, input.Unit, input.UnitPrice, input.TaxRateId);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.ToDto();
    }
}

public sealed class DeleteProduct(IProductRepository products, IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(id);
        }

        products.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
