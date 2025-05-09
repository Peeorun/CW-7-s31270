using System.Data.SqlClient;
using CW_7_s31270.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IDbService, DbService>();

builder.Services.AddSingleton(sp => {
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new SqlConnectionStringBuilder {
        DataSource = configuration["SqlSettings:Server"],
        InitialCatalog = configuration["SqlSettings:Database"],
        UserID = configuration["SqlSettings:User"],
        Password = configuration["SqlSettings:Password"]
    };
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();