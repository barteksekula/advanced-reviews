import type { StorybookConfig } from '@storybook/react-vite';
import { mergeConfig } from 'vite';

const config: StorybookConfig = {
  "stories": [
    "../src/**/*.mdx",
    "../src/**/*.stories.@(js|jsx|mjs|ts|tsx)"
  ],
  "addons": [
    "@chromatic-com/storybook",
    "@storybook/addon-vitest",
    "@storybook/addon-a11y",
    "@storybook/addon-docs"
  ],
  "framework": "@storybook/react-vite",
  "staticDirs": ['../.storybook'],
  typescript: {
    reactDocgen: false, // Disable to avoid decorator parsing issues
  },
  async viteFinal(config) {
    return mergeConfig(config, {
      resolve: {
        extensions: ['.mjs', '.js', '.ts', '.jsx', '.tsx', '.json'],
      },
      optimizeDeps: {
        include: ['storybook/actions'],
      },
      server: {
        fs: {
          strict: false,
        },
      },
      esbuild: {
        tsconfigRaw: {
          compilerOptions: {
            experimentalDecorators: true,
            useDefineForClassFields: false,
          },
        },
      },
    });
  },
};
export default config;
