
# MiniHttpServer

Учебный HTTP-сервер на C#.

## Возможности

- Обработка HTTP-запросов
- Раздача статических файлов
- Маршрутизация через Reflection API
- Поддержка атрибутов Controller, Get, Post
- Получение Query-параметров
- Обработка POST Body
- Отправка электронной почты через Mail.ru
- Обработка HTTP-ошибок

## Запуск

1. Открыть проект в Visual Studio.
2. Восстановить NuGet-пакеты.
3. Собрать проект.
4. Запустить MiniHttpServer.

Адрес сервера:
http://127.0.0.1:8888/connection/

## Маршруты

GET /connection/test/index
GET /connection/test/hello
GET /connection/test/greet?name=Ilyas

POST /connection/test/submit
POST /connection/auth/sendemail

## HTML-страницы

/connection/form.html
/connection/email.html

## Настройка SMTP

Для отправки писем используется MailKit.

SMTP-сервер: smtp.mail.ru
Порт: 465
Защита: SSL/TLS

Для настройки необходимо:

1. Создать пароль внешнего приложения Mail.ru.
2. Скопировать smtpsettings.example.json
   в smtpsettings.json.
3. Указать свой адрес почты и пароль приложения.
4. Запустить сервер.

Файл smtpsettings.json содержит секретные данные
и не должен публиковаться.

## HTTP-статусы

200 — успешное выполнение
400 — некорректные данные запроса
404 — маршрут не найден
502 — ошибка отправки письма
