# ASP.NET Core JWT Authentication API

ASP.NET Core 9 Web API ဥပမာ project ဖြစ်ပြီး ASP.NET Core Identity, SQL Server, JWT access token နှင့် refresh token များကို အသုံးပြုထားသည်။ Development environment တွင် Scalar API documentation ကိုလည်း ဖွင့်ပေးသည်။

## လိုအပ်ချက်များ

- .NET 9 SDK
- SQL Server (local သို့မဟုတ် remote instance)
- Entity Framework Core command-line tool (`dotnet-ef`)

## စတင်အသုံးပြုရန်

1. `AspnetcoreJwtTest/appsettings.json` သို့မဟုတ် `AspnetcoreJwtTest/appsettings.Development.json` တွင် `ConnectionStrings:DefaultConnection` ကို မိမိ SQL Server အတွက် ပြင်ဆင်ပါ။ JWT အတွက် `Jwt:Key`, `Jwt:Issuer` နှင့် `Jwt:Audience` တို့ကိုလည်း သတ်မှတ်ပါ။ Key သည် အနည်းဆုံး 16 characters ရှိရမည်။
2. Repository root မှ အောက်ပါ command များကို run ပါ။

   ```bash
   dotnet tool install --global dotnet-ef
   dotnet restore AspnetcoreJwtTest.sln
   dotnet ef database update --project AspnetcoreJwtTest/AspnetcoreJwtTest.csproj
   dotnet run --project AspnetcoreJwtTest/AspnetcoreJwtTest.csproj
   ```

   `dotnet-ef` ကို အရင်က install လုပ်ထားပြီးပါက install command ကို ကျော်နိုင်သည်။
3. Development environment မှာ API docs ကို `https://localhost:<port>/docs/scalar` တွင် ဖွင့်ပါ။ အသုံးပြုမည့် port ကို terminal ရှိ application output သို့မဟုတ် `AspnetcoreJwtTest/Properties/launchSettings.json` မှ ကြည့်နိုင်သည်။

## Configuration

အရေးကြီးသော configuration များကို environment variable ဖြင့်လည်း ထည့်နိုင်သည်။ .NET configuration key များတွင် `:` အစား `__` သုံးပါ။

```text
ConnectionStrings__DefaultConnection=<SQL Server connection string>
Jwt__Key=<အနည်းဆုံး 16 characters ရှိသော လျှို့ဝှက် key>
Jwt__Issuer=<token issuer>
Jwt__Audience=<token audience>
```

Password reset email ပို့ခြင်းအတွက် SMTP server နှင့် sender credentials ကို `AccountController` ရှိ `SendResetEmail` မှာ လက်ရှိ placeholder များအစား သတ်မှတ်ရန်လိုသည်။

## API endpoints

Base path: `/api/Account`

| Method | Path | ရည်ရွယ်ချက် |
| --- | --- | --- |
| `POST` | `/register` | User အသစ်ဖန်တီးရန်။ Body: `{"userName":"user1","email":"user@example.com","password":"StrongPassword1!","role":"User"}`။ `role` မပေးပါက `User` ကို သုံးသည်။ |
| `POST` | `/login` | Email နှင့် password ဖြင့် login ဝင်ပြီး access token နှင့် refresh token ရယူရန်။ Body: `{"email":"user@example.com","password":"StrongPassword1!"}` |
| `POST` | `/refresh` | သက်တမ်းကုန်သွားသော access token နှင့် refresh token ကို ပေး၍ token အသစ်ရယူရန်။ Body: `{"accessToken":"<expired-access-token>","refreshToken":"<refresh-token>"}` |
| `POST` | `/forgot-password` | Password reset email တောင်းရန်။ Body: `{"email":"user@example.com"}` |
| `POST` | `/reset-password` | Reset token ဖြင့် password အသစ်ထားရန်။ Body: `{"email":"user@example.com","token":"<reset-token>","newPassword":"NewStrongPassword1!"}` |
| `GET` | `/admin-only` | `Admin` role ရှိသော JWT ဖြင့်သာ အသုံးပြုနိုင်သည်။ Header: `Authorization: Bearer <access-token>` |

Login ရရှိသော access token သည် 1 မိနစ်၊ refresh token သည် 3 မိနစ် သက်တမ်းရှိသည်။ Login မအောင်မြင်မှု 3 ကြိမ်ရှိလျှင် Identity lockout ကို အသုံးပြုသည်။

## Development admin account

Application စတင်ချိန်တွင် `Admin` နှင့် `User` role များကို ဖန်တီးပြီး `admin@admin.com` အတွက် development admin account ကိုလည်း seed လုပ်သည်။ လက်ရှိ password သည် source code ထဲတွင် hard-coded ထားသော `Password@123` ဖြစ်သည်။ Local development အတွက်သာ အသုံးပြုပါ။ အများသုံး သို့မဟုတ် production environment တွင် မသုံးမီ seed logic နှင့် credential များကို ဖယ်ရှား/ပြောင်းလဲပြီး secrets များကို source control ထဲ မသိမ်းပါနှင့်။

## Project structure

- `AspnetcoreJwtTest/Controllers/` — API controllers နှင့် authentication endpoints
- `AspnetcoreJwtTest/Entities/` — Request models, Identity entities, application entities
- `AspnetcoreJwtTest/Services/` — Token builder နှင့် email sender services
- `AspnetcoreJwtTest/Data/` — Entity Framework database context
- `AspnetcoreJwtTest/Migrations/` — Database schema migrations
