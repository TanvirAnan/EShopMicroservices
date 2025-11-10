using BuildingBlocks.Behaviors;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Exceptions.Handler;
using Carter;
using Catalog.API.Data;
using FluentValidation;
using HealthChecks.UI.Client;
using Marten;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var assembly = typeof(Program).Assembly;
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

if (builder.Environment.IsDevelopment())
    builder.Services.InitializeMartenWith<CatalogInitialData>();

builder.Services.AddValidatorsFromAssembly(assembly);

builder.Services.AddCarter();

builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("Database")!);
}).UseLightweightSessions();

builder.Services.AddExceptionHandler<CustomExceptionHandler>();


builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);


var app = builder.Build();

app.MapCarter();

app.UseHealthChecks("/health",
    new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

app.UseExceptionHandler(options => { });


//app.UseExceptionHandler(exceptionHandlerApp =>
//{
//    exceptionHandlerApp.Run(async context =>
//    {
//        var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;

//        // Default Status Code and Type for general errors
//        var statusCode = StatusCodes.Status500InternalServerError;
//        var title = "An internal server error occurred.";
//        var details = exception?.Message;
//        object? errors = null; // Used for ValidationException

//        // Determine Status Code and structured response based on exception type
//        switch (exception)
//        {
//            case ValidationException validationException:
//                statusCode = StatusCodes.Status400BadRequest;
//                title = "One or more validation errors occurred.";
//                errors = validationException.Errors;
//                break;

//            case BadRequestException badRequestException:
//                statusCode = StatusCodes.Status400BadRequest;
//                title = "Bad Request.";
//                details = badRequestException.Message;
//                break;

//            case NotFoundException notFoundException:
//                statusCode = StatusCodes.Status404NotFound;
//                title = "Resource Not Found.";
//                details = notFoundException.Message;
//                break;

//            case InternalServerException internalServerException:
//                statusCode = StatusCodes.Status500InternalServerError;
//                title = "Internal Server Error.";
//                details = internalServerException.Details ?? internalServerException.Message;
//                break;
//        }

//        // Set the response status code and content type
//        context.Response.StatusCode = statusCode;
//        context.Response.ContentType = System.Net.Mime.MediaTypeNames.Application.Json;

//        // Write the structured error response
//        await context.Response.WriteAsJsonAsync(new
//        {
//            Title = title,
//            Status = statusCode,
//            Detail = details,
//            Errors = errors // This will only be non-null for ValidationException
//        });
//    });
//});



app.Run();
