pipeline {
    agent any

environment {
    DISCORD_URL = credentials('DISCORD_WEBHOOK')
}

    stages {
        stage('1.Checkout') {
            steps {
                checkout scm
            }
        }

        stage('2.Server Deploy') {
            steps {
                dir('Server') {
                    sh 'docker-compose down'
                    sh 'docker-compose up --build -d'
                }
            }
        }
    }
}

pipeline {
    agent any
    
    environment {
        DISCORD_URL = credentials('DISCORD_WEBHOOK')
    }

    stages {
        stage('1.Checkout') {
            steps {
                checkout scm
            }
        }

        stage('2.Server Deploy') {
            steps {
                dir('Server') {
                    sh 'docker-compose down'
                    sh 'docker-compose up --build -d'
                }
            }
        }
    }

    post {
        success {
            sh """
                curl -H "Content-Type: application/json" \
                -X POST \
                -d '{"content": "✅ **PocoPoachers 서버 배포 성공!**"}' \
                ${env.DISCORD_URL}
            """
        }
        failure {
            sh """
                curl -H "Content-Type: application/json" \
                -X POST \
                -d '{"content": "❌ **PocoPoachers 서버 배포 실패!**\\n로그를 확인하세요: ${env.BUILD_URL}"}' \
                ${env.DISCORD_URL}
            """
        }
    }
}