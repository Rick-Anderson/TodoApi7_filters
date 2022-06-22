using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/todoitems", async (TodoDb db) =>
    await db.Todos.ToListAsync());

app.MapGet("/todoitems/complete", async (TodoDb db) =>
    await db.Todos.Where(t => t.IsComplete).ToListAsync());

app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
    await db.Todos.FindAsync(id)
        is Todo todo
            ? Results.Ok(todo)
            : Results.NotFound());

app.MapPost("/todoitems", async (Todo todo, TodoDb db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
});

//app.MapPut("/todoitems/{id}", async (int id, Todo inputTodo, TodoDb db) =>
//{
//    var todo = await db.Todos.FindAsync(id);

//    if (todo is null) return Results.NotFound();

//    todo.Name = inputTodo.Name;
//    todo.IsComplete = inputTodo.IsComplete;

//    await db.SaveChangesAsync();

//    return Results.NoContent();
//}).AddFilter((routeHandlerContext, next) => {
//    var parameters = routeHandlerContext.MethodInfo.GetParameters();
//    var anyTodos = parameters.FindIndex(parameter => parameter.ParameterType == typeof(Todo));
//    return async (invocationContext) =>
//    {
//        if (anyTodos >= 0)
//        {
//            var todoParameter = invocationContet.Parameters[anyTodos];
//            if (!IsValid(todoParameter))
//            {
//                return Results.Problem("The Todo is invalid.");
//            }
//        }
//        return await next(invocationContext);
//    };
//});

bool IsValid(Todo td)
{
    if (td.Id < 0 || td.Name!.Length < 3)
    {
        return false;
    }
    else
    {
        return true;
    }
}

app.MapPut("/todoitems/{id}", async (int id, Todo inputTodo, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return Results.NotFound();

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();
}).AddFilter(async (routeHandlerInvocationContext, next) =>
{
    var tdparam = (Todo)routeHandlerInvocationContext.Arguments[1]!;

    if (!IsValid(tdparam))
    {
        return Results.Problem("The Todo is invalid.");
    }
    return await next(routeHandlerInvocationContext);
});

app.MapDelete("/todoitems/{id}", async (int id, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.Ok(todo);
    }

    return Results.NotFound();
});

app.Run();

#region snippet_model
class Todo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
}
#endregion

#region snippet_cntx
class TodoDb : DbContext
{
    public TodoDb(DbContextOptions<TodoDb> options)
        : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();
}
#endregion
