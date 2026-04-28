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
                    userRemoteConfigs: [[
                        url: 'git@github.com:ksjgusdh123/PocoPoachers.git',
                        credentialsId: 'github-ssh-key'
                    ]],
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
                    sh 'docker-compose down || true'
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
                -d '{"content": "🚀 **PocoPoachers** Server started #${env.BUILD_NUMBER}"}' \
                ${env.DISCORD_URL}
            """
        }
    }
}