/**
 * The single place environment settings enter the application.
 *
 * Nothing else reads `import.meta.env`. Everything that needs configuration receives
 * this object (or a narrower slice of it) by injection, so a consumer can be handed a
 * different configuration in a test without touching the environment.
 */
export interface ApiConfig {
  readonly baseUrl: string
  readonly timeoutMs: number
}

export interface UiConfig {
  readonly appTitle: string
  readonly defaultPageSize: number
  readonly invoiceStatuses: readonly string[]
}

export interface AppConfig {
  readonly api: ApiConfig
  readonly ui: UiConfig
}

class ConfigurationError extends Error {
  constructor(key: string, reason: string) {
    super(`Configuration error for ${key}: ${reason}`)
    this.name = 'ConfigurationError'
  }
}

function readString(key: string, value: string | undefined): string {
  if (value === undefined || value.trim() === '') {
    throw new ConfigurationError(key, 'missing or empty')
  }

  return value.trim()
}

function readPositiveInt(key: string, value: string | undefined): number {
  const parsed = Number(readString(key, value))

  if (!Number.isInteger(parsed) || parsed <= 0) {
    throw new ConfigurationError(key, `expected a positive integer, got "${value}"`)
  }

  return parsed
}

function readList(key: string, value: string | undefined): readonly string[] {
  const items = readString(key, value)
    .split(',')
    .map((item) => item.trim())
    .filter((item) => item.length > 0)

  if (items.length === 0) {
    throw new ConfigurationError(key, 'expected at least one comma separated value')
  }

  return Object.freeze(items)
}

/**
 * Fails loudly at startup rather than surfacing `undefined` deep inside a request:
 * a misconfigured environment should stop the app, not degrade it.
 */
export function loadAppConfig(env: ImportMetaEnv = import.meta.env): AppConfig {
  return Object.freeze({
    api: Object.freeze({
      baseUrl: readString('VITE_API_BASE_URL', env.VITE_API_BASE_URL),
      timeoutMs: readPositiveInt('VITE_API_TIMEOUT_MS', env.VITE_API_TIMEOUT_MS),
    }),
    ui: Object.freeze({
      appTitle: readString('VITE_APP_TITLE', env.VITE_APP_TITLE),
      defaultPageSize: readPositiveInt('VITE_DEFAULT_PAGE_SIZE', env.VITE_DEFAULT_PAGE_SIZE),
      invoiceStatuses: readList('VITE_INVOICE_STATUSES', env.VITE_INVOICE_STATUSES),
    }),
  })
}
