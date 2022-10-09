using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Infrastructure.Context;
using AGooday.AgPay.Merchant.Api.Extensions;
using AGooday.AgPay.Merchant.Api.Extensions.AuthContext;
using AGooday.AgPay.Merchant.Api.Middlewares;
using AGooday.AgPay.Merchant.Api.Models;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var logging = builder.Logging;
// ���� ClearProviders �Դ���������ɾ������ ILoggerProvider ʵ��
logging.ClearProviders();
//// ͨ������־����Ӧ��������ָ�����������ڴ�����ָ����
//logging.AddFilter("Microsoft", LogLevel.Warning);
// ���ӿ���̨��־��¼�ṩ����
logging.AddConsole();

// Add services to the container.
var services = builder.Services;
var Env = builder.Environment;

services.AddSingleton(new Appsettings(Env.ContentRootPath));

//// ע����־
//services.AddLogging(config =>
//{
//    //Microsoft.Extensions.Logging.Log4Net.AspNetCore
//    config.AddLog4Net();
//});
services.AddSingleton<ILoggerProvider, Log4NetLoggerProvider>();

#region Redis
//redis����
var section = builder.Configuration.GetSection("Redis:Default");
//�����ַ���
string _connectionString = section.GetSection("Connection").Value;
//ʵ������
string _instanceName = section.GetSection("InstanceName").Value;
//Ĭ�����ݿ� 
int _defaultDB = int.Parse(section.GetSection("DefaultDB").Value ?? "0");
services.AddSingleton(new RedisUtil(_connectionString, _instanceName, _defaultDB));
#endregion

services.AddCors(o =>
    o.AddPolicy("CorsPolicy",
        builder => builder
            .WithOrigins("http://localhost:9001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            //.AllowAnyOrigin()
            .AllowCredentials()
    ));

services.AddMemoryCache();
services.AddHttpContextAccessor();
services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
services.Configure<JwtSettings>(jwtSettingsSection);
// JWT
var appSettings = jwtSettingsSection.Get<JwtSettings>();
services.AddJwtBearerAuthentication(appSettings);

// Automapper ע��
services.AddAutoMapperSetup();
services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        //https://blog.poychang.net/using-newtonsoft-json-in-asp-net-core-projects/
        //options.SerializerSettings.Formatting = Formatting.Indented;
        //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();//Json key ���ַ�Сд�����շ�תС�շ壩
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

// Adding MediatR for Domain Events
// ������������¼���ע��
// ���ð� MediatR.Extensions.Microsoft.DependencyInjection
//services.AddMediatR(typeof(MyxxxHandler));//����ע��ĳһ����������
//��
services.AddMediatR(typeof(Program));//Ŀ����Ϊ��ɨ��Handler��ʵ�ֶ������ӵ�IOC��������

// .NET Core ԭ������ע��
// ��дһ�����������������չʾ�� Presentation �и���
NativeInjectorBootStrapper.RegisterServices(services);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCalculateExecutionTime();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseCors("CorsPolicy");

var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
AuthContextService.Configure(httpContextAccessor);

app.UseAuthorization();

app.UseExceptionHandling();

app.UseRequestResponseLogging();

app.MapControllers();

app.Run();