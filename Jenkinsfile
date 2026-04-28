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

        stage('Resolve paths') {
            steps {
                script {
                    if (fileExists('Server/Server.sln')) {
                        // 모노레포: PocoPoachers/ 루트가 워크스페이스
                        env.DOTNET_SLN = 'Server/Server.sln'
                        env.DOCKER_COMPOSE_DIR = 'Server'
                    } else if (fileExists('Server.sln')) {
                        // Server 전용 저장소: Server.sln 이 워크스페이스 루트
                        env.DOTNET_SLN = 'Server.sln'
                        env.DOCKER_COMPOSE_DIR = '.'
                    } else {
                        error('Solution not found: Server/Server.sln (mono) or Server.sln (server repo root)')
                    }
                }
            }
        }

        stage('Restore & Build (Docker .NET)') {
            steps {
                sh """
                    docker run --rm \\
                        -v \$WORKSPACE:/app \\
                        -w /app \\
                        mcr.microsoft.com/dotnet/sdk:8.0 \\
                        dotnet restore ${env.DOTNET_SLN}

                    docker run --rm \\
                        -v \$WORKSPACE:/app \\
                        -w /app \\
                        mcr.microsoft.com/dotnet/sdk:8.0 \\
                        dotnet build ${env.DOTNET_SLN} -c Release --no-restore
                """
            }
        }

        stage('Docker Build') {
            when {
                expression {
                    return fileExists('Server/docker-compose.yml') || fileExists('docker-compose.yml')
                }
            }
            steps {
                script {
                    if (env.DOCKER_COMPOSE_DIR == '.') {
                        sh 'docker compose build'
                    } else {
                        dir(env.DOCKER_COMPOSE_DIR) {
                            sh 'docker compose build'
                        }
                    }
                }
            }
        }

        stage('Deploy') {
            steps {
                script {
                    if (env.DOCKER_COMPOSE_DIR == '.') {
                        sh '''
                            docker compose down || true
                            docker compose up -d
                        '''
                    } else {
                        dir(env.DOCKER_COMPOSE_DIR) {
                            sh '''
                                docker compose down || true
                                docker compose up -d
                            '''
                        }
                    }
                }
            }
        }
    }
}
