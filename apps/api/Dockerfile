# ─────────────────────────────────────────────────────────────────
# Stage 1: Build
# Sử dụng image SDK đầy đủ để compile source code .NET 9
# ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file và restore dependencies trước (tận dụng Docker cache layer)
COPY *.csproj ./
RUN dotnet restore

# Copy toàn bộ source code và tiến hành publish
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# ─────────────────────────────────────────────────────────────────
# Stage 2: Runtime
# Sử dụng image ASP.NET Runtime gọn nhẹ, không chứa SDK
# Giúp giảm kích thước image cuối từ ~800MB xuống còn ~250MB
# ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Tạo user non-root để chạy ứng dụng (bảo mật)
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

# Copy artifacts đã được publish từ stage build
COPY --from=build /app/publish .

# Ứng dụng lắng nghe trên cổng 8080 (không phải 80 vì chạy với non-root user)
EXPOSE 8080

ENTRYPOINT ["dotnet", "DentalClinic.API.dll"]
