pipeline {
    agent any

    environment {
        DISCORD_URL = credentials('DISCORD_WEBHOOK')
    }

    stages {

        stage('1. Checkout') {
            steps {
                cleanWs()

                checkout([$class: 'GitSCM',
                    branches: [[name: '*/main']],
                    userRemoteConfigs: [[url: 'git@github.com:ksjgusdh123/PocoPoachers.git']],
                    extensions: [
                        [$class: 'SparseCheckoutPaths',
                            sparseCheckoutPaths: [[path: 'Server']]]
                    ]
                ])
            }
        }

        stage('2. Server Deploy') {
            steps {
                dir('Server') {
                    sh 'docker compose down || true'
                    sh 'docker compose up --build -d'
                }
            }
        }
    }

    post {
        success {
            sh """
                curl -H "Content-Type: application/json" \
                -X POST \
                -d '{"content": "✅ PocoPoachers 서버 배포 성공!"}' \
                ${env.DISCORD_URL}
            """
        }

        failure {
            sh """
                curl -H "Content-Type: application/json" \
                -X POST \
                -d '{"content": "❌ PocoPoachers 서버 배포 실패!\\n${env.BUILD_URL}"}' \
                ${env.DISCORD_URL}
            """
        }
    }
}