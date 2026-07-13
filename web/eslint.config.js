import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
    },
    rules: {
      // Scaffold stubs intentionally leave parameters unused until the junior dev fills the
      // body in; an underscore prefix is the idiomatic "not yet used" marker.
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
      // Design-token enforcement for inline styles. stylelint guards .css files, but component
      // styling lives in JSX style objects it cannot see — this closes that gap. Any color must
      // come from a token: var(--...) defined in theme/tokens.css.
      'no-restricted-syntax': [
        'error',
        {
          selector:
            "JSXAttribute[name.name='style'] Literal[value=/#[0-9a-fA-F]{3}|\\b(?:rgba?|hsla?|oklch|color-mix)\\(|\\b(?:white|black|red|green|blue|yellow|orange|purple|brown|pink|gr[ae]y|beige|ivory|tan|gold|silver)\\b/]",
          message: 'No raw colors in inline styles — use a design token: var(--...) from theme/tokens.css.',
        },
      ],
    },
  },
])
