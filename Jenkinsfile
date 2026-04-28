pipeline {
    agent any

    options {
        timestamps()
    }

    stages {

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Restore & Build (Docker .NET)') {
            steps {
                sh '''
                docker run --rm \
                -v $WORKSPACE:/app \
                -w /app \
                mcr.microsoft.com/dotnet/sdk:8.0 \
                dotnet restore Server/Server.sln

                docker run --rm \
                -v $WORKSPACE:/app \
                -w /app \
                mcr.microsoft.com/dotnet/sdk:8.0 \
                dotnet build Server/Server.sln -c Release --no-restore
                '''
            }
        }

        stage('Docker Build') {
            when {
                expression { fileExists('Server/docker-compose.yml') }
            }
            steps {
                dir('Server') {
                    sh 'docker compose build'
                }
            }
        }

        stage('Deploy') {
            steps {
                dir('Server') {
                    sh '''
                    docker compose down || true
                    docker compose up -d
                    '''
                }
            }
        }
    }
}