# ─────────────────────────────────────────────────────────────────
# Stage 1: Build
# Sử dụng image SDK đầy đủ để compile source code .NET 9
# ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file và restore dependencies trước (tận dụng Docker cache layer)
# Chỉ định rõ project (không dùng *.csproj / để dotnet tự dò) vì thư mục này còn có
# DentalClinic.sln (kèm 3 project test) — nếu để dotnet tự dò sẽ báo lỗi "ambiguous project".
COPY DentalClinic.API.csproj ./
RUN dotnet restore DentalClinic.API.csproj

# Copy toàn bộ source code (bao gồm cả .sln + tests/) và tiến hành publish
COPY . ./
RUN dotnet publish DentalClinic.API.csproj -c Release -o /app/publish --no-restore

# ─────────────────────────────────────────────────────────────────
# Stage 2: Runtime
# Sử dụng image ASP.NET Runtime gọn nhẹ, không chứa SDK
# Giúp giảm kích thước image cuối từ ~800MB xuống còn ~250MB
# ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Tạo user non-root để chạy ứng dụng (bảo mật)
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Copy artifacts đã được publish từ stage build
COPY --from=build /app/publish .

# wwwroot/uploads CHƯA tồn tại sẵn (chỉ được code tự tạo lúc có upload đầu tiên) — tạo sẵn
# trước khi docker-compose mount volume vào đây, nếu không Docker sẽ tạo mount point với
# quyền root khi khởi tạo volume rỗng, khiến appuser không ghi được.
RUN mkdir -p wwwroot/uploads && chown -R appuser:appgroup /app

USER appuser

# Ứng dụng lắng nghe trên cổng 8080 (không phải 80 vì chạy với non-root user)
EXPOSE 8080

ENTRYPOINT ["dotnet", "DentalClinic.API.dll"]
