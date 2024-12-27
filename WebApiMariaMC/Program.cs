using Data.Models;
using Logic.ILogic;
using Logic.Logic;
using Logic.Session;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WebApiMariaMC.IServicies;
using WebApiMariaMC.Servicies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

/* Clase 9: El siguiente fragmento (invocación) es una modificación al método 
 * builder.Services.AddSwaggerGen() para agregar el Sagger el botón de 
 * Authorize.
 */
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Maria MC",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddHttpClient<MercadoPagoService>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<ICompraService, CompraService>();
builder.Services.AddScoped<ICierreDeCajaService, CierreDeCajaService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ISendMailService, SendMailService>();
builder.Services.AddScoped<AfipService, AfipService>();

builder.Services.AddScoped<IUserLogic, UserLogic>();
builder.Services.AddScoped<IVentaLogic, VentaLogic>();
builder.Services.AddScoped<ICompraLogic, CompraLogic>();
builder.Services.AddScoped<ICierreDeCajaLogic, CierreDeCajaLogic>();
builder.Services.AddScoped<IProductoLogic, ProductoLogic>();
builder.Services.AddScoped<ISendMailLogic, SendMailLogic>();

builder.Services.AddScoped<ISessionLogic, SessionLogic>();
builder.Services.Configure<Jwt>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddDbContext<Maria_MCContext>(
        options => options.UseSqlServer("name=ConnectionStrings:ServiceContext"));

/* Clase 9: El siguiente fragmento (invocación) de datos configura el 
 * comportamiento de la autentificación basada en JWT.
 * Los datos para configurar el compotamiento se toman desde el appsettings,
 * por lo tanto es necesario agregar al appsettings la siguiente sección
 * "Jwt": {
 *   "Key": "Contraseña secreta para generar la clave simétrica",
 *   "Issuer": "Emisor: URL del que provee el token, por ejemplo: http://localhost:5063",
 *   "Audience": "Usuario: URL del que provee el token, por ejemplo: http://localhost:5063",
 *   "Subject":  "Tema del token"
 * }
 * 
 * Recordatorio: para usar esta característica hay que usar la dependencia: Microsoft.AspNetCore.Authentication.JwtBearer
 */
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Clase 9: Se indica que hay que verificar la firma a partir de la clave configurada.
            ValidateIssuerSigningKey = true,
            // Clase 9: Se crea la clave de cifrado simétrico a partir de la contraseña
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "")),

            // Clase 9: Se indica que hay que verificar el Issuer
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            // Clase 9: Se indica que hay que verificar la Audience
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            // Clase 9: Se indica que hay que verificar el tiempo de validez
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

// Agregar servicios CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins("http://localhost:5173") // Agrega la URL de tu frontend
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// Agregar el uso de CORS
app.UseCors("AllowSpecificOrigin");

app.UseAuthentication();
app.UseAuthorization();



// Configura la carpeta 'Portraits' como una carpeta estática
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "Portraits")),
    RequestPath = "/Portraits"
});

app.MapControllers();

app.Run();

