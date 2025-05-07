# ProjecTrack

## Overview
ProjecTrack is a project management application developed using **ASP.NET Core MVC** and **Microsoft SQL Server**. It enables project managers to register and manage multiple projects while efficiently assigning team members based on their availability. 

For each project, managers can add activities and automatically calculate the **Critical Path**, which is displayed graphically and can be downloaded as a PNG file. Team members have their own access, allowing them to view assigned projects and critical path estimates. They can update the status of their tasks when they begin or complete them, ensuring real-time progress tracking. The system also notifies project managers via email whenever a task status is updated, ensuring continuous tracking and improved project execution.

## Technologies Used

### Backend
- **ASP.NET Core MVC** - Web application framework
- **Microsoft SQL Server** - Database management system
- **Entity Framework Core** - ORM for database interactions
- **Identity Framework** - User authentication and authorization

### Frontend
- **Razor Views** - Dynamic UI rendering
- **Bootstrap** - Responsive UI design
- **JavaScript & jQuery** - Frontend interactions

## Features

### Authentication & Authorization
- User authentication using **ASP.NET Identity**
- Role-based access control (**Project Managers** vs. **Team Members**)

### Project Management
- Project managers can create, edit, and delete projects
- Assign team members to projects based on availability
- Real-time project progress tracking

### Task & Activity Management
- Define tasks and dependencies for each project
- Automated **Critical Path Method (CPM)** calculation
- Graphical representation of the critical path (downloadable as PNG)
- Team members can update task status (e.g., In Progress, Completed)
- Automatic status notifications sent to project managers via email

### Error Handling & Validation
- **Server-side validation** for project/task creation
- **Client-side validation** with interactive error messages
- **Logging system** to track errors and user activities

### User Experience
- **Interactive dashboard** with project insights
- **Visual feedback** for errors and form validations
- **Real-time updates** for task progress
