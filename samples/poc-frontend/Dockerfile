# Use official Node.js image for building
FROM node:20 AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# Use official Nginx image for serving static files
FROM nginx:alpine AS production
WORKDIR /usr/share/nginx/html
COPY --from=build /app/build .
EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]
