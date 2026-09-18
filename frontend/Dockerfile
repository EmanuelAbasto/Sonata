# Etapa de construcción
FROM node:20-alpine AS build
WORKDIR /app

# Forzar variables de entorno para producción (esto es lo importante)
ENV VITE_API_BASE_URL=/
ENV VITE_HUB_BASE_URL=/hubs/audio-processing

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# Etapa de producción
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]