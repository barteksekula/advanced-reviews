/// <reference types="vitest/config" />
import react from "@vitejs/plugin-react";
import eslint from "vite-plugin-eslint2";
import { visualizer } from "rollup-plugin-visualizer";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";
import { storybookTest } from "@storybook/addon-vitest/vitest-plugin";
const dirname = typeof __dirname !== "undefined" ? __dirname : path.dirname(fileURLToPath(import.meta.url));

// More info at: https://storybook.js.org/docs/next/writing-tests/integrations/vitest-addon
const isWatch = process.argv.includes("--watch");
const isProductionBuild = process.argv.includes("build") && !isWatch;
const plugins = [react(), visualizer() as any];
const turnOffLinkBuildErrors = isWatch;
plugins.push(
    eslint({
        fix: !isProductionBuild,
        emitError: !turnOffLinkBuildErrors,
        failOnError: !turnOffLinkBuildErrors,
    }),
);
export default {
    plugins: plugins,
    build: {
        chunkSizeWarningLimit: 5000,
        emptyOutDir: false,
        rollupOptions: {
            input: "",
            output: {
                entryFileNames: "[name].js",
                assetFileNames: `[name].[ext]`,
                dir: "../ClientResources/dist",
            },
        },
        sourcemap: true,
    },
    test: {
        globals: true,
        environment: "jsdom",
        setupFiles: "./setup.ts",
        css: false,
        workspace: [
            {
                extends: true,
                plugins: [
                    // The plugin will run tests for the stories defined in your Storybook config
                    // See options at: https://storybook.js.org/docs/next/writing-tests/integrations/vitest-addon#storybooktest
                    storybookTest({
                        configDir: path.join(dirname, ".storybook"),
                    }),
                ],
                test: {
                    name: "storybook",
                    browser: {
                        enabled: true,
                        headless: true,
                        provider: "playwright",
                        instances: [
                            {
                                browser: "chromium",
                            },
                        ],
                    },
                    setupFiles: [".storybook/vitest.setup.ts"],
                },
            },
        ],
    },
};
