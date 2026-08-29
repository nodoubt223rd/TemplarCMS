import { fileURLToPath, URL } from 'node:url';
import child_process from 'child_process';
import fs from 'fs';
import path from 'path';
import { env } from 'process';
import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-vue';

export default defineConfig(({ command }) => {
    const base = command === 'build'
        ? env.VITE_PUBLIC_BASE_PATH || '/author-workspace/'
        : '/';

    const config = {
        base,
        plugins: [plugin()],
        resolve: {
            alias: {
                '@': fileURLToPath(new URL('./src', import.meta.url))
            }
        }
    };

    if (command !== 'serve') {
        return config;
    }

    const baseFolder = path.join(process.cwd(), '.certs');
    const certificateName = 'templarcms.admin.client';
    const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
    const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

    if (!fs.existsSync(baseFolder)) {
        fs.mkdirSync(baseFolder, { recursive: true });
    }

    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        if (0 !== child_process.spawnSync('dotnet', [
            'dev-certs',
            'https',
            '--export-path',
            certFilePath,
            '--format',
            'Pem',
            '--no-password',
        ], { stdio: 'inherit' }).status) {
            throw new Error('Could not create certificate.');
        }
    }

    const target = env.TEMPLAR_API_BASE_URL
        ? env.TEMPLAR_API_BASE_URL
        : env.ASPNETCORE_HTTPS_PORT
        ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
        : env.ASPNETCORE_URLS
            ? env.ASPNETCORE_URLS.split(';')[0]
            : 'https://templarcms.api';

    return {
        ...config,
        server: {
            proxy: {
                '^/api': {
                    target,
                    secure: false
                }
            },
            port: parseInt(env.DEV_SERVER_PORT || '54946'),
            https: {
                key: fs.readFileSync(keyFilePath),
                cert: fs.readFileSync(certFilePath)
            }
        }
    };
});
