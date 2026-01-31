import react from "@vitejs/plugin-react";
import eslint from "vite-plugin-eslint";
import externalize from "vite-plugin-externalize-dependencies";

const isWatch = process.argv.includes("--watch");
const isProductionBuild = process.argv.includes("build") && !isWatch;
const ignoredAmdDependencies = [
    "dojo/",
    "dijit/",
    "epi/",
    "epi-cms/",
    "episerver-cms-ui-react/",
    "advanced-cms-approval-reviews",
    "advanced-cms-external-reviews",
];

const plugins = [
    react(),
    externalize({
        externals: [
            (moduleName) => ignoredAmdDependencies.some((ignoredPattern) => moduleName.startsWith(ignoredPattern)),
        ],
    }),
    eslint({
        fix: !isProductionBuild,
        emitError: !isWatch,
        failOnError: !isWatch,
        include: ["src/**/*.ts", "src/**/*.tsx"],
        exclude: ["**/*.js", "node_modules/**"],
    }),
];

export default {
    plugins: plugins,
    build: {
        rollupOptions: {
            preserveEntrySignatures: "strict",
            input: "",
            output: {
                entryFileNames: `[name].js`,
                assetFileNames: `[name].[ext]`,
                format: "amd",
                dir: "../ClientResources/dist",
            },
            external: [
                "dojo/_base/declare",
                "dijit/_WidgetBase",
                "epi/i18n!epi/cms/nls/reviewcomponent",
                "epi/i18n!epi/cms/nls/externalreviews",
                "epi-cms/ApplicationSettings",
                "epi-cms/_ContentContextMixin",
                "advanced-cms-approval-reviews/advancedReviewService",
                "advanced-cms-external-reviews/external-review-service",
            ],
        },
        sourcemap: true,
    },
};
