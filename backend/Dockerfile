# Image of the backend for the dev and prod servers (nginx + php-fpm), run by deploy/docker-compose.yml.
# All configuration comes from environment variables at runtime — the image contains no .env and no secrets.
FROM serversideup/php:8.4-fpm-nginx

# On start the container caches config/routes/views. Migrations are NOT run here:
# deploy/deploy.sh runs them explicitly, so a failing migration fails the pipeline.
ENV AUTORUN_ENABLED=true \
    AUTORUN_LARAVEL_MIGRATION=false \
    PHP_OPCACHE_ENABLE=1

COPY --chown=www-data:www-data composer.json composer.lock ./
RUN composer install --no-dev --no-interaction --no-progress --prefer-dist --no-scripts --no-autoloader

COPY --chown=www-data:www-data . .
RUN composer dump-autoload --no-dev --optimize --classmap-authoritative
