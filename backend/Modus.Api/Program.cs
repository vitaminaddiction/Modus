using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Modus.Api.Auth;
using Modus.Api.Middleware;
using Modus.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── JWT 옵션 (dev면 SigningKey 미설정 시 개발용 키로 폴백) ──
var jwt = new JwtOptions();
builder.Configuration.GetSection(JwtOptions.SectionName).Bind(jwt);
if (string.IsNullOrWhiteSpace(jwt.SigningKey))
{
    jwt.SigningKey = builder.Environment.IsDevelopment()
        ? "modus-dev-only-signing-key-change-me-in-production-please"   // dev 전용(≥32B)
        : throw new InvalidOperationException("Jwt:SigningKey 설정이 필요합니다(운영).");
}
builder.Services.AddSingleton(Options.Create(jwt));

// ── 서비스 ──
builder.Services.AddModusInfrastructure(builder.Configuration);
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(o => o.AddPolicy("AllowFrontend", p =>
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;   // "sub"/"tenant" 등 클레임 타입 원형 유지
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        };
        options.Events = new JwtBearerEvents
        {
            // Authorization 헤더 없으면 access_token 쿠키에서 토큰 폴백
            OnMessageReceived = ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Token) &&
                    ctx.Request.Cookies.TryGetValue(AuthCookies.AccessToken, out var t))
                    ctx.Token = t;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();   // JWT 클레임 사용 위해 인증 뒤
app.UseMiddleware<CsrfMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
