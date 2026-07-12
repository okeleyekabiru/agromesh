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

/**
 * True when the error is a 422 "this geometry looks wrong" rejection that the backend
 * would accept on retry with `confirmImplausibleGeometry: true` — covers both the
 * "points N and M are Xkm apart" and "computed area is far smaller than the spread of
 * your points suggests" (self-intersecting/misordered boundary) cases, since both
 * PolygonValidator messages end with the same "Confirm to proceed anyway." phrase.
 */
export function isImplausibleGeometryError(e: unknown): boolean {
  const err = e as { response?: { status?: number; data?: { detail?: string } } }
  return err?.response?.status === 422 && /confirm to proceed anyway/i.test(err?.response?.data?.detail ?? '')
}
