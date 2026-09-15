/**
 * Transport-agnostic failure model.
 *
 * The API answers with RFC 9110 problem details and a status code chosen by
 * `BaseController.GetErrorStatusCode` (409 conflict, 400 validation, 404 not found,
 * 500 otherwise). This translates that into something the UI can branch on without
 * knowing Axios, or HTTP, exists.
 */
export type ApiErrorKind =
  | 'validation'
  | 'notFound'
  | 'conflict'
  | 'network'
  | 'unexpected'

export interface ProblemDetails {
  readonly title?: string
  readonly detail?: string
  readonly status?: number
  readonly errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly kind: ApiErrorKind
  readonly status: number | null
  readonly fieldErrors: Readonly<Record<string, string[]>>

  constructor(
    kind: ApiErrorKind,
    message: string,
    status: number | null = null,
    fieldErrors: Record<string, string[]> = {},
  ) {
    super(message)
    this.name = 'ApiError'
    this.kind = kind
    this.status = status
    this.fieldErrors = Object.freeze(fieldErrors)
  }

  get isValidation(): boolean {
    return this.kind === 'validation'
  }

  get isNotFound(): boolean {
    return this.kind === 'notFound'
  }

  get isConflict(): boolean {
    return this.kind === 'conflict'
  }
}

const kindByStatus: Readonly<Record<number, ApiErrorKind>> = {
  400: 'validation',
  404: 'notFound',
  409: 'conflict',
}

export function kindForStatus(status: number): ApiErrorKind {
  return kindByStatus[status] ?? 'unexpected'
}

/**
 * Problem details arrive in two shapes: a flat `detail` string from
 * `Problem(statusCode, detail)`, and a keyed `errors` map from model-binding
 * failures. Both are reduced to one readable message plus per-field errors.
 */
export function describeProblem(problem: ProblemDetails | undefined, fallback: string): string {
  if (!problem) {
    return fallback
  }

  if (problem.detail && problem.detail.trim() !== '') {
    return problem.detail
  }

  if (problem.errors) {
    const messages = Object.values(problem.errors).flat()

    if (messages.length > 0) {
      return messages.join(' ')
    }
  }

  return problem.title ?? fallback
}
