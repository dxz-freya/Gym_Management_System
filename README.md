# Gym_Management_System

The application is a Gym Management System that allows gym members to register, book fitness sessions, track their attendance, and manage memberships. It also provides functionalities for trainers and administrators to manage class schedules, monitor attendance, and handle payments.

## 🛠 Tech Stack
**ASP.NET Core 8.0**, **Bootstrap 4** (chosen to match the provided template),  
**Entity Framework Core**, **SQLite**,  
**Chart.js**, **Barfiller.js**, **Google OAuth**, **DinkToPdf**

## 🔧 Project Setup Instructions

### 1. Restore NuGet Packages
Run this command to install all required backend libraries:
```
dotnet restore
```

### 2. Database Reset Instructions

**Delete existing migration files and database:**
- Remove all files from the `Migrations` folder  
- Delete `gym.db` from the project root

**Recreate the migration and update the database:**
```
dotnet ef database drop
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 🌱 Seeding Instructions

To fully seed everything (users, roles, branches, sessions, payments):
```
dotnet run -- seeddata
```

To clear old sessions and bookings before seeding new ones:
```
dotnet run -- seeddata --clearsessions
```

## 📌 Notes
- `Chart.js` and `Barfiller.js` are frontend libraries included via static files or CDN.
- They do **not require NuGet** and work as long as script tags or JS files are correctly included in `wwwroot/js`.

