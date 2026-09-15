/**
 * Presentation helpers shared by every view, so formatting is defined once rather
 * than re-derived per template.
 */
const dateFormatter = new Intl.DateTimeFormat(undefined, {
  year: 'numeric',
  month: 'short',
  day: '2-digit',
})

const moneyFormatter = new Intl.NumberFormat(undefined, {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

export function formatDate(value: string | null): string {
  if (!value) {
    return '—'
  }

  const parsed = new Date(value)

  return Number.isNaN(parsed.getTime()) ? '—' : dateFormatter.format(parsed)
}

/** The API stores dates as offsets; `<input type="date">` wants a bare yyyy-MM-dd. */
export function toDateInputValue(value: string | Date): string {
  const parsed = value instanceof Date ? value : new Date(value)

  return Number.isNaN(parsed.getTime()) ? '' : parsed.toISOString().slice(0, 10)
}

/** Sends a date back as an ISO instant, which is what DateTimeOffset binds from. */
export function fromDateInputValue(value: string): string {
  return value ? new Date(`${value}T00:00:00Z`).toISOString() : ''
}

export function formatMoney(value: number): string {
  return moneyFormatter.format(value)
}

/** Tax rates are stored as a 0–1 fraction; users think in percent. */
export function formatPercent(rate: number): string {
  return `${moneyFormatter.format(rate * 100)}%`
}
