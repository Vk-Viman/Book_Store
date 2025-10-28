# Frontend multi-stage Dockerfile
FROM node:20 AS build
WORKDIR /app
COPY readify-frontend/package*.json ./
RUN npm ci
COPY readify-frontend/ .
RUN npm run build

FROM nginx:stable-alpine
COPY --from=build /app/dist/readify-frontend /usr/share/nginx/html
COPY readify-frontend/nginx/default.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["/bin/sh", "-c", "nginx -g 'daemon off;'"]
