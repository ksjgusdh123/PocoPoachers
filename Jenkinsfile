pipeline {
    agent any

    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    }

    options {
        timestamps()
    }

    stages {
        stage('Restore & Build') {
            steps {
                sh 'dotnet restore Server/Server.sln'
                sh 'dotnet build Server/Server.sln -c Release --no-restore'
            }
        }

        stage('Docker') {
            when { expression { return fileExists('Server/docker-compose.yml') } }
            steps {
                dir('Server') {
                    sh 'docker compose build'
                }
            }
        }
    }
}
