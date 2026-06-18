# ─────────────────────────────────────────────────────────────────
# Stage 1: Install Dependencies
# ─────────────────────────────────────────────────────────────────
FROM node:20-alpine AS deps
WORKDIR /app

# Copy package files trước để tận dụng Docker cache layer.
# Nếu package.json không thay đổi, Docker sẽ dùng lại layer npm install.
COPY package.json package-lock.json ./
RUN npm ci

# ─────────────────────────────────────────────────────────────────
# Stage 2: Build Next.js Production Bundle
# ─────────────────────────────────────────────────────────────────
FROM node:20-alpine AS builder
WORKDIR /app

# Nhận các biến môi trường cần thiết cho quá trình build
# (Next.js cần các biến NEXT_PUBLIC_* tại build time, không phải runtime)
ARG NEXT_PUBLIC_API_URL
ARG NEXT_PUBLIC_SUPABASE_URL
ARG NEXT_PUBLIC_SUPABASE_ANON_KEY

ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_SUPABASE_URL=$NEXT_PUBLIC_SUPABASE_URL
ENV NEXT_PUBLIC_SUPABASE_ANON_KEY=$NEXT_PUBLIC_SUPABASE_ANON_KEY

COPY --from=deps /app/node_modules ./node_modules
COPY . .

# Tắt chức năng gửi telemetry của Next.js
ENV NEXT_TELEMETRY_DISABLED=1

RUN npm run build

# ─────────────────────────────────────────────────────────────────
# Stage 3: Production Runner
# Image gọn nhẹ chỉ chứa những thứ cần thiết để chạy
# ─────────────────────────────────────────────────────────────────
FROM node:20-alpine AS runtime
WORKDIR /app

# Tạo user non-root để tăng bảo mật
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Chỉ copy các artifacts cần thiết để chạy ứng dụng
COPY --from=builder /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs

EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

CMD ["node", "server.js"]
