import { globalIgnores } from 'eslint/config'
import globals from 'globals'
import pluginVue from 'eslint-plugin-vue'
import pluginOxlint from 'eslint-plugin-oxlint'
import js from '@eslint/js'

export default [
    {
        name: 'app/files-to-lint',
        files: ['**/*.{vue,js,mjs,jsx}']
    },

    globalIgnores(['**/dist/**', '**/dist-ssr/**', '**/coverage/**']),

    js.configs.recommended,
    ...pluginVue.configs['flat/essential'],

    {
        languageOptions: {
            globals: {
                ...globals.browser
            }
        }
    },

    ...pluginOxlint.buildFromOxlintConfigFile('.oxlintrc.json')
]
