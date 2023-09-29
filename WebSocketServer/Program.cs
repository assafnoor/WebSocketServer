using WebSocketServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();


app.UseWebSockets();                            // Aceptar WebSockets
//app.UseMiddleware<MiControladorDeWebSockets>();  // Controlador para WebSockets
app.UseMiddleware<SendNotifications>();  // Controlador para WebSockets


app.MapControllers();

app.Run();
