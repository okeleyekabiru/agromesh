// Shared helper for reading a human-readable message out of an API error.
//
// The backend's ExceptionMiddleware returns RFC 9110 problem+json bodies for both domain
// validation failures (422) and not-found errors (404) — the message lives in `detail`,
// not `message`. We still fall back to `message`/`title` for resilience against other
// error shapes (network errors, proxies, etc).
export function extractErrorMessage(e: unknown, fallback: string): string {
  const err = e as { response?: { data?: { detail?: string; message?: string; title?: string } } }
  return err?.response?.data?.detail ?? err?.response?.data?.message ?? err?.response?.data?.title ?? fallback
}

/** True when the error is a 422 "points N and M are Xkm apart" implausible-distance rejection. */
export function isImplausibleDistanceError(e: unknown): boolean {
  const err = e as { response?: { status?: number; data?: { detail?: string } } }
  return err?.response?.status === 422 && /apart/i.test(err?.response?.data?.detail ?? '')
}
