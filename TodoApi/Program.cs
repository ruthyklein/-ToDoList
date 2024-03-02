using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using TodoApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"), new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();

app.UseRouting();

// Enable CORS
app.UseCors();

app.UseAuthorization();

app.MapControllers();


// Fetching all tasks
app.MapGet("/tasks", async (ToDoDbContext dbcontext,HttpContext context) =>
{
    var tasks = await dbcontext.Items.ToListAsync();
    await context.Response.WriteAsJsonAsync(tasks);
});

// Adding a new task
app.MapPost("/tasks", async (ToDoDbContext dbContext, HttpContext context, Item item) =>
{
    dbContext.Items.Add(item);
    await dbContext.SaveChangesAsync();
    context.Response.StatusCode = StatusCodes.Status201Created;
    await context.Response.WriteAsJsonAsync(item);
});

// Updating a task
app.MapPut("/tasks/{id}", async (ToDoDbContext dbContext, HttpContext context, int id, Item updatedItem) =>
{
    if (updatedItem == null)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Invalid task data");
        return;
    }

    var existingItem = await dbContext.Items.FindAsync(id);
    if (existingItem == null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync($"Task with ID {id} not found");
        return;
    }

    if (updatedItem.Name != null)
    {
        existingItem.Name = updatedItem.Name;
    }

    existingItem.IsComplete = updatedItem.IsComplete;

    await dbContext.SaveChangesAsync();
    context.Response.StatusCode = StatusCodes.Status200OK;
    await context.Response.WriteAsJsonAsync(existingItem);
});

// Deleting a task
app.MapDelete("/tasks/{id}", async (ToDoDbContext dbContext, HttpContext context, int id) =>
{
    var existingItem = await dbContext.Items.FindAsync(id);
    if (existingItem == null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    dbContext.Items.Remove(existingItem);
    await dbContext.SaveChangesAsync();
    context.Response.StatusCode = StatusCodes.Status200OK;
});

app.Run();