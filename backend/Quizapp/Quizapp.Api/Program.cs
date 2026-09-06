using Quizapp.Application;
using Quizapp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(option => option.SwaggerEndpoint("/openapi/v1.json", "QuizApp"));
}

app.UseHttpsRedirection();
app.Run();
