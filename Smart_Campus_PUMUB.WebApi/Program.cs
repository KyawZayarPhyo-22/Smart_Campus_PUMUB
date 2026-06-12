using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddDbContext<SmartCampusDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Add services to the container.

// builder.Services.AddControllers();
// 🛠️ ဒီနေရာလေးကို ရှာပြီး အောက်ပါအတိုင်း ပြင်ပေးပါ
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Object အချင်းချင်း အပြန်အလှန် ပတ်ညွှန်းနေတာတွေကို Serializer က ကျော်သွားခိုင်းမည့် Logic
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<SmartCampusDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// builder.Services.AddDbContext<SmartCampusDbContext>(options =>
//     options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles(); 

app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
