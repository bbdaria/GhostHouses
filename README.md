# GhostHouses
Yearly Project (2340311) @Technion 2026


## Project Overview
GhostHouses is a web-based system developed as part of the yearly Software Engineering project. The project focuses on clean development practices including structured branching, automated testing, CI/CD, and Docker-based deployment.


## Tech Stack
- Backend: .NET / C#
- Testing: xUnit (WebServer.Tests)
- Containerization: Docker & Docker Compose
- CI/CD: GitHub Actions
- Version Control: Git + GitHub Flow


## Installation & Running the Project
To run the project locally, clone the repository and navigate into the main project directory. The system is fully containerized, so running it requires only Docker and Docker Compose. Once inside the `project` directory, rebuild and start the containers to launch the application.

```bash
git clone https://github.com/bbdaria/GhostHouses.git
cd GhostHouses/project
docker compose up -d --build
```


## Project Structure
- `project/` – main application code  
- `tests/WebServer.Tests/` – automated xUnit tests  
- `.github/workflows/` – CI/CD configuration files  
- `README.md` – project documentation  


## CI/CD
The repository includes a GitHub Actions workflow that automatically builds the project, executes tests, and validates pull requests. This automated process helps maintain stability and ensures that new contributions do not break existing functionality.


## Contribution Guidelines
1. Open an issue describing the feature or bug.
2. Create a new branch:
   ```bash
   git checkout -b feature/my-feature


## Quick rebuild/run

Restart and delete changes:
```bash
cd project
```
```bash
docker compose down -v && docker compose up -d --build
```

Restart without deleting changes:
```bash
docker compose down && docker compose up -d --build
```
