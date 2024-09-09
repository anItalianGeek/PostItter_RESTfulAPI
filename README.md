# Post-itter Backend

The Post-itter backend is an API developed in C# that serves as the bridge between the frontend application and the MySQL database. It handles data retrieval, storage, modification, and access.

## Project Structure

The backend project is organized into the following main folders:

- **Controllers**: Contains the main code that manages the application's logic and handles requests.
- **Entity and Database Models**: 
  - **Entity**: Contains classes representing the application's data types.
  - **Database Models**: Located within the Entity folder, this includes the models that map to the database tables.
- **Migrations**: Manages database schema changes and updates.
- **Application DB Context**: Defines the database context and configuration.

## Setup and Configuration

### Prerequisites

- **.NET SDK**: Version 8.0 stable

### Installation

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/anItalianGeek/PostItter_RESTfulAPI.git
   ```

2. **Set Up the Backend:**
   - Navigate to the backend directory.
   - Open the `.sln` file in your preferred IDE (e.g., Visual Studio).
   - Restore dependencies and run the application:
     ```bash
     dotnet restore
     dotnet run
     ```

### Configuration

- No specific environment variables are required.
- Database configuration is handled within the API.

## Usage

- **Running the Backend**: Use an IDE capable of handling `.sln` files to start the backend. You can run or stop the backend service from your IDE.

## Dependencies

- Dependencies are managed through NuGet and should be installed automatically when restoring the project.

## Contributing

Contributions are welcome! Feel free to fork the repository and submit pull requests. No specific contribution guidelines are established at this time.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
