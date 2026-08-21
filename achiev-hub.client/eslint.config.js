import { globalIgnores } from 'eslint/config'
import globals from 'globals'
import pluginVue from 'eslint-plugin-vue'
import pluginOxlint from 'eslint-plugin-oxlint'
import { configureVueProject, defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript'

configureVueProject({ scriptLangs: ['ts'] })

export default defineConfigWithVueTs(
  {
    name: 'app/files-to-lint',
    files: ['**/*.{vue,js,mjs,jsx,ts,tsx}'],
  },

  globalIgnores(['**/dist/**', '**/dist-ssr/**', '**/coverage/**']),

  {
    languageOptions: {
      globals: {
        ...globals.browser,
      },
    },
  },

  pluginVue.configs['flat/essential'],
  vueTsConfigs.recommended,

  {
    name: 'app/ui-components',
    files: ['src/components/ui/**/*.{vue,ts}'],
    rules: {
      'vue/multi-word-component-names': 'off',
    },
  },

  ...pluginOxlint.buildFromOxlintConfigFile('.oxlintrc.json'),
)
